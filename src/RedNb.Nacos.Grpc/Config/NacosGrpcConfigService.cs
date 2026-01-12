using Microsoft.Extensions.Logging;
using RedNb.Nacos.Client.Config;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Config.Filter;
using RedNb.Nacos.Core.Config.FuzzyWatch;
using RedNb.Nacos.Utils;

namespace RedNb.Nacos.GrpcClient.Config;

/// <summary>
/// Nacos config service implementation using gRPC.
/// </summary>
public class NacosGrpcConfigService : IConfigService
{
    private readonly NacosClientOptions _options;
    private readonly NacosGrpcClient _grpcClient;
    private readonly ILogger<NacosGrpcConfigService>? _logger;
    private readonly ConfigListenerManager _listenerManager;
    private readonly LocalConfigCache _localCache;
    private bool _disposed;
    private bool _isHealthy;

    public NacosGrpcConfigService(NacosClientOptions options, ILogger<NacosGrpcConfigService>? logger = null)
    {
        _options = options;
        _logger = logger;
        _grpcClient = new NacosGrpcClient(options, logger);
        _listenerManager = new ConfigListenerManager();
        _localCache = new LocalConfigCache(options);

        // Register push handler for config changes
        _grpcClient.RegisterPushHandler(HandlePushMessage);
    }

    /// <summary>
    /// Initializes the gRPC connection.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _grpcClient.ConnectAsync(cancellationToken);
        _isHealthy = true;
    }

    public async Task<string?> GetConfigAsync(string dataId, string group, long timeoutMs, 
        CancellationToken cancellationToken = default)
    {
        group = GetGroupOrDefault(group);

        try
        {
            var request = new ConfigQueryRequest
            {
                DataId = dataId,
                Group = group,
                Tenant = GetTenant()
            };

            var response = await _grpcClient.RequestAsync<ConfigQueryResponse>(
                "ConfigQueryRequest", request, cancellationToken);

            if (response?.Success == true && response.Content != null)
            {
                _localCache.SaveSnapshot(dataId, group, response.Content);
                _isHealthy = true;
                return response.Content;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get config from gRPC server, trying local cache");
            _isHealthy = false;

            var cached = _localCache.GetSnapshot(dataId, group);
            if (cached != null)
            {
                return cached;
            }

            throw;
        }
    }

    public async Task<string?> GetConfigAndSignListenerAsync(string dataId, string group, long timeoutMs, 
        IConfigChangeListener listener, CancellationToken cancellationToken = default)
    {
        var content = await GetConfigAsync(dataId, group, timeoutMs, cancellationToken);
        await AddListenerAsync(dataId, group, listener, cancellationToken);
        return content;
    }

    public async Task AddListenerAsync(string dataId, string group, IConfigChangeListener listener, 
        CancellationToken cancellationToken = default)
    {
        group = GetGroupOrDefault(group);
        _listenerManager.AddListener(dataId, group, GetTenant(), listener);

        // Send listen request to server
        var request = new ConfigListenRequest
        {
            DataId = dataId,
            Group = group,
            Tenant = GetTenant(),
            Listen = true
        };

        await _grpcClient.SendStreamRequestAsync("ConfigBatchListenRequest", 
            new { configListenContexts = new[] { request } }, cancellationToken);

        _logger?.LogDebug("Added gRPC listener for {DataId}@{Group}", dataId, group);
    }

    public void RemoveListener(string dataId, string group, IConfigChangeListener listener)
    {
        group = GetGroupOrDefault(group);
        _listenerManager.RemoveListener(dataId, group, GetTenant(), listener);
        _logger?.LogDebug("Removed gRPC listener for {DataId}@{Group}", dataId, group);
    }

    public async Task<bool> PublishConfigAsync(string dataId, string group, string content, 
        CancellationToken cancellationToken = default)
    {
        return await PublishConfigAsync(dataId, group, content, ConfigType.Default, cancellationToken);
    }

    public async Task<bool> PublishConfigAsync(string dataId, string group, string content, string type, 
        CancellationToken cancellationToken = default)
    {
        group = GetGroupOrDefault(group);

        var request = new ConfigPublishRequest
        {
            DataId = dataId,
            Group = group,
            Tenant = GetTenant(),
            Content = content,
            Type = type
        };

        var response = await _grpcClient.RequestAsync<ConfigPublishResponse>(
            "ConfigPublishRequest", request, cancellationToken);

        return response?.Success ?? false;
    }

    public async Task<bool> PublishConfigCasAsync(string dataId, string group, string content, string casMd5, 
        CancellationToken cancellationToken = default)
    {
        return await PublishConfigCasAsync(dataId, group, content, casMd5, ConfigType.Default, cancellationToken);
    }

    public async Task<bool> PublishConfigCasAsync(string dataId, string group, string content, string casMd5, 
        string type, CancellationToken cancellationToken = default)
    {
        group = GetGroupOrDefault(group);

        var request = new ConfigPublishRequest
        {
            DataId = dataId,
            Group = group,
            Tenant = GetTenant(),
            Content = content,
            Type = type,
            CasMd5 = casMd5
        };

        var response = await _grpcClient.RequestAsync<ConfigPublishResponse>(
            "ConfigPublishRequest", request, cancellationToken);

        return response?.Success ?? false;
    }

    public async Task<bool> RemoveConfigAsync(string dataId, string group, 
        CancellationToken cancellationToken = default)
    {
        group = GetGroupOrDefault(group);

        var request = new ConfigRemoveRequest
        {
            DataId = dataId,
            Group = group,
            Tenant = GetTenant()
        };

        var response = await _grpcClient.RequestAsync<ConfigRemoveResponse>(
            "ConfigRemoveRequest", request, cancellationToken);

        if (response?.Success == true)
        {
            _localCache.RemoveSnapshot(dataId, group);
        }

        return response?.Success ?? false;
    }

    public string GetServerStatus()
    {
        return _isHealthy && _grpcClient.IsConnected ? "UP" : "DOWN";
    }

    public void AddConfigFilter(IConfigFilter configFilter)
    {
        // Filters are not yet implemented in gRPC client
        _logger?.LogWarning("Config filters are not yet supported in gRPC client");
    }

    #region Fuzzy Watch

    public Task FuzzyWatchAsync(string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        return FuzzyWatchAsync("*", groupNamePattern, watcher, cancellationToken);
    }

    public Task FuzzyWatchAsync(string dataIdPattern, string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement fuzzy watch via gRPC stream
        _logger?.LogWarning("Fuzzy watch is not yet implemented in gRPC client");
        return Task.CompletedTask;
    }

    public Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string groupNamePattern, 
        IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        return FuzzyWatchWithGroupKeysAsync("*", groupNamePattern, watcher, cancellationToken);
    }

    public Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string dataIdPattern, string groupNamePattern, 
        IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        // TODO: Implement fuzzy watch via gRPC stream
        _logger?.LogWarning("Fuzzy watch is not yet implemented in gRPC client");
        return Task.FromResult<ISet<string>>(new HashSet<string>());
    }

    public Task CancelFuzzyWatchAsync(string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        return CancelFuzzyWatchAsync("*", groupNamePattern, watcher, cancellationToken);
    }

    public Task CancelFuzzyWatchAsync(string dataIdPattern, string groupNamePattern, 
        IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        // TODO: Implement fuzzy watch via gRPC stream
        _logger?.LogWarning("Fuzzy watch is not yet implemented in gRPC client");
        return Task.CompletedTask;
    }

    #endregion

    public async Task ShutdownAsync()
    {
        await DisposeAsync();
    }

    private void HandlePushMessage(string type, string body)
    {
        if (type.Contains("ConfigChange"))
        {
            try
            {
                var change = System.Text.Json.JsonSerializer.Deserialize<ConfigChangeNotifyRequest>(body);
                if (change != null)
                {
                    _ = NotifyListenersAsync(change.DataId, change.Group, change.Tenant);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling config change push");
            }
        }
    }

    private async Task NotifyListenersAsync(string dataId, string group, string? tenant)
    {
        try
        {
            var content = await GetConfigAsync(dataId, group, _options.DefaultTimeout);
            var md5 = content != null ? NacosUtils.GetMd5(content) : null;

            var configInfo = new ConfigInfo
            {
                DataId = dataId,
                Group = group,
                Tenant = tenant,
                Content = content,
                Md5 = md5
            };

            var listeners = _listenerManager.GetListeners(dataId, group, tenant);
            foreach (var listener in listeners)
            {
                try
                {
                    listener.OnReceiveConfigInfo(configInfo);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error notifying listener for {DataId}@{Group}", dataId, group);
                }
            }

            _listenerManager.UpdateMd5(dataId, group, tenant, md5);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to notify listeners for {DataId}@{Group}", dataId, group);
        }
    }

    private string GetGroupOrDefault(string? group)
    {
        return string.IsNullOrWhiteSpace(group) ? NacosConstants.DefaultGroup : group.Trim();
    }

    private string? GetTenant()
    {
        return string.IsNullOrWhiteSpace(_options.Namespace) ? null : _options.Namespace;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _grpcClient.DisposeAsync();
        _disposed = true;
    }

    #region Request/Response Models

    private class ConfigQueryRequest
    {
        public string DataId { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string? Tenant { get; set; }
    }

    private class ConfigQueryResponse
    {
        public bool Success { get; set; }
        public string? Content { get; set; }
        public string? Md5 { get; set; }
        public string? Type { get; set; }
    }

    private class ConfigPublishRequest
    {
        public string DataId { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string? Tenant { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? CasMd5 { get; set; }
    }

    private class ConfigPublishResponse
    {
        public bool Success { get; set; }
    }

    private class ConfigRemoveRequest
    {
        public string DataId { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string? Tenant { get; set; }
    }

    private class ConfigRemoveResponse
    {
        public bool Success { get; set; }
    }

    private class ConfigListenRequest
    {
        public string DataId { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string? Tenant { get; set; }
        public bool Listen { get; set; }
    }

    private class ConfigChangeNotifyRequest
    {
        public string DataId { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string? Tenant { get; set; }
    }

    #endregion
}
