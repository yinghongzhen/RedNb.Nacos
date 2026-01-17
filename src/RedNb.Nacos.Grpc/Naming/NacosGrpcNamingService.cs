using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Core.Naming.FuzzyWatch;
using RedNb.Nacos.Core.Naming.Selector;
using RedNb.Nacos.Utils;

namespace RedNb.Nacos.GrpcClient.Naming;

/// <summary>
/// Nacos naming service implementation using gRPC.
/// Provides service registration, discovery, and subscription with server push support.
/// </summary>
public class NacosGrpcNamingService : INamingService
{
    private readonly NacosClientOptions _options;
    private readonly NacosGrpcClient _grpcClient;
    private readonly NamingRpcTransportClient _transportClient;
    private readonly NamingServiceInfoHolder _serviceInfoHolder;
    private readonly NamingGrpcRedoService _redoService;
    private readonly ILogger<NacosGrpcNamingService>? _logger;

    // Subscription management
    private readonly ConcurrentDictionary<string, List<Action<IInstancesChangeEvent>>> _subscribeCallbacks = new();
    private readonly ConcurrentDictionary<string, Action<IInstancesChangeEvent>> _selectorListeners = new();
    private readonly object _subscribeLock = new();

    // Fuzzy watch management
    private readonly ConcurrentDictionary<string, FuzzyWatchEntry> _fuzzyWatchers = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _knownServices = new();

    // Background tasks
    private readonly CancellationTokenSource _cts;
    private Task? _updateTask;

    private bool _disposed;
    private bool _isHealthy;
    private bool _initialized;

    /// <summary>
    /// Update interval for service info (milliseconds).
    /// </summary>
    private const int UpdateIntervalMs = 10000;

    public NacosGrpcNamingService(NacosClientOptions options, ILogger<NacosGrpcNamingService>? logger = null)
    {
        _options = options;
        _logger = logger;
        _grpcClient = new NacosGrpcClient(options, logger);
        _transportClient = new NamingRpcTransportClient(_grpcClient, options, logger);
        _serviceInfoHolder = new NamingServiceInfoHolder(options, logger);
        _redoService = new NamingGrpcRedoService(_transportClient, GetNamespace(), logger);
        _cts = new CancellationTokenSource();

        // Register for service change notifications
        _transportClient.OnServiceChanged += HandleServiceChangeNotify;
        _transportClient.OnFuzzyWatchChanged += HandleFuzzyWatchNotify;
        _serviceInfoHolder.OnServiceInfoChanged += HandleServiceInfoChanged;
    }

    /// <summary>
    /// Initializes the gRPC connection.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        await _grpcClient.ConnectAsync(cancellationToken);
        _isHealthy = true;
        _initialized = true;

        // Start background update task
        _updateTask = UpdateServiceInfoLoopAsync(_cts.Token);

        _logger?.LogInformation("NacosGrpcNamingService initialized");
    }

    #region Instance Registration

    public Task RegisterInstanceAsync(string serviceName, string ip, int port,
        CancellationToken cancellationToken = default)
    {
        return RegisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, ip, port,
            NacosConstants.DefaultClusterName, cancellationToken);
    }

    public Task RegisterInstanceAsync(string serviceName, string groupName, string ip, int port,
        CancellationToken cancellationToken = default)
    {
        return RegisterInstanceAsync(serviceName, groupName, ip, port,
            NacosConstants.DefaultClusterName, cancellationToken);
    }

    public Task RegisterInstanceAsync(string serviceName, string ip, int port, string clusterName,
        CancellationToken cancellationToken = default)
    {
        return RegisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, ip, port,
            clusterName, cancellationToken);
    }

    public Task RegisterInstanceAsync(string serviceName, string groupName, string ip, int port,
        string clusterName, CancellationToken cancellationToken = default)
    {
        var instance = new Instance
        {
            Ip = ip,
            Port = port,
            Weight = 1.0,
            Healthy = true,
            Enabled = true,
            Ephemeral = true,
            ClusterName = clusterName
        };
        return RegisterInstanceAsync(serviceName, groupName, instance, cancellationToken);
    }

    public Task RegisterInstanceAsync(string serviceName, Instance instance,
        CancellationToken cancellationToken = default)
    {
        return RegisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, instance, cancellationToken);
    }

    public async Task RegisterInstanceAsync(string serviceName, string groupName, Instance instance,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        instance.Validate();
        groupName = GetGroupOrDefault(groupName);

        var namingInstance = NamingServiceInfoHolder.MapToNamingInstance(instance);

        bool success;
        if (instance.Ephemeral)
        {
            success = await _transportClient.RegisterInstanceAsync(
                serviceName, groupName, GetNamespace(), namingInstance, cancellationToken);
        }
        else
        {
            success = await _transportClient.RegisterPersistentInstanceAsync(
                serviceName, groupName, GetNamespace(), namingInstance, cancellationToken);
        }

        if (!success)
        {
            throw new NacosException(NacosException.ServerError,
                $"Failed to register instance {instance.Ip}:{instance.Port}");
        }

        // Cache for redo
        _redoService.CacheRegisteredInstance(serviceName, groupName, instance);

        _logger?.LogInformation("Registered instance {Ip}:{Port} for service {Service}@{Group}",
            instance.Ip, instance.Port, serviceName, groupName);
    }

    public async Task BatchRegisterInstanceAsync(string serviceName, string groupName,
        List<Instance> instances, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        groupName = GetGroupOrDefault(groupName);

        var namingInstances = instances.Select(NamingServiceInfoHolder.MapToNamingInstance).ToList();

        var success = await _transportClient.BatchRegisterInstanceAsync(
            serviceName, groupName, GetNamespace(), namingInstances, cancellationToken);

        if (!success)
        {
            throw new NacosException(NacosException.ServerError,
                $"Failed to batch register {instances.Count} instances");
        }

        // Cache for redo
        _redoService.CacheBatchRegisteredInstances(serviceName, groupName, instances);

        _logger?.LogInformation("Batch registered {Count} instances for service {Service}@{Group}",
            instances.Count, serviceName, groupName);
    }

    public async Task BatchDeregisterInstanceAsync(string serviceName, string groupName,
        List<Instance> instances, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        groupName = GetGroupOrDefault(groupName);

        // Deregister one by one (gRPC doesn't have batch deregister)
        foreach (var instance in instances)
        {
            await DeregisterInstanceAsync(serviceName, groupName, instance, cancellationToken);
        }

        // Remove from redo cache
        _redoService.RemoveBatchRegisteredInstances(serviceName, groupName);
    }

    #endregion

    #region Instance Deregistration

    public Task DeregisterInstanceAsync(string serviceName, string ip, int port,
        CancellationToken cancellationToken = default)
    {
        return DeregisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, ip, port, cancellationToken);
    }

    public Task DeregisterInstanceAsync(string serviceName, string groupName, string ip, int port,
        CancellationToken cancellationToken = default)
    {
        return DeregisterInstanceAsync(serviceName, groupName, ip, port,
            NacosConstants.DefaultClusterName, cancellationToken);
    }

    public Task DeregisterInstanceAsync(string serviceName, string ip, int port, string clusterName,
        CancellationToken cancellationToken = default)
    {
        return DeregisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, ip, port,
            clusterName, cancellationToken);
    }

    public Task DeregisterInstanceAsync(string serviceName, string groupName, string ip, int port,
        string clusterName, CancellationToken cancellationToken = default)
    {
        var instance = new Instance
        {
            Ip = ip,
            Port = port,
            ClusterName = clusterName
        };
        return DeregisterInstanceAsync(serviceName, groupName, instance, cancellationToken);
    }

    public Task DeregisterInstanceAsync(string serviceName, Instance instance,
        CancellationToken cancellationToken = default)
    {
        return DeregisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, instance, cancellationToken);
    }

    public async Task DeregisterInstanceAsync(string serviceName, string groupName, Instance instance,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        groupName = GetGroupOrDefault(groupName);

        var namingInstance = NamingServiceInfoHolder.MapToNamingInstance(instance);

        bool success;
        if (instance.Ephemeral)
        {
            success = await _transportClient.DeregisterInstanceAsync(
                serviceName, groupName, GetNamespace(), namingInstance, cancellationToken);
        }
        else
        {
            success = await _transportClient.DeregisterPersistentInstanceAsync(
                serviceName, groupName, GetNamespace(), namingInstance, cancellationToken);
        }

        if (!success)
        {
            throw new NacosException(NacosException.ServerError,
                $"Failed to deregister instance {instance.Ip}:{instance.Port}");
        }

        // Remove from redo cache
        _redoService.RemoveRegisteredInstance(serviceName, groupName, instance);

        _logger?.LogInformation("Deregistered instance {Ip}:{Port} from service {Service}@{Group}",
            instance.Ip, instance.Port, serviceName, groupName);
    }

    #endregion

    #region Instance Query

    public Task<List<Instance>> GetAllInstancesAsync(string serviceName,
        CancellationToken cancellationToken = default)
    {
        return GetAllInstancesAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), true, cancellationToken);
    }

    public Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName,
        CancellationToken cancellationToken = default)
    {
        return GetAllInstancesAsync(serviceName, groupName, new List<string>(), true, cancellationToken);
    }

    public Task<List<Instance>> GetAllInstancesAsync(string serviceName, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return GetAllInstancesAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), subscribe, cancellationToken);
    }

    public Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return GetAllInstancesAsync(serviceName, groupName, new List<string>(), subscribe, cancellationToken);
    }

    public Task<List<Instance>> GetAllInstancesAsync(string serviceName, List<string> clusters,
        CancellationToken cancellationToken = default)
    {
        return GetAllInstancesAsync(serviceName, NacosConstants.DefaultGroup, clusters, true, cancellationToken);
    }

    public Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName, List<string> clusters,
        CancellationToken cancellationToken = default)
    {
        return GetAllInstancesAsync(serviceName, groupName, clusters, true, cancellationToken);
    }

    public Task<List<Instance>> GetAllInstancesAsync(string serviceName, List<string> clusters, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return GetAllInstancesAsync(serviceName, NacosConstants.DefaultGroup, clusters, subscribe, cancellationToken);
    }

    public async Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName,
        List<string> clusters, bool subscribe, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        groupName = GetGroupOrDefault(groupName);
        var clusterString = NacosUtils.GetClusterString(clusters);

        NamingServiceInfo? serviceInfo;

        if (subscribe)
        {
            // Try cache first
            serviceInfo = _serviceInfoHolder.GetServiceInfo(serviceName, groupName, clusterString);

            if (serviceInfo == null)
            {
                // Subscribe to get service info
                serviceInfo = await _transportClient.SubscribeServiceAsync(
                    serviceName, groupName, GetNamespace(), clusterString, cancellationToken);

                if (serviceInfo != null)
                {
                    _serviceInfoHolder.ProcessServiceInfo(serviceInfo);
                    _redoService.CacheSubscribedService(serviceName, groupName, clusterString);
                }
            }
        }
        else
        {
            // Query directly without subscription
            serviceInfo = await _transportClient.QueryServiceAsync(
                serviceName, groupName, GetNamespace(), clusterString, false, cancellationToken);
        }

        if (serviceInfo?.Hosts == null)
        {
            return new List<Instance>();
        }

        _isHealthy = true;
        return serviceInfo.Hosts.Select(NamingServiceInfoHolder.MapToInstance).ToList();
    }

    public Task<List<Instance>> SelectInstancesAsync(string serviceName, bool healthy,
        CancellationToken cancellationToken = default)
    {
        return SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), healthy, true, cancellationToken);
    }

    public Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, bool healthy,
        CancellationToken cancellationToken = default)
    {
        return SelectInstancesAsync(serviceName, groupName, new List<string>(), healthy, true, cancellationToken);
    }

    public Task<List<Instance>> SelectInstancesAsync(string serviceName, bool healthy, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), healthy, subscribe, cancellationToken);
    }

    public Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, bool healthy, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return SelectInstancesAsync(serviceName, groupName, new List<string>(), healthy, subscribe, cancellationToken);
    }

    public Task<List<Instance>> SelectInstancesAsync(string serviceName, List<string> clusters, bool healthy,
        CancellationToken cancellationToken = default)
    {
        return SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, clusters, healthy, true, cancellationToken);
    }

    public Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, List<string> clusters, bool healthy,
        CancellationToken cancellationToken = default)
    {
        return SelectInstancesAsync(serviceName, groupName, clusters, healthy, true, cancellationToken);
    }

    public Task<List<Instance>> SelectInstancesAsync(string serviceName, List<string> clusters, bool healthy, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, clusters, healthy, subscribe, cancellationToken);
    }

    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName,
        List<string> clusters, bool healthy, bool subscribe, CancellationToken cancellationToken = default)
    {
        var instances = await GetAllInstancesAsync(serviceName, groupName, clusters, subscribe, cancellationToken);
        return instances.Where(i => i.Healthy == healthy && i.Enabled && i.Weight > 0).ToList();
    }

    public Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName,
        CancellationToken cancellationToken = default)
    {
        return SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), true, cancellationToken);
    }

    public Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName,
        CancellationToken cancellationToken = default)
    {
        return SelectOneHealthyInstanceAsync(serviceName, groupName, new List<string>(), true, cancellationToken);
    }

    public Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), subscribe, cancellationToken);
    }

    public Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return SelectOneHealthyInstanceAsync(serviceName, groupName, new List<string>(), subscribe, cancellationToken);
    }

    public Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, List<string> clusters,
        CancellationToken cancellationToken = default)
    {
        return SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, clusters, true, cancellationToken);
    }

    public Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, List<string> clusters,
        CancellationToken cancellationToken = default)
    {
        return SelectOneHealthyInstanceAsync(serviceName, groupName, clusters, true, cancellationToken);
    }

    public Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, List<string> clusters, bool subscribe,
        CancellationToken cancellationToken = default)
    {
        return SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, clusters, subscribe, cancellationToken);
    }

    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName,
        List<string> clusters, bool subscribe, CancellationToken cancellationToken = default)
    {
        var instances = await SelectInstancesAsync(serviceName, groupName, clusters, true, subscribe, cancellationToken);
        return SelectByRandomWeight(instances);
    }

    #endregion

    #region Subscription

    public Task SubscribeAsync(string serviceName, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return SubscribeAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), listener, cancellationToken);
    }

    public Task SubscribeAsync(string serviceName, string groupName, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return SubscribeAsync(serviceName, groupName, new List<string>(), listener, cancellationToken);
    }

    public Task SubscribeAsync(string serviceName, List<string> clusters, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return SubscribeAsync(serviceName, NacosConstants.DefaultGroup, clusters, listener, cancellationToken);
    }

    public async Task SubscribeAsync(string serviceName, string groupName, List<string> clusters,
        Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        groupName = GetGroupOrDefault(groupName);
        var clusterString = NacosUtils.GetClusterString(clusters);
        var key = GetServiceKey(serviceName, groupName, clusterString);

        // Add listener
        lock (_subscribeLock)
        {
            if (!_subscribeCallbacks.TryGetValue(key, out var listeners))
            {
                listeners = new List<Action<IInstancesChangeEvent>>();
                _subscribeCallbacks[key] = listeners;
            }
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        // Subscribe to server
        var serviceInfo = await _transportClient.SubscribeServiceAsync(
            serviceName, groupName, GetNamespace(), clusterString, cancellationToken);

        if (serviceInfo != null)
        {
            _serviceInfoHolder.ProcessServiceInfo(serviceInfo);
        }

        // Cache for redo
        _redoService.CacheSubscribedService(serviceName, groupName, clusterString);

        _logger?.LogDebug("Subscribed to service {Service}@{Group}", serviceName, groupName);
    }

    public Task SubscribeAsync(string serviceName, INamingSelector selector,
        Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default)
    {
        return SubscribeAsync(serviceName, NacosConstants.DefaultGroup, selector, listener, cancellationToken);
    }

    public async Task SubscribeAsync(string serviceName, string groupName, INamingSelector selector,
        Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        groupName = GetGroupOrDefault(groupName);
        var key = GetServiceKey(serviceName, groupName, "");
        var listenerKey = $"{serviceName}@@{groupName}@@{listener.GetHashCode()}";

        // Create a filtered listener
        Action<IInstancesChangeEvent> filteredListener = evt =>
        {
            if (selector != null)
            {
                var context = new NamingContext
                {
                    ServiceName = serviceName,
                    GroupName = groupName,
                    Instances = evt.Instances,
                    HealthyOnly = false
                };
                var result = selector.Select(context);

                evt = new GrpcInstancesChangeEvent
                {
                    ServiceName = evt.ServiceName,
                    GroupName = evt.GroupName,
                    Clusters = evt.Clusters,
                    Instances = result.Instances,
                    AddedInstances = evt.AddedInstances?.Where(i => result.Instances.Contains(i)).ToList(),
                    RemovedInstances = evt.RemovedInstances,
                    ModifiedInstances = evt.ModifiedInstances?.Where(i => result.Instances.Contains(i)).ToList()
                };
            }
            listener(evt);
        };

        lock (_subscribeLock)
        {
            if (!_subscribeCallbacks.TryGetValue(key, out var listeners))
            {
                listeners = new List<Action<IInstancesChangeEvent>>();
                _subscribeCallbacks[key] = listeners;
            }
            listeners.Add(filteredListener);
            _selectorListeners[listenerKey] = filteredListener;
        }

        // Subscribe to server
        var serviceInfo = await _transportClient.SubscribeServiceAsync(
            serviceName, groupName, GetNamespace(), null, cancellationToken);

        if (serviceInfo != null)
        {
            _serviceInfoHolder.ProcessServiceInfo(serviceInfo);
        }

        _redoService.CacheSubscribedService(serviceName, groupName, null);

        _logger?.LogDebug("Subscribed to service {Service}@{Group} with selector", serviceName, groupName);
    }

    public Task UnsubscribeAsync(string serviceName, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return UnsubscribeAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), listener, cancellationToken);
    }

    public Task UnsubscribeAsync(string serviceName, string groupName, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return UnsubscribeAsync(serviceName, groupName, new List<string>(), listener, cancellationToken);
    }

    public Task UnsubscribeAsync(string serviceName, List<string> clusters, Action<IInstancesChangeEvent> listener,
        CancellationToken cancellationToken = default)
    {
        return UnsubscribeAsync(serviceName, NacosConstants.DefaultGroup, clusters, listener, cancellationToken);
    }

    public async Task UnsubscribeAsync(string serviceName, string groupName, List<string> clusters,
        Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default)
    {
        groupName = GetGroupOrDefault(groupName);
        var clusterString = NacosUtils.GetClusterString(clusters);
        var key = GetServiceKey(serviceName, groupName, clusterString);

        bool shouldUnsubscribe = false;

        lock (_subscribeLock)
        {
            if (_subscribeCallbacks.TryGetValue(key, out var listeners))
            {
                listeners.Remove(listener);
                if (listeners.Count == 0)
                {
                    _subscribeCallbacks.TryRemove(key, out _);
                    shouldUnsubscribe = true;
                }
            }
        }

        if (shouldUnsubscribe)
        {
            await _transportClient.UnsubscribeServiceAsync(
                serviceName, groupName, GetNamespace(), clusterString, cancellationToken);
            _redoService.RemoveSubscribedService(serviceName, groupName, clusterString);
        }

        _logger?.LogDebug("Unsubscribed from service {Service}@{Group}", serviceName, groupName);
    }

    public Task UnsubscribeAsync(string serviceName, INamingSelector selector,
        Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default)
    {
        return UnsubscribeAsync(serviceName, NacosConstants.DefaultGroup, selector, listener, cancellationToken);
    }

    public async Task UnsubscribeAsync(string serviceName, string groupName, INamingSelector selector,
        Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default)
    {
        groupName = GetGroupOrDefault(groupName);
        var key = GetServiceKey(serviceName, groupName, "");
        var listenerKey = $"{serviceName}@@{groupName}@@{listener.GetHashCode()}";

        bool shouldUnsubscribe = false;

        lock (_subscribeLock)
        {
            if (_selectorListeners.TryRemove(listenerKey, out var filteredListener))
            {
                if (_subscribeCallbacks.TryGetValue(key, out var listeners))
                {
                    listeners.Remove(filteredListener);
                    if (listeners.Count == 0)
                    {
                        _subscribeCallbacks.TryRemove(key, out _);
                        shouldUnsubscribe = true;
                    }
                }
            }
        }

        if (shouldUnsubscribe)
        {
            await _transportClient.UnsubscribeServiceAsync(
                serviceName, groupName, GetNamespace(), null, cancellationToken);
            _redoService.RemoveSubscribedService(serviceName, groupName, null);
        }

        _logger?.LogDebug("Unsubscribed from service {Service}@{Group} with selector", serviceName, groupName);
    }

    #endregion

    #region Service List

    public Task<ListView<string>> GetServicesOfServerAsync(int pageNo, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return GetServicesOfServerAsync(pageNo, pageSize, NacosConstants.DefaultGroup, cancellationToken);
    }

    public async Task<ListView<string>> GetServicesOfServerAsync(int pageNo, int pageSize, string groupName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        groupName = GetGroupOrDefault(groupName);

        var response = await _transportClient.ListServicesAsync(
            GetNamespace(), groupName, pageNo, pageSize, null, cancellationToken);

        return new ListView<string>
        {
            Count = response?.Count ?? 0,
            Data = response?.ServiceNames ?? new List<string>()
        };
    }

    public Task<ListView<string>> GetServicesOfServerAsync(int pageNo, int pageSize, INamingSelector selector,
        CancellationToken cancellationToken = default)
    {
        return GetServicesOfServerAsync(pageNo, pageSize, NacosConstants.DefaultGroup, selector, cancellationToken);
    }

    public async Task<ListView<string>> GetServicesOfServerAsync(int pageNo, int pageSize, string groupName,
        INamingSelector selector, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        groupName = GetGroupOrDefault(groupName);

        string? selectorJson = null;
        if (selector != null)
        {
            selectorJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = selector.Type,
                expression = selector.Expression
            });
        }

        var response = await _transportClient.ListServicesAsync(
            GetNamespace(), groupName, pageNo, pageSize, selectorJson, cancellationToken);

        return new ListView<string>
        {
            Count = response?.Count ?? 0,
            Data = response?.ServiceNames ?? new List<string>()
        };
    }

    public Task<List<ServiceInfo>> GetSubscribeServicesAsync(CancellationToken cancellationToken = default)
    {
        var services = new List<ServiceInfo>();

        foreach (var data in _redoService.GetSubscribedServices())
        {
            var cachedInfo = _serviceInfoHolder.GetServiceInfo(data.ServiceName, data.GroupName, data.Clusters);
            if (cachedInfo != null)
            {
                services.Add(new ServiceInfo
                {
                    Name = cachedInfo.Name,
                    GroupName = cachedInfo.GroupName,
                    Clusters = cachedInfo.Clusters,
                    Hosts = cachedInfo.Hosts?.Select(NamingServiceInfoHolder.MapToInstance).ToList() ?? new List<Instance>()
                });
            }
        }

        return Task.FromResult(services);
    }

    #endregion

    #region Server Status

    public string GetServerStatus()
    {
        return _isHealthy && _grpcClient.IsConnected ? "UP" : "DOWN";
    }

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

    public async Task FuzzyWatchAsync(string serviceNamePattern, string groupNamePattern,
        INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var watchKey = GetFuzzyWatchKey(serviceNamePattern, groupNamePattern);

        // Add watcher
        var entry = _fuzzyWatchers.GetOrAdd(watchKey, _ => new FuzzyWatchEntry
        {
            ServiceNamePattern = serviceNamePattern,
            GroupNamePattern = groupNamePattern
        });
        entry.AddWatcher(watcher);

        // Send watch request
        await _transportClient.SendFuzzyWatchAsync(
            GetNamespace(), serviceNamePattern, groupNamePattern,
            null, true, cancellationToken);

        _logger?.LogDebug("Added fuzzy watch for service={ServicePattern}, group={GroupPattern}",
            serviceNamePattern, groupNamePattern);
    }

    public Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string serviceNamePattern,
        INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        return FuzzyWatchWithGroupKeysAsync(serviceNamePattern, "*", watcher, cancellationToken);
    }

    public async Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string serviceNamePattern, string groupNamePattern,
        INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var watchKey = GetFuzzyWatchKey(serviceNamePattern, groupNamePattern);

        // Add watcher
        var entry = _fuzzyWatchers.GetOrAdd(watchKey, _ => new FuzzyWatchEntry
        {
            ServiceNamePattern = serviceNamePattern,
            GroupNamePattern = groupNamePattern
        });
        entry.AddWatcher(watcher);

        // Send watch request and get matching keys
        var response = await _transportClient.FuzzyWatchAsync(
            GetNamespace(), serviceNamePattern, groupNamePattern,
            null, true, cancellationToken);

        var matchedKeys = new HashSet<string>();

        if (response?.ServiceChangedList != null)
        {
            foreach (var item in response.ServiceChangedList)
            {
                var key = NamingServiceInfo.GetKey(item.ServiceName, item.GroupName, "");
                matchedKeys.Add(key);
                _knownServices.TryAdd(key, new HashSet<string> { watchKey });
            }
        }

        _logger?.LogDebug("Added fuzzy watch with keys for service={ServicePattern}, group={GroupPattern}, matched={Count}",
            serviceNamePattern, groupNamePattern, matchedKeys.Count);

        return matchedKeys;
    }

    public Task CancelFuzzyWatchAsync(string serviceNamePattern, INamingFuzzyWatchEventWatcher watcher,
        CancellationToken cancellationToken = default)
    {
        return CancelFuzzyWatchAsync(serviceNamePattern, "*", watcher, cancellationToken);
    }

    public async Task CancelFuzzyWatchAsync(string serviceNamePattern, string groupNamePattern,
        INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        var watchKey = GetFuzzyWatchKey(serviceNamePattern, groupNamePattern);

        if (_fuzzyWatchers.TryGetValue(watchKey, out var entry))
        {
            entry.RemoveWatcher(watcher);

            if (!entry.HasWatchers)
            {
                _fuzzyWatchers.TryRemove(watchKey, out _);

                // Send cancel request
                await _transportClient.CancelFuzzyWatchAsync(
                    GetNamespace(), serviceNamePattern, groupNamePattern, cancellationToken);
            }
        }

        _logger?.LogDebug("Cancelled fuzzy watch for service={ServicePattern}, group={GroupPattern}",
            serviceNamePattern, groupNamePattern);
    }

    #endregion

    #region Private Methods

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await InitializeAsync(cancellationToken);
        }
    }

    private async Task UpdateServiceInfoLoopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting service info update loop");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(UpdateIntervalMs, cancellationToken);

                if (!_grpcClient.IsConnected)
                {
                    continue;
                }

                // Update all subscribed services
                foreach (var data in _redoService.GetSubscribedServices())
                {
                    try
                    {
                        var serviceInfo = await _transportClient.QueryServiceAsync(
                            data.ServiceName, data.GroupName, GetNamespace(),
                            data.Clusters, false, cancellationToken);

                        if (serviceInfo != null)
                        {
                            _serviceInfoHolder.ProcessServiceInfo(serviceInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to update service info for {Service}@{Group}",
                            data.ServiceName, data.GroupName);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error in service info update loop");
            }
        }

        _logger?.LogInformation("Service info update loop stopped");
    }

    private void HandleServiceChangeNotify(NotifySubscriberRequest request)
    {
        _logger?.LogDebug("Received service change notify for {Service}@{Group}",
            request.ServiceName, request.GroupName);

        if (request.ServiceInfo != null)
        {
            _serviceInfoHolder.ProcessServiceInfo(request.ServiceInfo);
        }
    }

    private void HandleServiceInfoChanged(NamingServiceInfo newInfo, NamingServiceInfo? oldInfo)
    {
        var key = newInfo.GetKey();

        // Compute changes
        var (added, removed, modified) = _serviceInfoHolder.ComputeChanges(oldInfo, newInfo);

        // Create change event
        var changeEvent = new GrpcInstancesChangeEvent
        {
            ServiceName = newInfo.Name ?? "",
            GroupName = newInfo.GroupName ?? NacosConstants.DefaultGroup,
            Clusters = newInfo.Clusters,
            Instances = newInfo.Hosts?.Select(NamingServiceInfoHolder.MapToInstance).ToList() ?? new List<Instance>(),
            AddedInstances = added,
            RemovedInstances = removed,
            ModifiedInstances = modified
        };

        // Notify listeners
        NotifySubscribers(key, changeEvent);
    }

    private void NotifySubscribers(string key, GrpcInstancesChangeEvent changeEvent)
    {
        List<Action<IInstancesChangeEvent>>? listeners;

        lock (_subscribeLock)
        {
            if (!_subscribeCallbacks.TryGetValue(key, out listeners) || listeners.Count == 0)
            {
                return;
            }
            listeners = new List<Action<IInstancesChangeEvent>>(listeners);
        }

        foreach (var listener in listeners)
        {
            try
            {
                listener(changeEvent);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error notifying subscriber for {Key}", key);
            }
        }
    }

    private void HandleFuzzyWatchNotify(NamingFuzzyWatchNotifyRequest request)
    {
        _logger?.LogDebug("Received fuzzy watch notify for {Service}@{Group}, Type={ChangeType}",
            request.ServiceName, request.GroupName, request.ChangedType);

        // Update known services
        var serviceKey = NamingServiceInfo.GetKey(request.ServiceName, request.GroupName, "");
        if (request.ChangedType == NamingChangedType.AddService)
        {
            _knownServices.TryAdd(serviceKey, new HashSet<string>());
        }
        else if (request.ChangedType == NamingChangedType.DeleteService)
        {
            _knownServices.TryRemove(serviceKey, out _);
        }

        // Notify matching watchers
        foreach (var entry in _fuzzyWatchers.Values)
        {
            if (MatchesPattern(request.ServiceName, entry.ServiceNamePattern) &&
                MatchesPattern(request.GroupName, entry.GroupNamePattern))
            {
                var changeEvent = new NamingFuzzyWatchChangeEvent(
                    request.Namespace ?? GetNamespace() ?? "",
                    request.GroupName,
                    request.ServiceName,
                    request.ChangedType,
                    request.SyncType);

                foreach (var watcher in entry.GetWatchers())
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
                        _logger?.LogError(ex, "Error notifying fuzzy watcher for {Service}@{Group}",
                            request.ServiceName, request.GroupName);
                    }
                }
            }
        }
    }

    private static bool MatchesPattern(string value, string pattern)
    {
        if (pattern == "*") return true;
        if (string.IsNullOrEmpty(pattern)) return true;

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
    }

    private static Instance? SelectByRandomWeight(List<Instance> instances)
    {
        if (instances.Count == 0) return null;
        if (instances.Count == 1) return instances[0];

        var totalWeight = instances.Sum(i => i.Weight);
        if (totalWeight <= 0)
        {
            return instances[Random.Shared.Next(instances.Count)];
        }

        var randomWeight = Random.Shared.NextDouble() * totalWeight;
        var currentWeight = 0.0;

        foreach (var instance in instances)
        {
            currentWeight += instance.Weight;
            if (currentWeight >= randomWeight)
            {
                return instance;
            }
        }

        return instances[^1];
    }

    private string GetGroupOrDefault(string? group)
    {
        return string.IsNullOrWhiteSpace(group) ? NacosConstants.DefaultGroup : group.Trim();
    }

    private string? GetNamespace()
    {
        return string.IsNullOrWhiteSpace(_options.Namespace) ? null : _options.Namespace;
    }

    private static string GetServiceKey(string serviceName, string groupName, string? clusters)
    {
        return NamingServiceInfo.GetKey(serviceName, groupName, clusters ?? "");
    }

    private static string GetFuzzyWatchKey(string serviceNamePattern, string groupNamePattern)
    {
        return $"{serviceNamePattern}@@{groupNamePattern}";
    }

    #endregion

    #region Lifecycle

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _cts.CancelAsync();

        // Wait for update task
        if (_updateTask != null)
        {
            try { await _updateTask.WaitAsync(TimeSpan.FromSeconds(2)); } catch { /* Ignore */ }
        }

        await _redoService.DisposeAsync();
        await _transportClient.DisposeAsync();
        await _grpcClient.DisposeAsync();

        _cts.Dispose();

        _logger?.LogInformation("NacosGrpcNamingService disposed");
    }

    #endregion

    #region Internal Types

    private class FuzzyWatchEntry
    {
        private readonly List<INamingFuzzyWatchEventWatcher> _watchers = new();
        private readonly object _lock = new();

        public string ServiceNamePattern { get; init; } = "";
        public string GroupNamePattern { get; init; } = "";

        public bool HasWatchers
        {
            get { lock (_lock) { return _watchers.Count > 0; } }
        }

        public void AddWatcher(INamingFuzzyWatchEventWatcher watcher)
        {
            lock (_lock)
            {
                if (!_watchers.Contains(watcher))
                {
                    _watchers.Add(watcher);
                }
            }
        }

        public void RemoveWatcher(INamingFuzzyWatchEventWatcher watcher)
        {
            lock (_lock)
            {
                _watchers.Remove(watcher);
            }
        }

        public List<INamingFuzzyWatchEventWatcher> GetWatchers()
        {
            lock (_lock)
            {
                return new List<INamingFuzzyWatchEventWatcher>(_watchers);
            }
        }
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
