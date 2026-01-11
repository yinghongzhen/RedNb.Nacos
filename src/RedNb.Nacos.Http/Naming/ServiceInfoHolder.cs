using System.Collections.Concurrent;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Client.Naming;

/// <summary>
/// Holds and caches service information.
/// </summary>
public class ServiceInfoHolder
{
    private readonly ConcurrentDictionary<string, ServiceInfo> _serviceInfoMap = new();
    private readonly NacosClientOptions? _options;
    private readonly string _cacheDir;

    public ServiceInfoHolder()
    {
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nacos",
            "naming",
            "default"
        );
    }

    public ServiceInfoHolder(NacosClientOptions options)
    {
        _options = options;
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nacos",
            "naming",
            string.IsNullOrWhiteSpace(options.Namespace) ? "default" : options.Namespace
        );

        if (_options.NamingLoadCacheAtStart)
        {
            LoadFromDisk();
        }
    }

    /// <summary>
    /// Gets service info from cache.
    /// </summary>
    public ServiceInfo? GetServiceInfo(string serviceName, string groupName, string clusters)
    {
        var key = ServiceInfo.GetKey(
            $"{groupName}{NacosConstants.ServiceInfoSplitter}{serviceName}", 
            clusters);
        
        _serviceInfoMap.TryGetValue(key, out var serviceInfo);
        return serviceInfo;
    }

    /// <summary>
    /// Processes service info from server, returns true if changed.
    /// </summary>
    public bool ProcessServiceInfo(ServiceInfo serviceInfo)
    {
        var key = serviceInfo.Key;
        var oldInfo = _serviceInfoMap.GetValueOrDefault(key);

        serviceInfo.LastRefTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _serviceInfoMap[key] = serviceInfo;

        // Check if changed
        var changed = HasChanged(oldInfo, serviceInfo);

        if (changed)
        {
            SaveToDisk(serviceInfo);
        }

        return changed;
    }

    /// <summary>
    /// Gets all cached service infos.
    /// </summary>
    public IEnumerable<ServiceInfo> GetAllServiceInfos()
    {
        return _serviceInfoMap.Values;
    }

    /// <summary>
    /// Updates service info in cache.
    /// </summary>
    public void UpdateServiceInfo(ServiceInfo serviceInfo)
    {
        var key = GetServiceKey(serviceInfo.Name, serviceInfo.GroupName ?? NacosConstants.DefaultGroup, serviceInfo.Clusters);
        serviceInfo.LastRefTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _serviceInfoMap[key] = serviceInfo;
    }

    /// <summary>
    /// Removes service info from cache.
    /// </summary>
    public void RemoveServiceInfo(string serviceName, string groupName, string clusters)
    {
        var key = GetServiceKey(serviceName, groupName, clusters);
        _serviceInfoMap.TryRemove(key, out _);
    }

    private static string GetServiceKey(string? serviceName, string? groupName, string? clusters)
    {
        var key = $"{groupName ?? NacosConstants.DefaultGroup}@@{serviceName ?? ""}";
        if (!string.IsNullOrEmpty(clusters))
        {
            key += $"@@{clusters}";
        }
        return key;
    }

    private bool HasChanged(ServiceInfo? oldInfo, ServiceInfo newInfo)
    {
        if (oldInfo == null)
        {
            return true;
        }

        if (oldInfo.Hosts.Count != newInfo.Hosts.Count)
        {
            return true;
        }

        var oldIps = oldInfo.Hosts.Select(h => h.ToInetAddr()).OrderBy(x => x).ToList();
        var newIps = newInfo.Hosts.Select(h => h.ToInetAddr()).OrderBy(x => x).ToList();

        return !oldIps.SequenceEqual(newIps);
    }

    private void SaveToDisk(ServiceInfo serviceInfo)
    {
        try
        {
            if (!Directory.Exists(_cacheDir))
            {
                Directory.CreateDirectory(_cacheDir);
            }

            var fileName = GetFileName(serviceInfo.Key);
            var filePath = Path.Combine(_cacheDir, fileName);
            var json = System.Text.Json.JsonSerializer.Serialize(serviceInfo);
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Ignore cache write errors
        }
    }

    private void LoadFromDisk()
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
                    var serviceInfo = System.Text.Json.JsonSerializer.Deserialize<ServiceInfo>(json);
                    if (serviceInfo != null)
                    {
                        _serviceInfoMap[serviceInfo.Key] = serviceInfo;
                    }
                }
                catch
                {
                    // Ignore individual file read errors
                }
            }
        }
        catch
        {
            // Ignore cache load errors
        }
    }

    private static string GetFileName(string key)
    {
        // Replace invalid chars
        var fileName = key.Replace("@@", "_").Replace(":", "_");
        return $"{fileName}.json";
    }
}
