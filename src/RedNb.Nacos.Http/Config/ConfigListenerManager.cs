using System.Collections.Concurrent;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Utils;

namespace RedNb.Nacos.Client.Config;

/// <summary>
/// Manages config listeners and their states.
/// </summary>
public class ConfigListenerManager
{
    private readonly ConcurrentDictionary<string, ConfigListenerInfo> _listeners = new();

    /// <summary>
    /// Adds a listener for a config.
    /// </summary>
    public void AddListener(string dataId, string group, string? tenant, IConfigChangeListener listener)
    {
        var key = NacosUtils.GetGroupKey(dataId, group, tenant);
        var info = _listeners.GetOrAdd(key, _ => new ConfigListenerInfo(dataId, group, tenant));
        info.AddListener(listener);
    }

    /// <summary>
    /// Removes a listener for a config.
    /// </summary>
    public void RemoveListener(string dataId, string group, string? tenant, IConfigChangeListener listener)
    {
        var key = NacosUtils.GetGroupKey(dataId, group, tenant);
        if (_listeners.TryGetValue(key, out var info))
        {
            info.RemoveListener(listener);
            if (info.IsEmpty)
            {
                _listeners.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Gets all listeners for a config.
    /// </summary>
    public List<IConfigChangeListener> GetListeners(string dataId, string group, string? tenant)
    {
        var key = NacosUtils.GetGroupKey(dataId, group, tenant);
        if (_listeners.TryGetValue(key, out var info))
        {
            return info.GetListeners();
        }
        return new List<IConfigChangeListener>();
    }

    /// <summary>
    /// Gets all configs being listened to.
    /// </summary>
    public List<ListeningConfig> GetListeningConfigs()
    {
        return _listeners.Values.Select(info => new ListeningConfig
        {
            DataId = info.DataId,
            Group = info.Group,
            Tenant = info.Tenant,
            Md5 = info.Md5
        }).ToList();
    }

    /// <summary>
    /// Gets all listened configs.
    /// </summary>
    public List<ListeningConfig> GetAllListenedConfigs()
    {
        return GetListeningConfigs();
    }

    /// <summary>
    /// Gets the MD5 hash for a config.
    /// </summary>
    public string? GetMd5(string dataId, string group, string? tenant)
    {
        var key = NacosUtils.GetGroupKey(dataId, group, tenant);
        if (_listeners.TryGetValue(key, out var info))
        {
            return info.Md5;
        }
        return null;
    }

    /// <summary>
    /// Checks if the config has changed based on MD5.
    /// </summary>
    public bool HasChanged(string dataId, string group, string? tenant, string? newMd5)
    {
        var currentMd5 = GetMd5(dataId, group, tenant);
        return currentMd5 != newMd5;
    }

    /// <summary>
    /// Updates the MD5 hash for a config.
    /// </summary>
    public void UpdateMd5(string dataId, string group, string? tenant, string? md5)
    {
        var key = NacosUtils.GetGroupKey(dataId, group, tenant);
        if (_listeners.TryGetValue(key, out var info))
        {
            info.Md5 = md5;
        }
    }

    private class ConfigListenerInfo
    {
        private readonly List<IConfigChangeListener> _listeners = new();
        private readonly object _lock = new();

        public string DataId { get; }
        public string Group { get; }
        public string? Tenant { get; }
        public string? Md5 { get; set; }

        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return _listeners.Count == 0;
                }
            }
        }

        public ConfigListenerInfo(string dataId, string group, string? tenant)
        {
            DataId = dataId;
            Group = group;
            Tenant = tenant;
        }

        public void AddListener(IConfigChangeListener listener)
        {
            lock (_lock)
            {
                if (!_listeners.Contains(listener))
                {
                    _listeners.Add(listener);
                }
            }
        }

        public void RemoveListener(IConfigChangeListener listener)
        {
            lock (_lock)
            {
                _listeners.Remove(listener);
            }
        }

        public List<IConfigChangeListener> GetListeners()
        {
            lock (_lock)
            {
                return new List<IConfigChangeListener>(_listeners);
            }
        }
    }
}

/// <summary>
/// Represents a config being listened to.
/// </summary>
public class ListeningConfig
{
    public string DataId { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? Tenant { get; set; }
    public string? Md5 { get; set; }
}
