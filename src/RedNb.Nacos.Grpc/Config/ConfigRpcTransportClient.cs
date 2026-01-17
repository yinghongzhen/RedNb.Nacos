using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;

namespace RedNb.Nacos.GrpcClient.Config;

/// <summary>
/// Config-specific gRPC transport client.
/// Handles configuration service communication over gRPC.
/// </summary>
internal class ConfigRpcTransportClient : IAsyncDisposable
{
    private readonly NacosGrpcClient _grpcClient;
    private readonly NacosClientOptions _options;
    private readonly ILogger? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Event fired when a config change notification is received.
    /// </summary>
    public event Action<ConfigChangeNotifyRequest>? OnConfigChanged;

    /// <summary>
    /// Event fired when a fuzzy watch change notification is received.
    /// </summary>
    public event Action<ConfigFuzzyWatchChangeNotifyRequest>? OnFuzzyWatchChanged;

    public ConfigRpcTransportClient(NacosGrpcClient grpcClient, NacosClientOptions options, ILogger? logger = null)
    {
        _grpcClient = grpcClient;
        _options = options;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Register push handler
        _grpcClient.RegisterPushHandler("config", HandlePushMessage);
    }

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    public bool IsConnected => _grpcClient.IsConnected;

    /// <summary>
    /// Queries configuration from server.
    /// </summary>
    public async Task<ConfigQueryResponse?> QueryConfigAsync(string dataId, string group, string? tenant, 
        string? tag = null, CancellationToken cancellationToken = default)
    {
        var request = new ConfigQueryRequest
        {
            DataId = dataId,
            Group = group,
            Tenant = tenant,
            Tag = tag
        };

        return await _grpcClient.RequestAsync<ConfigQueryResponse>(
            ConfigQueryRequest.TYPE, request, cancellationToken);
    }

    /// <summary>
    /// Publishes configuration to server.
    /// </summary>
    public async Task<bool> PublishConfigAsync(string dataId, string group, string? tenant,
        string content, string? type = null, string? casMd5 = null, string? encryptedDataKey = null,
        Dictionary<string, string>? additionalParams = null, CancellationToken cancellationToken = default)
    {
        var request = new ConfigPublishRequest
        {
            DataId = dataId,
            Group = group,
            Tenant = tenant,
            Content = content,
            Type = type,
            CasMd5 = casMd5,
            EncryptedDataKey = encryptedDataKey,
            AdditionMap = additionalParams
        };

        var response = await _grpcClient.RequestAsync<ConfigPublishResponse>(
            ConfigPublishRequest.TYPE, request, cancellationToken);

        return response?.IsSuccess ?? false;
    }

    /// <summary>
    /// Removes configuration from server.
    /// </summary>
    public async Task<bool> RemoveConfigAsync(string dataId, string group, string? tenant,
        string? tag = null, CancellationToken cancellationToken = default)
    {
        var request = new ConfigRemoveRequest
        {
            DataId = dataId,
            Group = group,
            Tenant = tenant,
            Tag = tag
        };

        var response = await _grpcClient.RequestAsync<ConfigRemoveResponse>(
            ConfigRemoveRequest.TYPE, request, cancellationToken);

        return response?.IsSuccess ?? false;
    }

    /// <summary>
    /// Sends batch listen request for configurations.
    /// </summary>
    public async Task<ConfigBatchListenResponse?> BatchListenAsync(
        List<ConfigListenContext> listenContexts, bool listen = true,
        CancellationToken cancellationToken = default)
    {
        var request = new ConfigBatchListenRequest
        {
            Listen = listen,
            ConfigListenContexts = listenContexts
        };

        // Use stream request for listen operations
        return await _grpcClient.SendStreamRequestWithResponseAsync<ConfigBatchListenResponse>(
            ConfigBatchListenRequest.TYPE, request,
            TimeSpan.FromMilliseconds(_options.LongPollTimeout),
            cancellationToken);
    }

    /// <summary>
    /// Sends batch listen request via stream (fire and forget).
    /// </summary>
    public async Task SendBatchListenAsync(List<ConfigListenContext> listenContexts, bool listen = true,
        CancellationToken cancellationToken = default)
    {
        var request = new ConfigBatchListenRequest
        {
            Listen = listen,
            ConfigListenContexts = listenContexts
        };

        await _grpcClient.SendStreamRequestAsync(ConfigBatchListenRequest.TYPE, request, cancellationToken);
    }

    /// <summary>
    /// Sends fuzzy watch request.
    /// </summary>
    public async Task<ConfigFuzzyWatchResponse?> FuzzyWatchAsync(
        List<ConfigFuzzyListenContext> contexts, bool watch = true,
        CancellationToken cancellationToken = default)
    {
        var request = new ConfigFuzzyWatchRequest
        {
            Watch = watch,
            Contexts = contexts
        };

        return await _grpcClient.SendStreamRequestWithResponseAsync<ConfigFuzzyWatchResponse>(
            ConfigFuzzyWatchRequest.TYPE, request,
            TimeSpan.FromMilliseconds(_options.DefaultTimeout),
            cancellationToken);
    }

    /// <summary>
    /// Sends fuzzy watch request via stream (fire and forget).
    /// </summary>
    public async Task SendFuzzyWatchAsync(List<ConfigFuzzyListenContext> contexts, bool watch = true,
        CancellationToken cancellationToken = default)
    {
        var request = new ConfigFuzzyWatchRequest
        {
            Watch = watch,
            Contexts = contexts
        };

        await _grpcClient.SendStreamRequestAsync(ConfigFuzzyWatchRequest.TYPE, request, cancellationToken);
    }

    private void HandlePushMessage(string type, string body)
    {
        try
        {
            switch (type)
            {
                case ConfigChangeNotifyRequest.TYPE:
                    HandleConfigChangeNotify(body);
                    break;
                    
                case ConfigFuzzyWatchChangeNotifyRequest.TYPE:
                    HandleFuzzyWatchChangeNotify(body);
                    break;
                    
                default:
                    _logger?.LogDebug("Received unknown config push type: {Type}", type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling config push message of type {Type}", type);
        }
    }

    private void HandleConfigChangeNotify(string body)
    {
        try
        {
            var request = JsonSerializer.Deserialize<ConfigChangeNotifyRequest>(body, _jsonOptions);
            if (request != null)
            {
                _logger?.LogDebug("Received config change notify: {DataId}@{Group}", 
                    request.DataId, request.Group);
                OnConfigChanged?.Invoke(request);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deserializing config change notify");
        }
    }

    private void HandleFuzzyWatchChangeNotify(string body)
    {
        try
        {
            var request = JsonSerializer.Deserialize<ConfigFuzzyWatchChangeNotifyRequest>(body, _jsonOptions);
            if (request != null)
            {
                _logger?.LogDebug("Received fuzzy watch change notify: {DataId}@{Group}, Type={ChangeType}", 
                    request.DataId, request.Group, request.ChangedType);
                OnFuzzyWatchChanged?.Invoke(request);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deserializing fuzzy watch change notify");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _grpcClient.UnregisterPushHandler("config");
        OnConfigChanged = null;
        OnFuzzyWatchChanged = null;
    }
}
