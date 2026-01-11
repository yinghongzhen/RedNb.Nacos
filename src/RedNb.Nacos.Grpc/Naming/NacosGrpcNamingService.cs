using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Core.Naming.FuzzyWatch;

namespace RedNb.Nacos.GrpcClient.Naming;

/// <summary>
/// Nacos naming service implementation using gRPC.
/// </summary>
public class NacosGrpcNamingService : INamingService
{
    private readonly NacosClientOptions _options;
    private readonly NacosGrpcClient _grpcClient;
    private readonly ILogger<NacosGrpcNamingService>? _logger;
    private readonly Dictionary<string, List<Action<IInstancesChangeEvent>>> _subscribeCallbacks = new();
    private readonly object _subscribeLock = new();
    private bool _disposed;
    private bool _isHealthy;

    /// <summary>
    /// Initializes a new instance of NacosGrpcNamingService.
    /// </summary>
    public NacosGrpcNamingService(NacosClientOptions options, ILogger<NacosGrpcNamingService>? logger = null)
    {
        _options = options;
        _logger = logger;
        _grpcClient = new NacosGrpcClient(options, logger);

        // Register push handler for instance changes
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

    #region RegisterInstance

    /// <inheritdoc/>
    public async Task RegisterInstanceAsync(string serviceName, string ip, int port, 
        CancellationToken cancellationToken = default)
    {
        await RegisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, ip, port, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RegisterInstanceAsync(string serviceName, string groupName, string ip, int port, 
        CancellationToken cancellationToken = default)
    {
        await RegisterInstanceAsync(serviceName, groupName, new Instance
        {
            Ip = ip,
            Port = port,
            Weight = 1.0,
            Healthy = true,
            Enabled = true,
            Ephemeral = true
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RegisterInstanceAsync(string serviceName, string ip, int port, string clusterName,
        CancellationToken cancellationToken = default)
    {
        await RegisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, ip, port, clusterName, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RegisterInstanceAsync(string serviceName, string groupName, string ip, int port, string clusterName,
        CancellationToken cancellationToken = default)
    {
        await RegisterInstanceAsync(serviceName, groupName, new Instance
        {
            Ip = ip,
            Port = port,
            Weight = 1.0,
            Healthy = true,
            Enabled = true,
            Ephemeral = true,
            ClusterName = clusterName
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RegisterInstanceAsync(string serviceName, Instance instance, 
        CancellationToken cancellationToken = default)
    {
        await RegisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, instance, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RegisterInstanceAsync(string serviceName, string groupName, Instance instance, 
        CancellationToken cancellationToken = default)
    {
        var request = new InstanceRequest
        {
            Namespace = GetNamespace(),
            ServiceName = serviceName,
            GroupName = groupName,
            Instance = new InstanceInfo
            {
                Ip = instance.Ip!,
                Port = instance.Port,
                Weight = instance.Weight,
                Healthy = instance.Healthy,
                Enabled = instance.Enabled,
                Ephemeral = instance.Ephemeral,
                ClusterName = instance.ClusterName ?? NacosConstants.DefaultClusterName,
                Metadata = instance.Metadata ?? new Dictionary<string, string>()
            }
        };

        var response = await _grpcClient.RequestAsync<InstanceResponse>(
            "InstanceRequest", request, cancellationToken);

        if (response?.Success != true)
        {
            throw new NacosException(NacosException.ServerError, "Failed to register instance");
        }

        _logger?.LogInformation("Registered instance {Ip}:{Port} for service {Service}@{Group}",
            instance.Ip, instance.Port, serviceName, groupName);
    }

    /// <inheritdoc/>
    public async Task BatchRegisterInstanceAsync(string serviceName, string groupName, List<Instance> instances,
        CancellationToken cancellationToken = default)
    {
        foreach (var instance in instances)
        {
            await RegisterInstanceAsync(serviceName, groupName, instance, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task BatchDeregisterInstanceAsync(string serviceName, string groupName, List<Instance> instances,
        CancellationToken cancellationToken = default)
    {
        foreach (var instance in instances)
        {
            await DeregisterInstanceAsync(serviceName, groupName, instance, cancellationToken);
        }
    }

    #endregion

    #region DeregisterInstance

    /// <inheritdoc/>
    public async Task DeregisterInstanceAsync(string serviceName, string ip, int port, 
        CancellationToken cancellationToken = default)
    {
        await DeregisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, ip, port, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeregisterInstanceAsync(string serviceName, string groupName, string ip, int port, 
        CancellationToken cancellationToken = default)
    {
        await DeregisterInstanceAsync(serviceName, groupName, new Instance
        {
            Ip = ip,
            Port = port
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeregisterInstanceAsync(string serviceName, string ip, int port, string clusterName,
        CancellationToken cancellationToken = default)
    {
        await DeregisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, ip, port, clusterName, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeregisterInstanceAsync(string serviceName, string groupName, string ip, int port, string clusterName,
        CancellationToken cancellationToken = default)
    {
        await DeregisterInstanceAsync(serviceName, groupName, new Instance
        {
            Ip = ip,
            Port = port,
            ClusterName = clusterName
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeregisterInstanceAsync(string serviceName, Instance instance, 
        CancellationToken cancellationToken = default)
    {
        await DeregisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, instance, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeregisterInstanceAsync(string serviceName, string groupName, Instance instance, 
        CancellationToken cancellationToken = default)
    {
        var request = new DeregisterInstanceRequest
        {
            Namespace = GetNamespace(),
            ServiceName = serviceName,
            GroupName = groupName,
            Instance = new InstanceInfo
            {
                Ip = instance.Ip!,
                Port = instance.Port,
                ClusterName = instance.ClusterName ?? NacosConstants.DefaultClusterName,
                Ephemeral = instance.Ephemeral
            }
        };

        var response = await _grpcClient.RequestAsync<InstanceResponse>(
            "DeregisterInstanceRequest", request, cancellationToken);

        if (response?.Success != true)
        {
            throw new NacosException(NacosException.ServerError, "Failed to deregister instance");
        }

        _logger?.LogInformation("Deregistered instance {Ip}:{Port} for service {Service}@{Group}",
            instance.Ip, instance.Port, serviceName, groupName);
    }

    #endregion

    #region GetAllInstances

    /// <inheritdoc/>
    public async Task<List<Instance>> GetAllInstancesAsync(string serviceName, 
        CancellationToken cancellationToken = default)
    {
        return await GetAllInstancesAsync(serviceName, NacosConstants.DefaultGroup, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName, 
        CancellationToken cancellationToken = default)
    {
        return await GetAllInstancesAsync(serviceName, groupName, new List<string>(), false, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> GetAllInstancesAsync(string serviceName, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return await GetAllInstancesAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), subscribe, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return await GetAllInstancesAsync(serviceName, groupName, new List<string>(), subscribe, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> GetAllInstancesAsync(string serviceName, List<string> clusters,
        CancellationToken cancellationToken = default)
    {
        return await GetAllInstancesAsync(serviceName, NacosConstants.DefaultGroup, clusters, false, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName, List<string> clusters,
        CancellationToken cancellationToken = default)
    {
        return await GetAllInstancesAsync(serviceName, groupName, clusters, false, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> GetAllInstancesAsync(string serviceName, List<string> clusters, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return await GetAllInstancesAsync(serviceName, NacosConstants.DefaultGroup, clusters, subscribe, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName, 
        List<string> clusters, bool subscribe, CancellationToken cancellationToken = default)
    {
        var request = new ServiceQueryRequest
        {
            Namespace = GetNamespace(),
            ServiceName = serviceName,
            GroupName = groupName,
            Clusters = string.Join(",", clusters),
            HealthyOnly = false
        };

        var response = await _grpcClient.RequestAsync<ServiceQueryResponse>(
            "ServiceQueryRequest", request, cancellationToken);

        if (response?.ServiceInfo?.Hosts == null)
        {
            return new List<Instance>();
        }

        return response.ServiceInfo.Hosts.Select(MapToInstance).ToList();
    }

    #endregion

    #region SelectInstances

    /// <inheritdoc/>
    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, bool healthy, 
        CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, healthy, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, bool healthy, 
        CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, groupName, new List<string>(), healthy, false, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, bool healthy, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), healthy, subscribe, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, bool healthy, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, groupName, new List<string>(), healthy, subscribe, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, List<string> clusters, bool healthy,
        CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, clusters, healthy, false, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, List<string> clusters, bool healthy,
        CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, groupName, clusters, healthy, false, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, List<string> clusters, bool healthy, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, clusters, healthy, subscribe, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, 
        List<string> clusters, bool healthy, bool subscribe, CancellationToken cancellationToken = default)
    {
        var request = new ServiceQueryRequest
        {
            Namespace = GetNamespace(),
            ServiceName = serviceName,
            GroupName = groupName,
            Clusters = string.Join(",", clusters),
            HealthyOnly = healthy
        };

        var response = await _grpcClient.RequestAsync<ServiceQueryResponse>(
            "ServiceQueryRequest", request, cancellationToken);

        if (response?.ServiceInfo?.Hosts == null)
        {
            return new List<Instance>();
        }

        return response.ServiceInfo.Hosts.Select(MapToInstance).ToList();
    }

    #endregion

    #region SelectOneHealthyInstance

    /// <inheritdoc/>
    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, 
        CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), false, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, 
        CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, groupName, new List<string>(), false, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), subscribe, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, groupName, new List<string>(), subscribe, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, List<string> clusters,
        CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, clusters, false, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, List<string> clusters,
        CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, groupName, clusters, false, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, List<string> clusters, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, clusters, subscribe, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, 
        List<string> clusters, bool subscribe, CancellationToken cancellationToken = default)
    {
        var instances = await SelectInstancesAsync(serviceName, groupName, clusters, true, subscribe, cancellationToken);

        if (instances.Count == 0) return null;

        // Weighted random selection
        var totalWeight = instances.Sum(i => i.Weight);
        var random = new Random().NextDouble() * totalWeight;
        var currentWeight = 0.0;

        foreach (var instance in instances)
        {
            currentWeight += instance.Weight;
            if (random <= currentWeight)
            {
                return instance;
            }
        }

        return instances.Last();
    }

    #endregion

    #region Subscribe/Unsubscribe

    /// <inheritdoc/>
    public Task SubscribeAsync(string serviceName, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return SubscribeAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), listener, cancellationToken);
    }

    /// <inheritdoc/>
    public Task SubscribeAsync(string serviceName, string groupName, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return SubscribeAsync(serviceName, groupName, new List<string>(), listener, cancellationToken);
    }

    /// <inheritdoc/>
    public Task SubscribeAsync(string serviceName, List<string> clusters, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return SubscribeAsync(serviceName, NacosConstants.DefaultGroup, clusters, listener, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SubscribeAsync(string serviceName, string groupName, List<string> clusters, 
        Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default)
    {
        var key = GetServiceKey(serviceName, groupName);

        lock (_subscribeLock)
        {
            if (!_subscribeCallbacks.TryGetValue(key, out var listeners))
            {
                listeners = new List<Action<IInstancesChangeEvent>>();
                _subscribeCallbacks[key] = listeners;
            }

            listeners.Add(listener);
        }

        // Send subscribe request
        var request = new SubscribeServiceRequest
        {
            Namespace = GetNamespace(),
            ServiceName = serviceName,
            GroupName = groupName,
            Clusters = string.Join(",", clusters),
            Subscribe = true
        };

        await _grpcClient.SendStreamRequestAsync("SubscribeServiceRequest", request);

        _logger?.LogDebug("Subscribed to service {Service}@{Group}", serviceName, groupName);
    }

    /// <inheritdoc/>
    public Task UnsubscribeAsync(string serviceName, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return UnsubscribeAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), listener, cancellationToken);
    }

    /// <inheritdoc/>
    public Task UnsubscribeAsync(string serviceName, string groupName, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return UnsubscribeAsync(serviceName, groupName, new List<string>(), listener, cancellationToken);
    }

    /// <inheritdoc/>
    public Task UnsubscribeAsync(string serviceName, List<string> clusters, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return UnsubscribeAsync(serviceName, NacosConstants.DefaultGroup, clusters, listener, cancellationToken);
    }

    /// <inheritdoc/>
    public Task UnsubscribeAsync(string serviceName, string groupName, List<string> clusters, 
        Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default)
    {
        var key = GetServiceKey(serviceName, groupName);

        lock (_subscribeLock)
        {
            if (_subscribeCallbacks.TryGetValue(key, out var listeners))
            {
                listeners.Remove(listener);
                if (listeners.Count == 0)
                {
                    _subscribeCallbacks.Remove(key);
                }
            }
        }

        _logger?.LogDebug("Unsubscribed from service {Service}@{Group}", serviceName, groupName);
        return Task.CompletedTask;
    }

    #endregion

    #region Service List

    /// <inheritdoc/>
    public async Task<ListView<string>> GetServicesOfServerAsync(int pageNo, int pageSize, 
        CancellationToken cancellationToken = default)
    {
        return await GetServicesOfServerAsync(pageNo, pageSize, NacosConstants.DefaultGroup, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ListView<string>> GetServicesOfServerAsync(int pageNo, int pageSize, string groupName, 
        CancellationToken cancellationToken = default)
    {
        var request = new ServiceListRequest
        {
            Namespace = GetNamespace(),
            GroupName = groupName,
            PageNo = pageNo,
            PageSize = pageSize
        };

        var response = await _grpcClient.RequestAsync<ServiceListResponse>(
            "ServiceListRequest", request, cancellationToken);

        return new ListView<string>
        {
            Count = response?.Count ?? 0,
            Data = response?.ServiceNames ?? new List<string>()
        };
    }

    /// <inheritdoc/>
    public async Task<List<ServiceInfo>> GetSubscribeServicesAsync(CancellationToken cancellationToken = default)
    {
        var services = new List<ServiceInfo>();

        lock (_subscribeLock)
        {
            foreach (var key in _subscribeCallbacks.Keys)
            {
                var parts = key.Split("@@");
                services.Add(new ServiceInfo
                {
                    Name = parts.Length > 1 ? parts[1] : parts[0],
                    GroupName = parts.Length > 1 ? parts[0] : NacosConstants.DefaultGroup
                });
            }
        }

        return await Task.FromResult(services);
    }

    #endregion

    #region Server Status

    /// <inheritdoc/>
    public string GetServerStatus()
    {
        return _isHealthy && _grpcClient.IsConnected ? "UP" : "DOWN";
    }

    /// <inheritdoc/>
    public async Task ShutdownAsync()
    {
        await DisposeAsync();
    }

    #endregion

    #region Fuzzy Watch

    public Task FuzzyWatchAsync(string serviceNamePattern, INamingFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        return FuzzyWatchAsync(serviceNamePattern, "*", watcher, cancellationToken);
    }

    public Task FuzzyWatchAsync(string serviceNamePattern, string groupNamePattern, 
        INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        // TODO: Implement fuzzy watch via gRPC stream
        _logger?.LogWarning("Fuzzy watch is not yet implemented in gRPC client");
        return Task.CompletedTask;
    }

    public Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string serviceNamePattern, 
        INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        return FuzzyWatchWithGroupKeysAsync(serviceNamePattern, "*", watcher, cancellationToken);
    }

    public Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string serviceNamePattern, string groupNamePattern, 
        INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        // TODO: Implement fuzzy watch via gRPC stream
        _logger?.LogWarning("Fuzzy watch is not yet implemented in gRPC client");
        return Task.FromResult<ISet<string>>(new HashSet<string>());
    }

    public Task CancelFuzzyWatchAsync(string serviceNamePattern, INamingFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        return CancelFuzzyWatchAsync(serviceNamePattern, "*", watcher, cancellationToken);
    }

    public Task CancelFuzzyWatchAsync(string serviceNamePattern, string groupNamePattern, 
        INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        // TODO: Implement fuzzy watch via gRPC stream
        _logger?.LogWarning("Fuzzy watch is not yet implemented in gRPC client");
        return Task.CompletedTask;
    }

    #endregion

    private void HandlePushMessage(string type, string body)
    {
        if (type.Contains("ServiceChanged") || type.Contains("NotifySubscriber"))
        {
            try
            {
                var notification = System.Text.Json.JsonSerializer.Deserialize<ServiceChangedNotification>(body);
                if (notification != null)
                {
                    NotifySubscribers(notification);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling service change push");
            }
        }
    }

    private void NotifySubscribers(ServiceChangedNotification notification)
    {
        var key = GetServiceKey(notification.ServiceName, notification.GroupName);

        List<Action<IInstancesChangeEvent>>? listeners;
        lock (_subscribeLock)
        {
            if (!_subscribeCallbacks.TryGetValue(key, out listeners) || listeners.Count == 0)
            {
                return;
            }

            listeners = new List<Action<IInstancesChangeEvent>>(listeners);
        }

        var instances = notification.Hosts?.Select(MapToInstance).ToList() ?? new List<Instance>();

        var changeEvent = new GrpcInstancesChangeEvent
        {
            ServiceName = notification.ServiceName,
            GroupName = notification.GroupName,
            Clusters = notification.Clusters,
            Instances = instances
        };

        foreach (var listener in listeners)
        {
            try
            {
                listener(changeEvent);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error notifying subscriber for {Service}@{Group}",
                    notification.ServiceName, notification.GroupName);
            }
        }
    }

    private static Instance MapToInstance(InstanceInfo info)
    {
        return new Instance
        {
            InstanceId = info.InstanceId,
            Ip = info.Ip,
            Port = info.Port,
            Weight = info.Weight,
            Healthy = info.Healthy,
            Enabled = info.Enabled,
            Ephemeral = info.Ephemeral,
            ClusterName = info.ClusterName,
            ServiceName = info.ServiceName,
            Metadata = info.Metadata ?? new Dictionary<string, string>()
        };
    }

    private static string GetServiceKey(string serviceName, string groupName)
    {
        return $"{groupName}@@{serviceName}";
    }

    private string? GetNamespace()
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

    private class InstanceRequest
    {
        public string? Namespace { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public InstanceInfo Instance { get; set; } = new();
    }

    private class DeregisterInstanceRequest
    {
        public string? Namespace { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public InstanceInfo Instance { get; set; } = new();
    }

    private class InstanceResponse
    {
        public bool Success { get; set; }
    }

    private class ServiceQueryRequest
    {
        public string? Namespace { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string? Clusters { get; set; }
        public bool HealthyOnly { get; set; }
    }

    private class ServiceQueryResponse
    {
        public ServiceInfoDto? ServiceInfo { get; set; }
    }

    private class ServiceInfoDto
    {
        public string? Name { get; set; }
        public string? GroupName { get; set; }
        public List<InstanceInfo>? Hosts { get; set; }
    }

    private class SubscribeServiceRequest
    {
        public string? Namespace { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string? Clusters { get; set; }
        public bool Subscribe { get; set; }
    }

    private class ServiceListRequest
    {
        public string? Namespace { get; set; }
        public string? GroupName { get; set; }
        public int PageNo { get; set; }
        public int PageSize { get; set; }
    }

    private class ServiceListResponse
    {
        public int Count { get; set; }
        public List<string>? ServiceNames { get; set; }
    }

    private class InstanceInfo
    {
        public string? InstanceId { get; set; }
        public string Ip { get; set; } = string.Empty;
        public int Port { get; set; }
        public double Weight { get; set; } = 1.0;
        public bool Healthy { get; set; } = true;
        public bool Enabled { get; set; } = true;
        public bool Ephemeral { get; set; } = true;
        public string? ClusterName { get; set; }
        public string? ServiceName { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    private class ServiceChangedNotification
    {
        public string ServiceName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string? Clusters { get; set; }
        public List<InstanceInfo>? Hosts { get; set; }
    }

    private class GrpcInstancesChangeEvent : IInstancesChangeEvent
    {
        public string ServiceName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string? Clusters { get; set; }
        public List<Instance> Instances { get; set; } = new();
        public List<Instance>? AddedInstances { get; set; }
        public List<Instance>? RemovedInstances { get; set; }
        public List<Instance>? ModifiedInstances { get; set; }
    }

    #endregion
}
