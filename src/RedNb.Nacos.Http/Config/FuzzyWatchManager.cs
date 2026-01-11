using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core.Config.FuzzyWatch;

namespace RedNb.Nacos.Client.Config;

/// <summary>
/// Manages fuzzy watch subscriptions for configuration changes.
/// </summary>
internal class FuzzyWatchManager
{
    private readonly ILogger? _logger;
    private readonly ConcurrentDictionary<string, FuzzyWatchEntry> _watchers = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _knownConfigs = new();
    private readonly object _lock = new();

    public FuzzyWatchManager(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a watcher for the specified pattern.
    /// </summary>
    public void AddWatcher(string dataIdPattern, string groupPattern, string tenant, 
        IConfigFuzzyWatchEventWatcher watcher)
    {
        var key = GetWatchKey(dataIdPattern, groupPattern, tenant);
        
        _watchers.AddOrUpdate(key,
            _ => new FuzzyWatchEntry
            {
                DataIdPattern = dataIdPattern,
                GroupPattern = groupPattern,
                Tenant = tenant,
                Watchers = new List<IConfigFuzzyWatchEventWatcher> { watcher }
            },
            (_, entry) =>
            {
                lock (_lock)
                {
                    if (!entry.Watchers.Contains(watcher))
                    {
                        entry.Watchers.Add(watcher);
                    }
                }
                return entry;
            });
    }

    /// <summary>
    /// Removes a watcher for the specified pattern.
    /// </summary>
    public void RemoveWatcher(string dataIdPattern, string groupPattern, string tenant, 
        IConfigFuzzyWatchEventWatcher watcher)
    {
        var key = GetWatchKey(dataIdPattern, groupPattern, tenant);
        
        if (_watchers.TryGetValue(key, out var entry))
        {
            lock (_lock)
            {
                entry.Watchers.Remove(watcher);
                
                if (entry.Watchers.Count == 0)
                {
                    _watchers.TryRemove(key, out _);
                }
            }
        }
    }

    /// <summary>
    /// Gets matching keys for the specified pattern.
    /// </summary>
    public ISet<string> GetMatchingKeys(string dataIdPattern, string groupPattern, string tenant)
    {
        var result = new HashSet<string>();
        var dataIdRegex = PatternToRegex(dataIdPattern);
        var groupRegex = PatternToRegex(groupPattern);

        foreach (var kvp in _knownConfigs)
        {
            var parts = kvp.Key.Split("@@");
            if (parts.Length >= 3)
            {
                var configDataId = parts[0];
                var configGroup = parts[1];
                var configTenant = parts[2];

                if ((string.IsNullOrEmpty(tenant) || configTenant == tenant) &&
                    dataIdRegex.IsMatch(configDataId) &&
                    groupRegex.IsMatch(configGroup))
                {
                    result.Add(kvp.Key);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Notifies watchers of a configuration change.
    /// </summary>
    public void NotifyConfigChange(string dataId, string group, string tenant, 
        string changeType, string syncType)
    {
        var configKey = GetConfigKey(dataId, group, tenant);

        // Update known configs
        if (changeType == ConfigChangedType.AddConfig)
        {
            _knownConfigs.TryAdd(configKey, new HashSet<string>());
        }
        else if (changeType == ConfigChangedType.DeleteConfig)
        {
            _knownConfigs.TryRemove(configKey, out _);
        }

        // Notify matching watchers
        foreach (var entry in _watchers.Values)
        {
            if (!string.IsNullOrEmpty(entry.Tenant) && entry.Tenant != tenant)
            {
                continue;
            }

            var dataIdRegex = PatternToRegex(entry.DataIdPattern);
            var groupRegex = PatternToRegex(entry.GroupPattern);

            if (dataIdRegex.IsMatch(dataId) && groupRegex.IsMatch(group))
            {
                var changeEvent = ConfigFuzzyWatchChangeEvent.Build(
                    tenant,
                    group,
                    dataId,
                    changeType,
                    syncType);

                foreach (var watcher in entry.Watchers.ToList())
                {
                    try
                    {
                        var scheduler = watcher.Scheduler;
                        if (scheduler != null)
                        {
                            Task.Factory.StartNew(() => watcher.OnEvent(changeEvent), 
                                CancellationToken.None, TaskCreationOptions.None, scheduler);
                        }
                        else
                        {
                            watcher.OnEvent(changeEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error notifying fuzzy watch watcher for {DataId}@{Group}", 
                            dataId, group);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds a known configuration.
    /// </summary>
    public void AddKnownConfig(string dataId, string group, string tenant)
    {
        var configKey = GetConfigKey(dataId, group, tenant);
        _knownConfigs.TryAdd(configKey, new HashSet<string>());
    }

    /// <summary>
    /// Removes a known configuration.
    /// </summary>
    public void RemoveKnownConfig(string dataId, string group, string tenant)
    {
        var configKey = GetConfigKey(dataId, group, tenant);
        _knownConfigs.TryRemove(configKey, out _);
    }

    private static string GetWatchKey(string dataIdPattern, string groupPattern, string tenant)
    {
        return $"{dataIdPattern}@@{groupPattern}@@{tenant}";
    }

    private static string GetConfigKey(string dataId, string group, string tenant)
    {
        return $"{dataId}@@{group}@@{tenant}";
    }

    private static Regex PatternToRegex(string pattern)
    {
        // Convert wildcard pattern to regex
        // * matches any sequence of characters
        // ? matches any single character
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        
        return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    private class FuzzyWatchEntry
    {
        public string DataIdPattern { get; init; } = "";
        public string GroupPattern { get; init; } = "";
        public string Tenant { get; init; } = "";
        public List<IConfigFuzzyWatchEventWatcher> Watchers { get; init; } = new();
    }
}
