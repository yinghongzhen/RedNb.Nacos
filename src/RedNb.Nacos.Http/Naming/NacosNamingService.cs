using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Client.Http;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Core.Naming.FuzzyWatch;
using RedNb.Nacos.Core.Naming.Selector;
using RedNb.Nacos.Utils;

namespace RedNb.Nacos.Client.Naming;

/// <summary>
/// Nacos naming service implementation using HTTP.
/// </summary>
public class NacosNamingService : INamingService
{
    private readonly NacosClientOptions _options;
    private readonly NacosHttpClient _httpClient;
    private readonly ILogger<NacosNamingService>? _logger;
    private readonly ServiceInfoHolder _serviceInfoHolder;
    private readonly InstancesChangeNotifier _changeNotifier;
    private readonly BeatReactor _beatReactor;
    private readonly NamingFuzzyWatchManager _fuzzyWatchManager;
    private readonly CancellationTokenSource _cts;
    private readonly Dictionary<string, Action<IInstancesChangeEvent>> _selectorListeners = new();
    private bool _disposed;
    private bool _isHealthy = true;

    private const string InstanceApiPath = "v1/ns/instance";
    private const string InstanceListApiPath = "v1/ns/instance/list";
    private const string ServiceApiPath = "v1/ns/service/list";
    private const string HealthApiPath = "v1/ns/health/instance";
    private const string BeatApiPath = "v1/ns/instance/beat";

    public NacosNamingService(NacosClientOptions options, ILogger<NacosNamingService>? logger = null)
    {
        _options = options;
        _logger = logger;
        _httpClient = new NacosHttpClient(options, logger);
        _serviceInfoHolder = new ServiceInfoHolder(options);
        _changeNotifier = new InstancesChangeNotifier();
        _beatReactor = new BeatReactor(this, options, logger);
        _fuzzyWatchManager = new NamingFuzzyWatchManager(logger);
        _cts = new CancellationTokenSource();

        // Start service info update task
        _ = StartServiceInfoUpdateTaskAsync(_cts.Token);
    }

    #region Registration

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
        return RegisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, ip, port, clusterName, cancellationToken);
    }

    public Task RegisterInstanceAsync(string serviceName, string groupName, string ip, int port, 
        string clusterName, CancellationToken cancellationToken = default)
    {
        var instance = new Instance
        {
            Ip = ip,
            Port = port,
            ClusterName = clusterName,
            Weight = 1.0,
            Healthy = true,
            Enabled = true,
            Ephemeral = true
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
        instance.Validate();
        groupName = GetGroupOrDefault(groupName);

        var parameters = BuildRegisterParameters(serviceName, groupName, instance);

        await _httpClient.PostAsync(InstanceApiPath, parameters, null, 
            _options.DefaultTimeout, cancellationToken);

        // Start heartbeat for ephemeral instances
        if (instance.Ephemeral)
        {
            _beatReactor.AddBeatInfo(serviceName, groupName, instance);
        }

        _logger?.LogInformation("Registered instance {Ip}:{Port} to service {Service}@{Group}", 
            instance.Ip, instance.Port, serviceName, groupName);
    }

    public async Task BatchRegisterInstanceAsync(string serviceName, string groupName, 
        List<Instance> instances, CancellationToken cancellationToken = default)
    {
        foreach (var instance in instances)
        {
            await RegisterInstanceAsync(serviceName, groupName, instance, cancellationToken);
        }
    }

    public async Task BatchDeregisterInstanceAsync(string serviceName, string groupName, 
        List<Instance> instances, CancellationToken cancellationToken = default)
    {
        foreach (var instance in instances)
        {
            await DeregisterInstanceAsync(serviceName, groupName, instance, cancellationToken);
        }
    }

    #endregion

    #region Deregistration

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
        return DeregisterInstanceAsync(serviceName, NacosConstants.DefaultGroup, ip, port, clusterName, cancellationToken);
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
        groupName = GetGroupOrDefault(groupName);

        // Stop heartbeat
        _beatReactor.RemoveBeatInfo(serviceName, groupName, instance);

        var parameters = new Dictionary<string, string?>
        {
            { "serviceName", serviceName },
            { "groupName", groupName },
            { "ip", instance.Ip },
            { "port", instance.Port.ToString() },
            { "clusterName", instance.ClusterName },
            { "ephemeral", instance.Ephemeral.ToString().ToLower() },
            { "namespaceId", GetNamespace() }
        };

        await _httpClient.DeleteAsync(InstanceApiPath, parameters, 
            _options.DefaultTimeout, cancellationToken);

        _logger?.LogInformation("Deregistered instance {Ip}:{Port} from service {Service}@{Group}", 
            instance.Ip, instance.Port, serviceName, groupName);
    }

    #endregion

    #region Query

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
        groupName = GetGroupOrDefault(groupName);
        var clusterString = NacosUtils.GetClusterString(clusters);

        ServiceInfo? serviceInfo;

        if (subscribe)
        {
            // Try to get from cache first
            serviceInfo = _serviceInfoHolder.GetServiceInfo(serviceName, groupName, clusterString);
            if (serviceInfo == null)
            {
                serviceInfo = await QueryServiceAsync(serviceName, groupName, clusterString, cancellationToken);
                if (serviceInfo != null)
                {
                    _serviceInfoHolder.ProcessServiceInfo(serviceInfo);
                }
            }
        }
        else
        {
            serviceInfo = await QueryServiceAsync(serviceName, groupName, clusterString, cancellationToken);
        }

        return serviceInfo?.Hosts ?? new List<Instance>();
    }

    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, bool healthy, 
        CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), healthy, true, cancellationToken);
    }

    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, bool healthy, 
        CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, groupName, new List<string>(), healthy, true, cancellationToken);
    }

    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, bool healthy, bool subscribe, 
        CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), healthy, subscribe, cancellationToken);
    }

    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, bool healthy, 
        bool subscribe, CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, groupName, new List<string>(), healthy, subscribe, cancellationToken);
    }

    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, List<string> clusters, bool healthy, 
        CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, clusters, healthy, true, cancellationToken);
    }

    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, 
        List<string> clusters, bool healthy, CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, groupName, clusters, healthy, true, cancellationToken);
    }

    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, List<string> clusters, 
        bool healthy, bool subscribe, CancellationToken cancellationToken = default)
    {
        return await SelectInstancesAsync(serviceName, NacosConstants.DefaultGroup, clusters, healthy, subscribe, cancellationToken);
    }

    public async Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, 
        List<string> clusters, bool healthy, bool subscribe, CancellationToken cancellationToken = default)
    {
        var instances = await GetAllInstancesAsync(serviceName, groupName, clusters, subscribe, cancellationToken);
        return instances.Where(i => i.Healthy == healthy && i.Enabled && i.Weight > 0).ToList();
    }

    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, 
        CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), true, cancellationToken);
    }

    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, 
        CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, groupName, new List<string>(), true, cancellationToken);
    }

    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, bool subscribe, 
        CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, new List<string>(), subscribe, cancellationToken);
    }

    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, 
        bool subscribe, CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, groupName, new List<string>(), subscribe, cancellationToken);
    }

    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, List<string> clusters, 
        CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, clusters, true, cancellationToken);
    }

    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, 
        List<string> clusters, CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, groupName, clusters, true, cancellationToken);
    }

    public async Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, List<string> clusters, 
        bool subscribe, CancellationToken cancellationToken = default)
    {
        return await SelectOneHealthyInstanceAsync(serviceName, NacosConstants.DefaultGroup, clusters, subscribe, cancellationToken);
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
        groupName = GetGroupOrDefault(groupName);
        var clusterString = NacosUtils.GetClusterString(clusters);

        _changeNotifier.RegisterListener(serviceName, groupName, clusterString, listener);
        
        // Ensure service is being polled
        var serviceInfo = await QueryServiceAsync(serviceName, groupName, clusterString, cancellationToken);
        if (serviceInfo != null)
        {
            _serviceInfoHolder.ProcessServiceInfo(serviceInfo);
        }

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
        groupName = GetGroupOrDefault(groupName);
        var selectorKey = selector?.Expression ?? "";

        // Create a filtered listener that applies the selector
        Action<IInstancesChangeEvent> filteredListener = (evt) =>
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
                evt = new InstancesChangeEvent
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

        _changeNotifier.RegisterListener(serviceName, groupName, selectorKey, filteredListener);
        _selectorListeners[$"{serviceName}@@{groupName}@@{listener.GetHashCode()}"] = filteredListener;

        // Ensure service is being polled
        var serviceInfo = await QueryServiceAsync(serviceName, groupName, "", cancellationToken);
        if (serviceInfo != null)
        {
            _serviceInfoHolder.ProcessServiceInfo(serviceInfo);
        }

        _logger?.LogDebug("Subscribed to service {Service}@{Group} with selector {Selector}", 
            serviceName, groupName, selector?.Expression);
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

    public Task UnsubscribeAsync(string serviceName, string groupName, List<string> clusters, 
        Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default)
    {
        groupName = GetGroupOrDefault(groupName);
        var clusterString = NacosUtils.GetClusterString(clusters);

        _changeNotifier.DeregisterListener(serviceName, groupName, clusterString, listener);
        
        _logger?.LogDebug("Unsubscribed from service {Service}@{Group}", serviceName, groupName);
        
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string serviceName, INamingSelector selector, 
        Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default)
    {
        return UnsubscribeAsync(serviceName, NacosConstants.DefaultGroup, selector, listener, cancellationToken);
    }

    public Task UnsubscribeAsync(string serviceName, string groupName, INamingSelector selector, 
        Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default)
    {
        groupName = GetGroupOrDefault(groupName);
        var selectorKey = selector?.Expression ?? "";
        var listenerKey = $"{serviceName}@@{groupName}@@{listener.GetHashCode()}";

        if (_selectorListeners.TryGetValue(listenerKey, out var filteredListener))
        {
            _changeNotifier.DeregisterListener(serviceName, groupName, selectorKey, filteredListener);
            _selectorListeners.Remove(listenerKey);
        }

        _logger?.LogDebug("Unsubscribed from service {Service}@{Group} with selector", serviceName, groupName);
        
        return Task.CompletedTask;
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
        groupName = GetGroupOrDefault(groupName);

        var parameters = new Dictionary<string, string?>
        {
            { "pageNo", pageNo.ToString() },
            { "pageSize", pageSize.ToString() },
            { "groupName", groupName },
            { "namespaceId", GetNamespace() }
        };

        var response = await _httpClient.GetAsync(ServiceApiPath, parameters, 
            _options.DefaultTimeout, cancellationToken);

        if (string.IsNullOrEmpty(response))
        {
            return new ListView<string>();
        }

        var result = JsonSerializer.Deserialize<ServiceListResponse>(response);
        return new ListView<string>(result?.Count ?? 0, result?.Doms ?? new List<string>());
    }

    public Task<ListView<string>> GetServicesOfServerAsync(int pageNo, int pageSize, INamingSelector selector, 
        CancellationToken cancellationToken = default)
    {
        return GetServicesOfServerAsync(pageNo, pageSize, NacosConstants.DefaultGroup, selector, cancellationToken);
    }

    public async Task<ListView<string>> GetServicesOfServerAsync(int pageNo, int pageSize, string groupName, 
        INamingSelector selector, CancellationToken cancellationToken = default)
    {
        groupName = GetGroupOrDefault(groupName);

        var parameters = new Dictionary<string, string?>
        {
            { "pageNo", pageNo.ToString() },
            { "pageSize", pageSize.ToString() },
            { "groupName", groupName },
            { "namespaceId", GetNamespace() }
        };

        // Add selector parameters if provided
        if (selector != null)
        {
            parameters["selector"] = JsonSerializer.Serialize(new
            {
                type = selector.Type,
                expression = selector.Expression
            });
        }

        var response = await _httpClient.GetAsync(ServiceApiPath, parameters, 
            _options.DefaultTimeout, cancellationToken);

        if (string.IsNullOrEmpty(response))
        {
            return new ListView<string>();
        }

        var result = JsonSerializer.Deserialize<ServiceListResponse>(response);
        return new ListView<string>(result?.Count ?? 0, result?.Doms ?? new List<string>());
    }

    public Task<List<ServiceInfo>> GetSubscribeServicesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_changeNotifier.GetSubscribedServices()
            .Select(key => 
            {
                var (serviceName, groupName, clusters) = ParseServiceKey(key);
                return _serviceInfoHolder.GetServiceInfo(serviceName, groupName, clusters);
            })
            .Where(s => s != null)
            .Cast<ServiceInfo>()
            .ToList());
    }

    #endregion

    #region Server Status

    public string GetServerStatus()
    {
        return _isHealthy ? "UP" : "DOWN";
    }

    public async Task ShutdownAsync()
    {
        await DisposeAsync();
    }

    #endregion

    #region Fuzzy Watch (Nacos 3.0)

    public Task FuzzyWatchAsync(string serviceNamePattern, INamingFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        return FuzzyWatchAsync(serviceNamePattern, "*", watcher, cancellationToken);
    }

    public Task FuzzyWatchAsync(string serviceNamePattern, string groupNamePattern, 
        INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        _fuzzyWatchManager.AddWatcher(serviceNamePattern, groupNamePattern, GetNamespace() ?? "", watcher);
        _logger?.LogDebug("Added fuzzy watch for service={ServicePattern}, group={GroupPattern}", 
            serviceNamePattern, groupNamePattern);
        return Task.CompletedTask;
    }

    public Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string serviceNamePattern, 
        INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        return FuzzyWatchWithGroupKeysAsync(serviceNamePattern, "*", watcher, cancellationToken);
    }

    public async Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string serviceNamePattern, 
        string groupNamePattern, INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        await FuzzyWatchAsync(serviceNamePattern, groupNamePattern, watcher, cancellationToken);
        
        // Return current matching keys
        var matchingKeys = _fuzzyWatchManager.GetMatchingKeys(serviceNamePattern, groupNamePattern, GetNamespace() ?? "");
        return matchingKeys;
    }

    public Task CancelFuzzyWatchAsync(string serviceNamePattern, INamingFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        return CancelFuzzyWatchAsync(serviceNamePattern, "*", watcher, cancellationToken);
    }

    public Task CancelFuzzyWatchAsync(string serviceNamePattern, string groupNamePattern, 
        INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        _fuzzyWatchManager.RemoveWatcher(serviceNamePattern, groupNamePattern, GetNamespace() ?? "", watcher);
        _logger?.LogDebug("Cancelled fuzzy watch for service={ServicePattern}, group={GroupPattern}", 
            serviceNamePattern, groupNamePattern);
        return Task.CompletedTask;
    }

    #endregion

    #region Internal Methods

    internal async Task<bool> SendBeatAsync(string serviceName, string groupName, Instance instance, 
        CancellationToken cancellationToken)
    {
        try
        {
            var beatInfo = new
            {
                serviceName = NacosUtils.GetGroupedServiceName(serviceName, groupName),
                ip = instance.Ip,
                port = instance.Port,
                weight = instance.Weight,
                cluster = instance.ClusterName,
                metadata = instance.Metadata
            };

            var parameters = new Dictionary<string, string?>
            {
                { "serviceName", serviceName },
                { "groupName", groupName },
                { "beat", JsonSerializer.Serialize(beatInfo) },
                { "namespaceId", GetNamespace() }
            };

            var body = NacosUtils.BuildQueryString(parameters);
            var response = await _httpClient.PutAsync(BeatApiPath, null, body, 
                _options.DefaultTimeout, cancellationToken);

            _isHealthy = true;
            return !string.IsNullOrEmpty(response);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send heartbeat for {Service}@{Group}", serviceName, groupName);
            return false;
        }
    }

    private async Task<ServiceInfo?> QueryServiceAsync(string serviceName, string groupName, 
        string clusters, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new Dictionary<string, string?>
            {
                { "serviceName", serviceName },
                { "groupName", groupName },
                { "clusters", clusters },
                { "healthyOnly", "false" },
                { "namespaceId", GetNamespace() }
            };

            var response = await _httpClient.GetAsync(InstanceListApiPath, parameters, 
                _options.DefaultTimeout, cancellationToken);

            if (string.IsNullOrEmpty(response))
            {
                return null;
            }

            _isHealthy = true;
            return JsonSerializer.Deserialize<ServiceInfo>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to query service {Service}@{Group}", serviceName, groupName);
            _isHealthy = false;
            return null;
        }
    }

    private async Task StartServiceInfoUpdateTaskAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting service info update task");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(10000, cancellationToken); // Update every 10 seconds

                var subscribedServices = _changeNotifier.GetSubscribedServices();
                foreach (var serviceKey in subscribedServices)
                {
                    try
                    {
                        var (serviceName, groupName, clusters) = ParseServiceKey(serviceKey);
                        var oldInfo = _serviceInfoHolder.GetServiceInfo(serviceName, groupName, clusters);
                        var newInfo = await QueryServiceAsync(serviceName, groupName, clusters, cancellationToken);

                        if (newInfo != null)
                        {
                            var hasChanged = _serviceInfoHolder.ProcessServiceInfo(newInfo);
                            if (hasChanged)
                            {
                                NotifyListeners(serviceName, groupName, clusters, oldInfo, newInfo);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to update service info for {Key}", serviceKey);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in service info update task");
            }
        }

        _logger?.LogInformation("Service info update task stopped");
    }

    private void NotifyListeners(string serviceName, string groupName, string clusters, 
        ServiceInfo? oldInfo, ServiceInfo newInfo)
    {
        var changeEvent = new InstancesChangeEvent
        {
            ServiceName = serviceName,
            GroupName = groupName,
            Clusters = clusters,
            Instances = newInfo.Hosts
        };

        // Calculate diff
        if (oldInfo != null)
        {
            var oldIps = oldInfo.Hosts.Select(h => h.ToInetAddr()).ToHashSet();
            var newIps = newInfo.Hosts.Select(h => h.ToInetAddr()).ToHashSet();

            changeEvent.AddedInstances = newInfo.Hosts.Where(h => !oldIps.Contains(h.ToInetAddr())).ToList();
            changeEvent.RemovedInstances = oldInfo.Hosts.Where(h => !newIps.Contains(h.ToInetAddr())).ToList();
        }
        else
        {
            changeEvent.AddedInstances = newInfo.Hosts;
        }

        _changeNotifier.NotifyListeners(serviceName, groupName, clusters, changeEvent);
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

    private Dictionary<string, string?> BuildRegisterParameters(string serviceName, string groupName, Instance instance)
    {
        var parameters = new Dictionary<string, string?>
        {
            { "serviceName", serviceName },
            { "groupName", groupName },
            { "ip", instance.Ip },
            { "port", instance.Port.ToString() },
            { "weight", instance.Weight.ToString() },
            { "enabled", instance.Enabled.ToString().ToLower() },
            { "healthy", instance.Healthy.ToString().ToLower() },
            { "ephemeral", instance.Ephemeral.ToString().ToLower() },
            { "clusterName", instance.ClusterName },
            { "namespaceId", GetNamespace() }
        };

        if (instance.Metadata.Count > 0)
        {
            parameters["metadata"] = JsonSerializer.Serialize(instance.Metadata);
        }

        return parameters;
    }

    private string GetGroupOrDefault(string? group)
    {
        return string.IsNullOrWhiteSpace(group) ? NacosConstants.DefaultGroup : group.Trim();
    }

    private string? GetNamespace()
    {
        return string.IsNullOrWhiteSpace(_options.Namespace) ? null : _options.Namespace;
    }

    private static string GetServiceKey(string serviceName, string groupName, string clusters)
    {
        return $"{groupName}@@{serviceName}@@{clusters}";
    }

    private static (string ServiceName, string GroupName, string Clusters) ParseServiceKey(string key)
    {
        var parts = key.Split("@@");
        return (parts.Length > 1 ? parts[1] : parts[0], 
                parts.Length > 0 ? parts[0] : NacosConstants.DefaultGroup,
                parts.Length > 2 ? parts[2] : string.Empty);
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await _cts.CancelAsync();
        _cts.Dispose();
        _beatReactor.Dispose();
        _httpClient.Dispose();
        _disposed = true;
    }

    private class ServiceListResponse
    {
        public int Count { get; set; }
        public List<string> Doms { get; set; } = new();
    }
}
