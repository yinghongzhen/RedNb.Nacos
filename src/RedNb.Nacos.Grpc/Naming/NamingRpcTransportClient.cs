using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;

namespace RedNb.Nacos.GrpcClient.Naming;

/// <summary>
/// Naming-specific gRPC transport client.
/// Handles naming service communication over gRPC.
/// </summary>
internal class NamingRpcTransportClient : IAsyncDisposable
{
    private readonly NacosGrpcClient _grpcClient;
    private readonly NacosClientOptions _options;
    private readonly ILogger? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Event fired when a service change notification is received.
    /// </summary>
    public event Action<NotifySubscriberRequest>? OnServiceChanged;

    /// <summary>
    /// Event fired when a fuzzy watch notification is received.
    /// </summary>
    public event Action<NamingFuzzyWatchNotifyRequest>? OnFuzzyWatchChanged;

    /// <summary>
    /// Event fired when connection is re-established (for redo).
    /// </summary>
    public event Func<Task>? OnReconnected;

    public NamingRpcTransportClient(NacosGrpcClient grpcClient, NacosClientOptions options, ILogger? logger = null)
    {
        _grpcClient = grpcClient;
        _options = options;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Register push handler
        _grpcClient.RegisterPushHandler("naming", HandlePushMessage);
    }

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    public bool IsConnected => _grpcClient.IsConnected;

    #region Instance Operations

    /// <summary>
    /// Registers an instance.
    /// </summary>
    public async Task<bool> RegisterInstanceAsync(string serviceName, string groupName, string? ns,
        NamingInstance instance, CancellationToken cancellationToken = default)
    {
        var request = new InstanceRequest
        {
            Namespace = ns,
            ServiceName = serviceName,
            GroupName = groupName,
            Type = InstanceOperationType.RegisterInstance,
            Instance = instance
        };

        var response = await _grpcClient.RequestAsync<InstanceResponse>(
            InstanceRequest.TYPE, request, cancellationToken);

        return response?.IsSuccess ?? false;
    }

    /// <summary>
    /// Deregisters an instance.
    /// </summary>
    public async Task<bool> DeregisterInstanceAsync(string serviceName, string groupName, string? ns,
        NamingInstance instance, CancellationToken cancellationToken = default)
    {
        var request = new InstanceRequest
        {
            Namespace = ns,
            ServiceName = serviceName,
            GroupName = groupName,
            Type = InstanceOperationType.DeregisterInstance,
            Instance = instance
        };

        var response = await _grpcClient.RequestAsync<InstanceResponse>(
            InstanceRequest.TYPE, request, cancellationToken);

        return response?.IsSuccess ?? false;
    }

    /// <summary>
    /// Batch registers instances.
    /// </summary>
    public async Task<bool> BatchRegisterInstanceAsync(string serviceName, string groupName, string? ns,
        List<NamingInstance> instances, CancellationToken cancellationToken = default)
    {
        var request = new BatchInstanceRequest
        {
            Namespace = ns,
            ServiceName = serviceName,
            GroupName = groupName,
            Type = InstanceOperationType.BatchRegisterInstance,
            Instances = instances
        };

        var response = await _grpcClient.RequestAsync<BatchInstanceResponse>(
            BatchInstanceRequest.TYPE, request, cancellationToken);

        return response?.IsSuccess ?? false;
    }

    /// <summary>
    /// Registers a persistent (non-ephemeral) instance.
    /// </summary>
    public async Task<bool> RegisterPersistentInstanceAsync(string serviceName, string groupName, string? ns,
        NamingInstance instance, CancellationToken cancellationToken = default)
    {
        var request = new PersistentInstanceRequest
        {
            Namespace = ns,
            ServiceName = serviceName,
            GroupName = groupName,
            Type = InstanceOperationType.RegisterInstance,
            Instance = instance
        };

        var response = await _grpcClient.RequestAsync<InstanceResponse>(
            PersistentInstanceRequest.TYPE, request, cancellationToken);

        return response?.IsSuccess ?? false;
    }

    /// <summary>
    /// Deregisters a persistent (non-ephemeral) instance.
    /// </summary>
    public async Task<bool> DeregisterPersistentInstanceAsync(string serviceName, string groupName, string? ns,
        NamingInstance instance, CancellationToken cancellationToken = default)
    {
        var request = new PersistentInstanceRequest
        {
            Namespace = ns,
            ServiceName = serviceName,
            GroupName = groupName,
            Type = InstanceOperationType.DeregisterInstance,
            Instance = instance
        };

        var response = await _grpcClient.RequestAsync<InstanceResponse>(
            PersistentInstanceRequest.TYPE, request, cancellationToken);

        return response?.IsSuccess ?? false;
    }

    #endregion

    #region Service Query

    /// <summary>
    /// Queries a service.
    /// </summary>
    public async Task<NamingServiceInfo?> QueryServiceAsync(string serviceName, string groupName, string? ns,
        string? clusters = null, bool healthyOnly = false, CancellationToken cancellationToken = default)
    {
        var request = new ServiceQueryRequest
        {
            Namespace = ns,
            ServiceName = serviceName,
            GroupName = groupName,
            Cluster = clusters,
            HealthyOnly = healthyOnly
        };

        var response = await _grpcClient.RequestAsync<ServiceQueryResponse>(
            ServiceQueryRequest.TYPE, request, cancellationToken);

        return response?.ServiceInfo;
    }

    #endregion

    #region Subscription

    /// <summary>
    /// Subscribes to a service.
    /// </summary>
    public async Task<NamingServiceInfo?> SubscribeServiceAsync(string serviceName, string groupName, string? ns,
        string? clusters = null, CancellationToken cancellationToken = default)
    {
        var request = new SubscribeServiceRequest
        {
            Namespace = ns,
            ServiceName = serviceName,
            GroupName = groupName,
            Clusters = clusters,
            Subscribe = true
        };

        var response = await _grpcClient.RequestAsync<SubscribeServiceResponse>(
            SubscribeServiceRequest.TYPE, request, cancellationToken);

        return response?.ServiceInfo;
    }

    /// <summary>
    /// Unsubscribes from a service.
    /// </summary>
    public async Task<bool> UnsubscribeServiceAsync(string serviceName, string groupName, string? ns,
        string? clusters = null, CancellationToken cancellationToken = default)
    {
        var request = new SubscribeServiceRequest
        {
            Namespace = ns,
            ServiceName = serviceName,
            GroupName = groupName,
            Clusters = clusters,
            Subscribe = false
        };

        var response = await _grpcClient.RequestAsync<SubscribeServiceResponse>(
            SubscribeServiceRequest.TYPE, request, cancellationToken);

        return response?.IsSuccess ?? false;
    }

    #endregion

    #region Service List

    /// <summary>
    /// Lists services.
    /// </summary>
    public async Task<ServiceListResponse?> ListServicesAsync(string? ns, string? groupName,
        int pageNo = 1, int pageSize = 10, string? selector = null,
        CancellationToken cancellationToken = default)
    {
        var request = new ServiceListRequest
        {
            Namespace = ns,
            GroupName = groupName,
            PageNo = pageNo,
            PageSize = pageSize,
            Selector = selector
        };

        return await _grpcClient.RequestAsync<ServiceListResponse>(
            ServiceListRequest.TYPE, request, cancellationToken);
    }

    #endregion

    #region Fuzzy Watch

    /// <summary>
    /// Sends a fuzzy watch request.
    /// </summary>
    public async Task<NamingFuzzyWatchResponse?> FuzzyWatchAsync(string? ns,
        string serviceNamePattern, string groupNamePattern,
        HashSet<string>? receivedGroupKeys = null, bool initializing = true,
        CancellationToken cancellationToken = default)
    {
        var request = new NamingFuzzyWatchRequest
        {
            Namespace = ns,
            ServiceNamePattern = serviceNamePattern,
            GroupNamePattern = groupNamePattern,
            Initializing = initializing,
            ReceivedGroupKeys = receivedGroupKeys ?? new HashSet<string>()
        };

        return await _grpcClient.RequestAsync<NamingFuzzyWatchResponse>(
            NamingFuzzyWatchRequest.TYPE, request, cancellationToken);
    }

    /// <summary>
    /// Sends a fuzzy watch request via stream (fire and forget).
    /// </summary>
    public async Task SendFuzzyWatchAsync(string? ns,
        string serviceNamePattern, string groupNamePattern,
        HashSet<string>? receivedGroupKeys = null, bool initializing = true,
        CancellationToken cancellationToken = default)
    {
        var request = new NamingFuzzyWatchRequest
        {
            Namespace = ns,
            ServiceNamePattern = serviceNamePattern,
            GroupNamePattern = groupNamePattern,
            Initializing = initializing,
            ReceivedGroupKeys = receivedGroupKeys ?? new HashSet<string>()
        };

        await _grpcClient.SendStreamRequestAsync(NamingFuzzyWatchRequest.TYPE, request, cancellationToken);
    }

    /// <summary>
    /// Cancels a fuzzy watch.
    /// </summary>
    public async Task<bool> CancelFuzzyWatchAsync(string? ns,
        string serviceNamePattern, string groupNamePattern,
        CancellationToken cancellationToken = default)
    {
        var request = new NamingFuzzyWatchCancelRequest
        {
            Namespace = ns,
            ServiceNamePattern = serviceNamePattern,
            GroupNamePattern = groupNamePattern
        };

        var response = await _grpcClient.RequestAsync<NamingFuzzyWatchCancelResponse>(
            NamingFuzzyWatchCancelRequest.TYPE, request, cancellationToken);

        return response?.IsSuccess ?? false;
    }

    #endregion

    #region Push Handling

    private void HandlePushMessage(string type, string body)
    {
        try
        {
            switch (type)
            {
                case NotifySubscriberRequest.TYPE:
                    HandleNotifySubscriber(body);
                    break;

                case NamingFuzzyWatchNotifyRequest.TYPE:
                    HandleFuzzyWatchNotify(body);
                    break;

                default:
                    // Try to match by partial type name
                    if (type.Contains("NotifySubscriber") || type.Contains("ServiceChanged"))
                    {
                        HandleNotifySubscriber(body);
                    }
                    else if (type.Contains("FuzzyWatch") && type.Contains("Notify"))
                    {
                        HandleFuzzyWatchNotify(body);
                    }
                    else
                    {
                        _logger?.LogDebug("Received unknown naming push type: {Type}", type);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling naming push message of type {Type}", type);
        }
    }

    private void HandleNotifySubscriber(string body)
    {
        try
        {
            var request = JsonSerializer.Deserialize<NotifySubscriberRequest>(body, _jsonOptions);
            if (request != null)
            {
                _logger?.LogDebug("Received service change notify: {Service}@{Group}",
                    request.ServiceName, request.GroupName);
                OnServiceChanged?.Invoke(request);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deserializing service change notify");
        }
    }

    private void HandleFuzzyWatchNotify(string body)
    {
        try
        {
            var request = JsonSerializer.Deserialize<NamingFuzzyWatchNotifyRequest>(body, _jsonOptions);
            if (request != null)
            {
                _logger?.LogDebug("Received fuzzy watch notify: {Service}@{Group}, Type={ChangeType}",
                    request.ServiceName, request.GroupName, request.ChangedType);
                OnFuzzyWatchChanged?.Invoke(request);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deserializing fuzzy watch notify");
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _grpcClient.UnregisterPushHandler("naming");
        OnServiceChanged = null;
        OnFuzzyWatchChanged = null;
        OnReconnected = null;
    }
}
