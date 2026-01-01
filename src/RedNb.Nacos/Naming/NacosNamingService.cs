using RedNb.Nacos.Naming.LoadBalancer;
using RedNb.Nacos.Naming.Models;
using RedNb.Nacos.Remote.Http;

namespace RedNb.Nacos.Naming;

/// <summary>
/// Nacos 命名服务实现
/// </summary>
public sealed class NacosNamingService : INacosNamingService
{
    private readonly NacosOptions _options;
    private readonly INacosHttpClient _httpClient;
    private readonly ILogger<NacosNamingService> _logger;
    private readonly ILoadBalancer _loadBalancer;

    private readonly ConcurrentDictionary<string, ServiceInfo> _serviceInfoCache = new();
    private readonly ConcurrentDictionary<string, List<IEventListener>> _subscribers = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _refreshTask;
    private bool _disposed;

    public NacosNamingService(
        IOptions<NacosOptions> options,
        INacosHttpClient httpClient,
        ILogger<NacosNamingService> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
        _loadBalancer = LoadBalancerFactory.Create(_options.Naming.LoadBalancerStrategy);

        // 启动服务刷新任务
        StartRefreshTask();
    }

    #region 服务注册

    /// <inheritdoc />
    public async Task RegisterInstanceAsync(
        string serviceName,
        string groupName,
        Instance instance,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentNullException.ThrowIfNull(instance);
        groupName = string.IsNullOrEmpty(groupName) ? NacosConstants.DefaultGroup : groupName;

        var formParams = new Dictionary<string, string>
        {
            ["serviceName"] = serviceName,
            ["groupName"] = groupName,
            ["namespaceId"] = _options.Namespace,
            ["ip"] = instance.Ip,
            ["port"] = instance.Port.ToString(),
            ["weight"] = instance.Weight.ToString(),
            ["enabled"] = instance.Enabled.ToString().ToLower(),
            ["healthy"] = instance.Healthy.ToString().ToLower(),
            ["ephemeral"] = instance.Ephemeral.ToString().ToLower(),
            ["clusterName"] = instance.ClusterName
        };

        if (instance.Metadata.Count > 0)
        {
            formParams["metadata"] = JsonSerializer.Serialize(instance.Metadata);
        }

        await _httpClient.PostStringAsync(
            EndpointConstants.Instance_Register,
            formParams,
            requireAuth: false,
            cancellationToken);

        _logger.LogInformation(
            "服务实例注册成功: serviceName={ServiceName}, ip={Ip}, port={Port}",
            serviceName,
            instance.Ip,
            instance.Port);
    }

    /// <inheritdoc />
    public Task RegisterInstanceAsync(
        string serviceName,
        string ip,
        int port,
        string? clusterName = null,
        CancellationToken cancellationToken = default)
    {
        var instance = new Instance
        {
            Ip = ip,
            Port = port,
            ClusterName = clusterName ?? NacosConstants.DefaultCluster,
            Weight = _options.Naming.Weight,
            Enabled = _options.Naming.InstanceEnabled,
            Ephemeral = _options.Naming.Ephemeral,
            Metadata = new Dictionary<string, string>(_options.Naming.Metadata)
        };

        return RegisterInstanceAsync(serviceName, _options.Naming.GroupName, instance, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeregisterInstanceAsync(
        string serviceName,
        string groupName,
        Instance instance,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentNullException.ThrowIfNull(instance);
        groupName = string.IsNullOrEmpty(groupName) ? NacosConstants.DefaultGroup : groupName;

        var queryParams = new Dictionary<string, string>
        {
            ["serviceName"] = serviceName,
            ["groupName"] = groupName,
            ["namespaceId"] = _options.Namespace,
            ["ip"] = instance.Ip,
            ["port"] = instance.Port.ToString(),
            ["clusterName"] = instance.ClusterName,
            ["ephemeral"] = instance.Ephemeral.ToString().ToLower()
        };

        await _httpClient.DeleteAsync(
            EndpointConstants.Instance_Deregister,
            queryParams,
            requireAuth: false,
            cancellationToken);

        _logger.LogInformation(
            "服务实例注销成功: serviceName={ServiceName}, ip={Ip}, port={Port}",
            serviceName,
            instance.Ip,
            instance.Port);
    }

    /// <inheritdoc />
    public Task DeregisterInstanceAsync(
        string serviceName,
        string ip,
        int port,
        string? clusterName = null,
        CancellationToken cancellationToken = default)
    {
        var instance = new Instance
        {
            Ip = ip,
            Port = port,
            ClusterName = clusterName ?? NacosConstants.DefaultCluster,
            Ephemeral = _options.Naming.Ephemeral
        };

        return DeregisterInstanceAsync(serviceName, _options.Naming.GroupName, instance, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateInstanceAsync(
        string serviceName,
        string groupName,
        Instance instance,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentNullException.ThrowIfNull(instance);
        groupName = string.IsNullOrEmpty(groupName) ? NacosConstants.DefaultGroup : groupName;

        var formParams = new Dictionary<string, string>
        {
            ["serviceName"] = serviceName,
            ["groupName"] = groupName,
            ["namespaceId"] = _options.Namespace,
            ["ip"] = instance.Ip,
            ["port"] = instance.Port.ToString(),
            ["weight"] = instance.Weight.ToString(),
            ["enabled"] = instance.Enabled.ToString().ToLower(),
            ["ephemeral"] = instance.Ephemeral.ToString().ToLower(),
            ["clusterName"] = instance.ClusterName
        };

        if (instance.Metadata.Count > 0)
        {
            formParams["metadata"] = JsonSerializer.Serialize(instance.Metadata);
        }

        await _httpClient.PutAsync<object>(
            EndpointConstants.Instance_Update,
            formParams,
            requireAuth: false,
            cancellationToken);

        _logger.LogDebug(
            "服务实例更新成功: serviceName={ServiceName}, ip={Ip}",
            serviceName,
            instance.Ip);
    }

    #endregion

    #region 服务发现

    /// <inheritdoc />
    public async Task<List<Instance>> GetAllInstancesAsync(
        string serviceName,
        string groupName = NacosConstants.DefaultGroup,
        bool subscribe = true,
        CancellationToken cancellationToken = default)
    {
        var serviceInfo = await GetServiceInfoAsync(serviceName, groupName, cancellationToken);
        return serviceInfo?.Hosts ?? new List<Instance>();
    }

    /// <inheritdoc />
    public async Task<List<Instance>> GetHealthyInstancesAsync(
        string serviceName,
        string groupName = NacosConstants.DefaultGroup,
        bool subscribe = true,
        CancellationToken cancellationToken = default)
    {
        var serviceInfo = await GetServiceInfoAsync(serviceName, groupName, cancellationToken);
        return serviceInfo?.GetHealthyInstances() ?? new List<Instance>();
    }

    /// <inheritdoc />
    public async Task<Instance?> SelectOneHealthyInstanceAsync(
        string serviceName,
        string groupName = NacosConstants.DefaultGroup,
        bool subscribe = true,
        CancellationToken cancellationToken = default)
    {
        var healthyInstances = await GetHealthyInstancesAsync(
            serviceName,
            groupName,
            subscribe,
            cancellationToken);

        return _loadBalancer.Select(healthyInstances);
    }

    /// <inheritdoc />
    public async Task<ServiceInfo?> GetServiceAsync(
        string serviceName,
        string groupName = NacosConstants.DefaultGroup,
        CancellationToken cancellationToken = default)
    {
        return await GetServiceInfoAsync(serviceName, groupName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetServicesAsync(
        string groupName = NacosConstants.DefaultGroup,
        int pageNo = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["namespaceId"] = _options.Namespace,
            ["groupName"] = groupName,
            ["pageNo"] = pageNo.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        var response = await _httpClient.GetAsync<ServiceListResponse>(
            EndpointConstants.Service_List,
            queryParams,
            requireAuth: false,
            cancellationToken);

        return response?.Services ?? new List<string>();
    }

    private async Task<ServiceInfo?> GetServiceInfoAsync(
        string serviceName,
        string groupName,
        CancellationToken cancellationToken)
    {
        var serviceKey = $"{groupName}@@{serviceName}";

        // 先从缓存获取
        if (_serviceInfoCache.TryGetValue(serviceKey, out var cached))
        {
            return cached;
        }

        // 从服务端获取
        var queryParams = new Dictionary<string, string>
        {
            ["serviceName"] = serviceName,
            ["groupName"] = groupName,
            ["namespaceId"] = _options.Namespace,
            ["healthyOnly"] = "false"
        };

        var serviceInfo = await _httpClient.GetAsync<ServiceInfo>(
            EndpointConstants.Instance_List,
            queryParams,
            requireAuth: false,
            cancellationToken);

        if (serviceInfo != null)
        {
            serviceInfo.Name = serviceName;
            serviceInfo.GroupName = groupName;
            _serviceInfoCache[serviceKey] = serviceInfo;
        }

        return serviceInfo;
    }

    #endregion

    #region 服务订阅

    /// <inheritdoc />
    public Task SubscribeAsync(
        string serviceName,
        string groupName,
        IEventListener listener,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentNullException.ThrowIfNull(listener);
        groupName = string.IsNullOrEmpty(groupName) ? NacosConstants.DefaultGroup : groupName;

        var serviceKey = $"{groupName}@@{serviceName}";
        var listeners = _subscribers.GetOrAdd(serviceKey, _ => new List<IEventListener>());

        lock (listeners)
        {
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        _logger.LogDebug("订阅服务: serviceName={ServiceName}, groupName={GroupName}", serviceName, groupName);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SubscribeAsync(
        string serviceName,
        string groupName,
        Action<ServiceInfo> callback,
        CancellationToken cancellationToken = default)
    {
        var listener = new ActionEventListener(callback);
        return SubscribeAsync(serviceName, groupName, listener, cancellationToken);
    }

    /// <inheritdoc />
    public Task UnsubscribeAsync(
        string serviceName,
        string groupName,
        IEventListener listener,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentNullException.ThrowIfNull(listener);
        groupName = string.IsNullOrEmpty(groupName) ? NacosConstants.DefaultGroup : groupName;

        var serviceKey = $"{groupName}@@{serviceName}";
        if (_subscribers.TryGetValue(serviceKey, out var listeners))
        {
            lock (listeners)
            {
                listeners.Remove(listener);
            }
        }

        _logger.LogDebug("取消订阅服务: serviceName={ServiceName}", serviceName);

        return Task.CompletedTask;
    }

    #endregion

    #region 健康检查

    /// <inheritdoc />
    public async Task<string> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetStringAsync(
                EndpointConstants.Health_Ready,
                cancellationToken: cancellationToken);

            return result ?? "UNKNOWN";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取服务状态失败");
            return "DOWN";
        }
    }

    #endregion

    #region 私有方法

    private void StartRefreshTask()
    {
        _refreshTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.Naming.HeartbeatIntervalMs, _cts.Token);
                    await RefreshSubscribedServicesAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "服务刷新任务异常");
                }
            }
        }, _cts.Token);
    }

    private async Task RefreshSubscribedServicesAsync(CancellationToken cancellationToken)
    {
        foreach (var kvp in _subscribers)
        {
            if (kvp.Value.Count == 0)
            {
                continue;
            }

            var parts = kvp.Key.Split("@@");
            if (parts.Length != 2)
            {
                continue;
            }

            var groupName = parts[0];
            var serviceName = parts[1];

            try
            {
                var queryParams = new Dictionary<string, string>
                {
                    ["serviceName"] = serviceName,
                    ["groupName"] = groupName,
                    ["namespaceId"] = _options.Namespace,
                    ["healthyOnly"] = "false"
                };

                var newServiceInfo = await _httpClient.GetAsync<ServiceInfo>(
                    EndpointConstants.Instance_List,
                    queryParams,
                    requireAuth: false,
                    cancellationToken);

                if (newServiceInfo == null)
                {
                    continue;
                }

                newServiceInfo.Name = serviceName;
                newServiceInfo.GroupName = groupName;

                // 检查是否有变更
                var serviceKey = kvp.Key;
                var hasChanged = !_serviceInfoCache.TryGetValue(serviceKey, out var oldServiceInfo)
                    || oldServiceInfo.Checksum != newServiceInfo.Checksum
                    || oldServiceInfo.Hosts.Count != newServiceInfo.Hosts.Count;

                if (hasChanged)
                {
                    _serviceInfoCache[serviceKey] = newServiceInfo;

                    // 通知订阅者
                    foreach (var listener in kvp.Value.ToList())
                    {
                        try
                        {
                            await listener.OnEventAsync(newServiceInfo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "服务变更监听器执行异常");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "刷新服务信息失败: serviceName={ServiceName}", serviceName);
            }
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _cts.CancelAsync();

        if (_refreshTask != null)
        {
            try
            {
                await _refreshTask;
            }
            catch (OperationCanceledException)
            {
                // 忽略取消异常
            }
        }

        _cts.Dispose();
    }
}
