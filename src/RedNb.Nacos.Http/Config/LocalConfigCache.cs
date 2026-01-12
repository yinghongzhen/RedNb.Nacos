using RedNb.Nacos.Core;
using RedNb.Nacos.Utils;

namespace RedNb.Nacos.Client.Config;

/// <summary>
/// Local cache for configuration snapshots.
/// </summary>
public class LocalConfigCache
{
    private readonly string _cacheDir;
    private readonly object _lock = new();

    public LocalConfigCache(NacosClientOptions options)
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nacos",
            "config",
            string.IsNullOrWhiteSpace(options.Namespace) ? "default" : options.Namespace
        );
        _cacheDir = baseDir;
        
        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }
    }

    /// <summary>
    /// Saves a config snapshot to local cache.
    /// </summary>
    public void SaveSnapshot(string dataId, string group, string content)
    {
        var filePath = GetSnapshotPath(dataId, group);
        
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(filePath, content);
            }
            catch
            {
                // Ignore cache write errors
            }
        }
    }

    /// <summary>
    /// Gets a config snapshot from local cache.
    /// </summary>
    public string? GetSnapshot(string dataId, string group)
    {
        var filePath = GetSnapshotPath(dataId, group);
        
        lock (_lock)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath);
                }
            }
            catch
            {
                // Ignore cache read errors
            }
        }
        
        return null;
    }

    /// <summary>
    /// Removes a config snapshot from local cache.
    /// </summary>
    public void RemoveSnapshot(string dataId, string group)
    {
        var filePath = GetSnapshotPath(dataId, group);
        
        lock (_lock)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Ignore cache delete errors
            }
        }
    }

    /// <summary>
    /// Saves a failover config to local cache.
    /// </summary>
    public void SaveFailover(string dataId, string group, string content)
    {
        var filePath = GetFailoverPath(dataId, group);
        
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(filePath, content);
            }
            catch
            {
                // Ignore cache write errors
            }
        }
    }

    /// <summary>
    /// Gets a failover config from local cache.
    /// </summary>
    public string? GetFailover(string dataId, string group)
    {
        var filePath = GetFailoverPath(dataId, group);
        
        lock (_lock)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath);
                }
            }
            catch
            {
                // Ignore cache read errors
            }
        }
        
        return null;
    }

    private string GetSnapshotPath(string dataId, string group)
    {
        var fileName = $"{NacosUtils.UrlEncode(group)}_{NacosUtils.UrlEncode(dataId)}";
        return Path.Combine(_cacheDir, "snapshot", fileName);
    }

    private string GetFailoverPath(string dataId, string group)
    {
        var fileName = $"{NacosUtils.UrlEncode(group)}_{NacosUtils.UrlEncode(dataId)}";
        return Path.Combine(_cacheDir, "failover", fileName);
    }
}
