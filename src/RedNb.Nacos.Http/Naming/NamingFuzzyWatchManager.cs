using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core.Naming.FuzzyWatch;

namespace RedNb.Nacos.Client.Naming;

/// <summary>
/// Manages fuzzy watch subscriptions for service changes.
/// </summary>
internal class NamingFuzzyWatchManager
{
    private readonly ILogger? _logger;
    private readonly ConcurrentDictionary<string, FuzzyWatchEntry> _watchers = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _knownServices = new();
    private readonly object _lock = new();

    public NamingFuzzyWatchManager(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a watcher for the specified pattern.
    /// </summary>
    public void AddWatcher(string serviceNamePattern, string groupPattern, string namespaceId, 
        INamingFuzzyWatchEventWatcher watcher)
    {
        var key = GetWatchKey(serviceNamePattern, groupPattern, namespaceId);
        
        _watchers.AddOrUpdate(key,
            _ => new FuzzyWatchEntry
            {
                ServiceNamePattern = serviceNamePattern,
                GroupPattern = groupPattern,
                NamespaceId = namespaceId,
                Watchers = new List<INamingFuzzyWatchEventWatcher> { watcher }
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
    public void RemoveWatcher(string serviceNamePattern, string groupPattern, string namespaceId, 
        INamingFuzzyWatchEventWatcher watcher)
    {
        var key = GetWatchKey(serviceNamePattern, groupPattern, namespaceId);
        
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
    public ISet<string> GetMatchingKeys(string serviceNamePattern, string groupPattern, string namespaceId)
    {
        var result = new HashSet<string>();
        var serviceNameRegex = PatternToRegex(serviceNamePattern);
        var groupRegex = PatternToRegex(groupPattern);

        foreach (var kvp in _knownServices)
        {
            var parts = kvp.Key.Split("@@");
            if (parts.Length >= 3)
            {
                var serviceName = parts[0];
                var groupName = parts[1];
                var serviceNamespace = parts[2];

                if ((string.IsNullOrEmpty(namespaceId) || serviceNamespace == namespaceId) &&
                    serviceNameRegex.IsMatch(serviceName) &&
                    groupRegex.IsMatch(groupName))
                {
                    result.Add(kvp.Key);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Notifies watchers of a service change.
    /// </summary>
    public void NotifyServiceChange(string serviceName, string groupName, string namespaceId, 
        string changeType, string syncType)
    {
        var serviceKey = GetServiceKey(serviceName, groupName, namespaceId);

        // Update known services
        if (changeType == ServiceChangedType.AddService)
        {
            _knownServices.TryAdd(serviceKey, new HashSet<string>());
        }
        else if (changeType == ServiceChangedType.DeleteService)
        {
            _knownServices.TryRemove(serviceKey, out _);
        }

        // Notify matching watchers
        foreach (var entry in _watchers.Values)
        {
            if (!string.IsNullOrEmpty(entry.NamespaceId) && entry.NamespaceId != namespaceId)
            {
                continue;
            }

            var serviceNameRegex = PatternToRegex(entry.ServiceNamePattern);
            var groupRegex = PatternToRegex(entry.GroupPattern);

            if (serviceNameRegex.IsMatch(serviceName) && groupRegex.IsMatch(groupName))
            {
                var changeEvent = new NamingFuzzyWatchChangeEvent(
                    namespaceId,
                    groupName,
                    serviceName,
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
                        _logger?.LogError(ex, "Error notifying fuzzy watch watcher for {Service}@{Group}", 
                            serviceName, groupName);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds a known service.
    /// </summary>
    public void AddKnownService(string serviceName, string groupName, string namespaceId)
    {
        var serviceKey = GetServiceKey(serviceName, groupName, namespaceId);
        _knownServices.TryAdd(serviceKey, new HashSet<string>());
    }

    /// <summary>
    /// Removes a known service.
    /// </summary>
    public void RemoveKnownService(string serviceName, string groupName, string namespaceId)
    {
        var serviceKey = GetServiceKey(serviceName, groupName, namespaceId);
        _knownServices.TryRemove(serviceKey, out _);
    }

    private static string GetWatchKey(string serviceNamePattern, string groupPattern, string namespaceId)
    {
        return $"{serviceNamePattern}@@{groupPattern}@@{namespaceId}";
    }

    private static string GetServiceKey(string serviceName, string groupName, string namespaceId)
    {
        return $"{serviceName}@@{groupName}@@{namespaceId}";
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
        public string ServiceNamePattern { get; init; } = "";
        public string GroupPattern { get; init; } = "";
        public string NamespaceId { get; init; } = "";
        public List<INamingFuzzyWatchEventWatcher> Watchers { get; init; } = new();
    }
}
