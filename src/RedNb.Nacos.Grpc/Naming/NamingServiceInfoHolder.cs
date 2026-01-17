using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.GrpcClient.Naming;

/// <summary>
/// Holds and manages service information cache.
/// Provides local cache for failover and change detection.
/// </summary>
internal class NamingServiceInfoHolder
{
    private readonly NacosClientOptions _options;
    private readonly ILogger? _logger;
    private readonly ConcurrentDictionary<string, NamingServiceInfo> _serviceInfoMap = new();
    private readonly string _cacheDir;
    private readonly object _updateLock = new();

    /// <summary>
    /// Event fired when service info changes.
    /// </summary>
    public event Action<NamingServiceInfo, NamingServiceInfo?>? OnServiceInfoChanged;

    public NamingServiceInfoHolder(NacosClientOptions options, ILogger? logger = null)
    {
        _options = options;
        _logger = logger;
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "nacos", "naming", options.Namespace ?? "public");

        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }

        // Load cached services at start if enabled
        if (options.NamingLoadCacheAtStart)
        {
            LoadCachedServices();
        }
    }

    /// <summary>
    /// Gets service info from cache.
    /// </summary>
    public NamingServiceInfo? GetServiceInfo(string serviceName, string groupName, string? clusters = null)
    {
        var key = NamingServiceInfo.GetKey(serviceName, groupName, clusters ?? "");
        _serviceInfoMap.TryGetValue(key, out var serviceInfo);
        return serviceInfo;
    }

    /// <summary>
    /// Gets all cached service infos.
    /// </summary>
    public IEnumerable<NamingServiceInfo> GetAllServiceInfos()
    {
        return _serviceInfoMap.Values;
    }

    /// <summary>
    /// Processes incoming service info and detects changes.
    /// </summary>
    /// <param name="newServiceInfo">The new service info from server</param>
    /// <returns>True if there was a change</returns>
    public bool ProcessServiceInfo(NamingServiceInfo newServiceInfo)
    {
        if (newServiceInfo == null || !newServiceInfo.IsValid)
        {
            return false;
        }

        var key = newServiceInfo.GetKey();

        lock (_updateLock)
        {
            _serviceInfoMap.TryGetValue(key, out var oldServiceInfo);

            // Check if push empty protection is enabled
            if (_options.NamingPushEmptyProtection && IsEmptyOrNull(newServiceInfo.Hosts))
            {
                if (oldServiceInfo != null && !IsEmptyOrNull(oldServiceInfo.Hosts))
                {
                    _logger?.LogWarning(
                        "Push empty protection: ignoring empty service info for {Service}, keeping {Count} instances",
                        key, oldServiceInfo.Hosts?.Count ?? 0);
                    return false;
                }
            }

            var hasChanged = IsChanged(oldServiceInfo, newServiceInfo);

            if (hasChanged)
            {
                _serviceInfoMap[key] = newServiceInfo;

                // Save to disk cache
                SaveServiceInfoToDisk(key, newServiceInfo);

                // Fire change event
                OnServiceInfoChanged?.Invoke(newServiceInfo, oldServiceInfo);

                _logger?.LogDebug("Service info updated: {Key}, instances: {Count}",
                    key, newServiceInfo.Hosts?.Count ?? 0);
            }

            return hasChanged;
        }
    }

    /// <summary>
    /// Removes service info from cache.
    /// </summary>
    public void RemoveServiceInfo(string serviceName, string groupName, string? clusters = null)
    {
        var key = NamingServiceInfo.GetKey(serviceName, groupName, clusters ?? "");
        _serviceInfoMap.TryRemove(key, out _);
        RemoveServiceInfoFromDisk(key);
    }

    /// <summary>
    /// Checks if service info has changed.
    /// </summary>
    private bool IsChanged(NamingServiceInfo? oldInfo, NamingServiceInfo newInfo)
    {
        if (oldInfo == null)
        {
            return true;
        }

        // Check checksum if available
        if (!string.IsNullOrEmpty(oldInfo.Checksum) && !string.IsNullOrEmpty(newInfo.Checksum))
        {
            return oldInfo.Checksum != newInfo.Checksum;
        }

        // Compare host lists
        var oldHosts = oldInfo.Hosts ?? new List<NamingInstance>();
        var newHosts = newInfo.Hosts ?? new List<NamingInstance>();

        if (oldHosts.Count != newHosts.Count)
        {
            return true;
        }

        var oldHostSet = new HashSet<string>(oldHosts.Select(h => GetInstanceKey(h)));
        var newHostSet = new HashSet<string>(newHosts.Select(h => GetInstanceKey(h)));

        return !oldHostSet.SetEquals(newHostSet);
    }

    /// <summary>
    /// Gets a unique key for an instance.
    /// </summary>
    private static string GetInstanceKey(NamingInstance instance)
    {
        return $"{instance.Ip}:{instance.Port}:{instance.Weight}:{instance.Healthy}:{instance.Enabled}:{instance.ClusterName}";
    }

    /// <summary>
    /// Computes instance changes between old and new service info.
    /// </summary>
    public (List<Instance> Added, List<Instance> Removed, List<Instance> Modified) ComputeChanges(
        NamingServiceInfo? oldInfo, NamingServiceInfo newInfo)
    {
        var added = new List<Instance>();
        var removed = new List<Instance>();
        var modified = new List<Instance>();

        var oldHosts = oldInfo?.Hosts ?? new List<NamingInstance>();
        var newHosts = newInfo.Hosts ?? new List<NamingInstance>();

        var oldHostMap = oldHosts.ToDictionary(h => $"{h.Ip}:{h.Port}", h => h);
        var newHostMap = newHosts.ToDictionary(h => $"{h.Ip}:{h.Port}", h => h);

        // Find added and modified
        foreach (var kvp in newHostMap)
        {
            if (oldHostMap.TryGetValue(kvp.Key, out var oldHost))
            {
                // Check if modified
                if (IsInstanceModified(oldHost, kvp.Value))
                {
                    modified.Add(MapToInstance(kvp.Value));
                }
            }
            else
            {
                added.Add(MapToInstance(kvp.Value));
            }
        }

        // Find removed
        foreach (var kvp in oldHostMap)
        {
            if (!newHostMap.ContainsKey(kvp.Key))
            {
                removed.Add(MapToInstance(kvp.Value));
            }
        }

        return (added, removed, modified);
    }

    private static bool IsInstanceModified(NamingInstance old, NamingInstance current)
    {
        return old.Weight != current.Weight ||
               old.Healthy != current.Healthy ||
               old.Enabled != current.Enabled ||
               old.ClusterName != current.ClusterName ||
               !MetadataEquals(old.Metadata, current.Metadata);
    }

    private static bool MetadataEquals(Dictionary<string, string>? m1, Dictionary<string, string>? m2)
    {
        if (m1 == null && m2 == null) return true;
        if (m1 == null || m2 == null) return false;
        if (m1.Count != m2.Count) return false;
        return m1.All(kvp => m2.TryGetValue(kvp.Key, out var value) && value == kvp.Value);
    }

    /// <summary>
    /// Maps NamingInstance to Instance.
    /// </summary>
    public static Instance MapToInstance(NamingInstance ni)
    {
        return new Instance
        {
            InstanceId = ni.InstanceId,
            Ip = ni.Ip,
            Port = ni.Port,
            Weight = ni.Weight,
            Healthy = ni.Healthy,
            Enabled = ni.Enabled,
            Ephemeral = ni.Ephemeral,
            ClusterName = ni.ClusterName ?? NacosConstants.DefaultClusterName,
            ServiceName = ni.ServiceName,
            Metadata = ni.Metadata ?? new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// Maps Instance to NamingInstance.
    /// </summary>
    public static NamingInstance MapToNamingInstance(Instance instance)
    {
        return new NamingInstance
        {
            InstanceId = instance.InstanceId,
            Ip = instance.Ip ?? "",
            Port = instance.Port,
            Weight = instance.Weight,
            Healthy = instance.Healthy,
            Enabled = instance.Enabled,
            Ephemeral = instance.Ephemeral,
            ClusterName = instance.ClusterName ?? NacosConstants.DefaultClusterName,
            ServiceName = instance.ServiceName,
            Metadata = instance.Metadata ?? new Dictionary<string, string>()
        };
    }

    private static bool IsEmptyOrNull<T>(List<T>? list)
    {
        return list == null || list.Count == 0;
    }

    #region Disk Cache

    private void LoadCachedServices()
    {
        try
        {
            if (!Directory.Exists(_cacheDir))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(_cacheDir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var serviceInfo = JsonSerializer.Deserialize<NamingServiceInfo>(json);
                    if (serviceInfo != null && serviceInfo.IsValid)
                    {
                        var key = serviceInfo.GetKey();
                        _serviceInfoMap[key] = serviceInfo;
                        _logger?.LogDebug("Loaded cached service info: {Key}", key);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to load cached service from {File}", file);
                }
            }

            _logger?.LogInformation("Loaded {Count} cached services", _serviceInfoMap.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load cached services");
        }
    }

    private void SaveServiceInfoToDisk(string key, NamingServiceInfo serviceInfo)
    {
        try
        {
            var fileName = SanitizeFileName(key) + ".json";
            var filePath = Path.Combine(_cacheDir, fileName);
            var json = JsonSerializer.Serialize(serviceInfo);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save service info to disk: {Key}", key);
        }
    }

    private void RemoveServiceInfoFromDisk(string key)
    {
        try
        {
            var fileName = SanitizeFileName(key) + ".json";
            var filePath = Path.Combine(_cacheDir, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to remove service info from disk: {Key}", key);
        }
    }

    private static string SanitizeFileName(string key)
    {
        return key
            .Replace("@@", "_")
            .Replace("/", "_")
            .Replace("\\", "_")
            .Replace(":", "_");
    }

    #endregion
}
