using RedNb.Nacos.Common.Failover;
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
    private readonly INacosGrpcClient? _grpcClient;
    private readonly IServiceSnapshot _serviceSnapshot;
    private readonly ILogger<NacosNamingService> _logger;
    private readonly ILoadBalancer _loadBalancer;

    private readonly ConcurrentDictionary<string, ServiceInfo> _serviceInfoCache = new();
    private readonly ConcurrentDictionary<string, List<IEventListener>> _subscribers = new();
    private readonly ConcurrentDictionary<string, List<INamingChangeListener>> _changeListeners = new();
    private readonly ConcurrentDictionary<string, List<IEventListener>> _fuzzyWatchers = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _refreshTask;
    private bool _disposed;
    private bool _useGrpc;

    public NacosNamingService(
        IOptions<NacosOptions> options,
        INacosHttpClient httpClient,
        IServiceSnapshot serviceSnapshot,
        ILogger<NacosNamingService> logger,
        INacosGrpcClient? grpcClient = null)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _grpcClient = grpcClient;
        _serviceSnapshot = serviceSnapshot;
        _logger = logger;
        _loadBalancer = LoadBalancerFactory.Create(_options.Naming.LoadBalancerStrategy);
        _useGrpc = _options.UseGrpc && grpcClient != null;

        // 注册 gRPC 推送处理器
        if (_useGrpc && _grpcClient != null)
        {
            _grpcClient.RegisterPushHandler<NotifySubscriberRequest>(HandleNotifySubscriberAsync);
        }

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

        if (_useGrpc && _grpcClient != null)
        {
            await RegisterInstanceByGrpcAsync(serviceName, groupName, instance, cancellationToken);
        }
        else
        {
            await RegisterInstanceByHttpAsync(serviceName, groupName, instance, cancellationToken);
        }

        _logger.LogInformation(
            "服务实例注册成功: serviceName={ServiceName}, ip={Ip}, port={Port}",
            serviceName,
            instance.Ip,
            instance.Port);
    }

    private async Task RegisterInstanceByGrpcAsync(
        string serviceName,
        string groupName,
        Instance instance,
        CancellationToken cancellationToken)
    {
        var request = new InstanceRequest
        {
            Type = "registerInstance",
            ServiceName = serviceName,
            GroupName = groupName,
            Namespace = _options.Namespace,
            Instance = GrpcInstance.FromInstance(instance)
        };

        // 对于 gRPC 模式的实例注册，使用普通请求即可
        // Nacos 服务器会自动将通过双向流连接的客户端视为临时实例
        // 心跳通过连接保持自动维护
        var response = await _grpcClient!.RequestAsync<InstanceRequest, InstanceResponse>(
            request, cancellationToken);

        if (response == null || !response.IsSuccess)
        {
            throw new NacosException(response?.ErrorCode ?? 500, response?.Message ?? "注册实例失败");
        }
    }

    private async Task RegisterInstanceByHttpAsync(
        string serviceName,
        string groupName,
        Instance instance,
        CancellationToken cancellationToken)
    {
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
    public async Task BatchRegisterInstanceAsync(
        string serviceName,
        string groupName,
        List<Instance> instances,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentNullException.ThrowIfNull(instances);
        if (instances.Count == 0) return;

        groupName = string.IsNullOrEmpty(groupName) ? NacosConstants.DefaultGroup : groupName;

        if (_useGrpc && _grpcClient != null)
        {
            var request = new BatchInstanceRequest
            {
                Type = "batchRegisterInstance",
                ServiceName = serviceName,
                GroupName = groupName,
                Namespace = _options.Namespace,
                Instances = instances.Select(GrpcInstance.FromInstance).ToList()
            };

            var response = await _grpcClient.RequestAsync<BatchInstanceRequest, BatchInstanceResponse>(
                request, cancellationToken);

            if (response == null || !response.IsSuccess)
            {
                throw new NacosException(response?.ErrorCode ?? 500, response?.Message ?? "批量注册实例失败");
            }
        }
        else
        {
            // HTTP 不支持批量注册，逐个注册
            foreach (var instance in instances)
            {
                await RegisterInstanceByHttpAsync(serviceName, groupName, instance, cancellationToken);
            }
        }

        _logger.LogInformation(
            "批量服务实例注册成功: serviceName={ServiceName}, count={Count}",
            serviceName,
            instances.Count);
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

        if (_useGrpc && _grpcClient != null)
        {
            var request = new InstanceRequest
            {
                Type = "deregisterInstance",
                ServiceName = serviceName,
                GroupName = groupName,
                Namespace = _options.Namespace,
                Instance = GrpcInstance.FromInstance(instance)
            };

            var response = await _grpcClient.RequestAsync<InstanceRequest, InstanceResponse>(
                request, cancellationToken);

            if (response == null || !response.IsSuccess)
            {
                throw new NacosException(response?.ErrorCode ?? 500, response?.Message ?? "注销实例失败");
            }
        }
        else
        {
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
        }

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
    public async Task BatchDeregisterInstanceAsync(
        string serviceName,
        string groupName,
        List<Instance> instances,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentNullException.ThrowIfNull(instances);
        if (instances.Count == 0) return;

        groupName = string.IsNullOrEmpty(groupName) ? NacosConstants.DefaultGroup : groupName;

        if (_useGrpc && _grpcClient != null)
        {
            var request = new BatchInstanceRequest
            {
                Type = "batchDeregisterInstance",
                ServiceName = serviceName,
                GroupName = groupName,
                Namespace = _options.Namespace,
                Instances = instances.Select(GrpcInstance.FromInstance).ToList()
            };

            var response = await _grpcClient.RequestAsync<BatchInstanceRequest, BatchInstanceResponse>(
                request, cancellationToken);

            if (response == null || !response.IsSuccess)
            {
                throw new NacosException(response?.ErrorCode ?? 500, response?.Message ?? "批量注销实例失败");
            }
        }
        else
        {
            // HTTP 不支持批量注销，逐个注销
            foreach (var instance in instances)
            {
                await DeregisterInstanceAsync(serviceName, groupName, instance, cancellationToken);
            }
        }

        _logger.LogInformation(
            "批量服务实例注销成功: serviceName={ServiceName}, count={Count}",
            serviceName,
            instances.Count);
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

    /// <inheritdoc />
    public async Task<bool> SendHeartbeatAsync(
        string serviceName,
        string groupName,
        Instance instance,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentNullException.ThrowIfNull(instance);
        groupName = string.IsNullOrEmpty(groupName) ? NacosConstants.DefaultGroup : groupName;

        // gRPC 模式下，心跳由连接自动维护
        if (_useGrpc)
        {
            return true;
        }

        // Nacos v3 不再提供独立的心跳接口
        // 临时实例通过定期重新注册来维持心跳
        var formParams = new Dictionary<string, string>
        {
            ["serviceName"] = serviceName,
            ["groupName"] = groupName,
            ["namespaceId"] = _options.Namespace,
            ["ip"] = instance.Ip,
            ["port"] = instance.Port.ToString(),
            ["weight"] = instance.Weight.ToString(),
            ["enabled"] = instance.Enabled.ToString().ToLower(),
            ["healthy"] = "true",
            ["ephemeral"] = instance.Ephemeral.ToString().ToLower(),
            ["clusterName"] = instance.ClusterName
        };

        if (instance.Metadata.Count > 0)
        {
            formParams["metadata"] = JsonSerializer.Serialize(instance.Metadata);
        }

        try
        {
            // 使用注册接口，Nacos 会自动处理已存在的实例（相当于心跳续约）
            await _httpClient.PostStringAsync(
                EndpointConstants.Instance_Register,
                formParams,
                requireAuth: false,
                cancellationToken);

            _logger.LogDebug(
                "心跳发送成功: serviceName={ServiceName}, ip={Ip}, port={Port}",
                serviceName,
                instance.Ip,
                instance.Port);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "心跳发送失败: serviceName={ServiceName}, ip={Ip}, port={Port}",
                serviceName,
                instance.Ip,
                instance.Port);
            return false;
        }
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
        if (_useGrpc && _grpcClient != null)
        {
            var request = new ServiceListRequest
            {
                Namespace = _options.Namespace,
                GroupName = groupName,
                PageNo = pageNo,
                PageSize = pageSize
            };

            var response = await _grpcClient.RequestAsync<ServiceListRequest, Remote.Grpc.Models.ServiceListResponse>(
                request, cancellationToken);

            return response?.ServiceNames ?? new List<string>();
        }

        var queryParams = new Dictionary<string, string>
        {
            ["namespaceId"] = _options.Namespace,
            ["groupName"] = groupName,
            ["pageNo"] = pageNo.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        var httpResponse = await _httpClient.GetAsync<Naming.Models.ServiceListResponse>(
            EndpointConstants.Service_List,
            queryParams,
            requireAuth: false,
            cancellationToken);

        return httpResponse?.Services ?? new List<string>();
    }

    /// <inheritdoc />
    public Task<List<ServiceInfo>> GetSubscribedServicesAsync(CancellationToken cancellationToken = default)
    {
        var subscribedServices = _serviceInfoCache.Values.ToList();
        return Task.FromResult(subscribedServices);
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
        try
        {
            ServiceInfo? serviceInfo = null;

            if (_useGrpc && _grpcClient != null)
            {
                serviceInfo = await GetServiceInfoByGrpcAsync(serviceName, groupName, cancellationToken);
            }
            else
            {
                serviceInfo = await GetServiceInfoByHttpAsync(serviceName, groupName, cancellationToken);
            }

            if (serviceInfo != null)
            {
                serviceInfo.Name = serviceName;
                serviceInfo.GroupName = groupName;
                _serviceInfoCache[serviceKey] = serviceInfo;

                // 保存快照
                await _serviceSnapshot.SaveSnapshotAsync(
                    serviceName, groupName, _options.Namespace, serviceInfo, cancellationToken);
            }

            return serviceInfo;
        }
        catch (NacosConnectionException ex)
        {
            _logger.LogWarning(ex, "连接失败，尝试从快照获取服务: serviceName={ServiceName}", serviceName);

            // 尝试从快照获取
            var snapshotService = await _serviceSnapshot.GetSnapshotAsync(
                serviceName, groupName, _options.Namespace, cancellationToken);

            if (snapshotService != null)
            {
                _logger.LogInformation("从快照获取服务成功: serviceName={ServiceName}", serviceName);
                _serviceInfoCache[serviceKey] = snapshotService;
                return snapshotService;
            }

            throw;
        }
    }

    private async Task<ServiceInfo?> GetServiceInfoByGrpcAsync(
        string serviceName,
        string groupName,
        CancellationToken cancellationToken)
    {
        var request = new ServiceQueryRequest
        {
            ServiceName = serviceName,
            GroupName = groupName,
            Namespace = _options.Namespace,
            HealthyOnly = false
        };

        var response = await _grpcClient!.RequestAsync<ServiceQueryRequest, QueryServiceResponse>(
            request, cancellationToken);

        return response?.ServiceInfo?.ToServiceInfo();
    }

    private async Task<ServiceInfo?> GetServiceInfoByHttpAsync(
        string serviceName,
        string groupName,
        CancellationToken cancellationToken)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["serviceName"] = serviceName,
            ["groupName"] = groupName,
            ["namespaceId"] = _options.Namespace,
            ["healthyOnly"] = "false"
        };

        return await _httpClient.GetAsync<ServiceInfo>(
            EndpointConstants.Instance_List,
            queryParams,
            requireAuth: false,
            cancellationToken);
    }

    #endregion

    #region 服务订阅

    /// <inheritdoc />
    public async Task SubscribeAsync(
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

        // 如果使用 gRPC，发送订阅请求
        if (_useGrpc && _grpcClient != null)
        {
            await SendSubscribeRequestAsync(serviceName, groupName, true, cancellationToken);
        }

        _logger.LogDebug("订阅服务: serviceName={ServiceName}, groupName={GroupName}", serviceName, groupName);
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
    public Task SubscribeAsync(
        string serviceName,
        string groupName,
        INamingChangeListener listener,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentNullException.ThrowIfNull(listener);
        groupName = string.IsNullOrEmpty(groupName) ? NacosConstants.DefaultGroup : groupName;

        var serviceKey = $"{groupName}@@{serviceName}";
        var listeners = _changeListeners.GetOrAdd(serviceKey, _ => new List<INamingChangeListener>());

        lock (listeners)
        {
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        _logger.LogDebug("订阅服务(差异监听): serviceName={ServiceName}, groupName={GroupName}", serviceName, groupName);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(
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

            // 如果没有监听器了，取消订阅
            if (listeners.Count == 0 && _useGrpc && _grpcClient != null)
            {
                await SendSubscribeRequestAsync(serviceName, groupName, false, cancellationToken);
            }
        }

        _logger.LogDebug("取消订阅服务: serviceName={ServiceName}", serviceName);
    }

    private async Task SendSubscribeRequestAsync(
        string serviceName,
        string groupName,
        bool subscribe,
        CancellationToken cancellationToken)
    {
        var request = new SubscribeServiceRequest
        {
            ServiceName = serviceName,
            GroupName = groupName,
            Namespace = _options.Namespace,
            Subscribe = subscribe
        };

        await _grpcClient!.RequestAsync<SubscribeServiceRequest, SubscribeServiceResponse>(
            request, cancellationToken);
    }

    #endregion

    #region 模糊订阅

    /// <inheritdoc />
    public Task FuzzyWatchAsync(
        string servicePattern,
        string groupPattern,
        IEventListener listener,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(servicePattern);
        ArgumentException.ThrowIfNullOrEmpty(groupPattern);
        ArgumentNullException.ThrowIfNull(listener);

        var key = $"{servicePattern}@@{groupPattern}";
        var listeners = _fuzzyWatchers.GetOrAdd(key, _ => new List<IEventListener>());

        lock (listeners)
        {
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        _logger.LogDebug("添加模糊服务订阅: servicePattern={ServicePattern}, groupPattern={GroupPattern}",
            servicePattern, groupPattern);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelFuzzyWatchAsync(
        string servicePattern,
        string groupPattern,
        IEventListener listener,
        CancellationToken cancellationToken = default)
    {
        var key = $"{servicePattern}@@{groupPattern}";

        if (_fuzzyWatchers.TryGetValue(key, out var listeners))
        {
            lock (listeners)
            {
                listeners.Remove(listener);
            }
        }

        _logger.LogDebug("取消模糊服务订阅: servicePattern={ServicePattern}, groupPattern={GroupPattern}",
            servicePattern, groupPattern);

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

    #region gRPC 推送处理

    private async Task<NacosResponse?> HandleNotifySubscriberAsync(NotifySubscriberRequest request)
    {
        if (request.ServiceInfo == null)
        {
            return new NotifySubscriberResponse { ResultCode = 200 };
        }

        var serviceName = request.ServiceInfo.Name;
        var groupName = request.ServiceInfo.GroupName ?? NacosConstants.DefaultGroup;

        _logger.LogInformation("收到服务变更通知: serviceName={ServiceName}, groupName={GroupName}",
            serviceName, groupName);

        var serviceKey = $"{groupName}@@{serviceName}";
        var newServiceInfo = request.ServiceInfo.ToServiceInfo();

        // 计算差异
        _serviceInfoCache.TryGetValue(serviceKey, out var oldServiceInfo);
        var changeEvent = ComputeChangeEvent(serviceName!, groupName, oldServiceInfo, newServiceInfo);

        // 更新缓存
        _serviceInfoCache[serviceKey] = newServiceInfo;

        // 保存快照
        await _serviceSnapshot.SaveSnapshotAsync(
            serviceName!, groupName, _options.Namespace, newServiceInfo);

        // 通知订阅者
        if (_subscribers.TryGetValue(serviceKey, out var listeners))
        {
            foreach (var listener in listeners.ToList())
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

        // 通知差异监听器
        if (changeEvent.HasChanges && _changeListeners.TryGetValue(serviceKey, out var changeListeners))
        {
            foreach (var listener in changeListeners.ToList())
            {
                try
                {
                    await listener.OnChangeAsync(changeEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "服务差异监听器执行异常");
                }
            }
        }

        // 通知模糊订阅者
        await NotifyFuzzyWatchersAsync(serviceName!, groupName, newServiceInfo);

        return new NotifySubscriberResponse { ResultCode = 200 };
    }

    private NamingChangeEvent ComputeChangeEvent(
        string serviceName,
        string groupName,
        ServiceInfo? oldServiceInfo,
        ServiceInfo newServiceInfo)
    {
        var oldInstances = oldServiceInfo?.Hosts ?? new List<Instance>();
        var newInstances = newServiceInfo.Hosts;

        var oldInstanceMap = oldInstances.ToDictionary(i => $"{i.Ip}:{i.Port}");
        var newInstanceMap = newInstances.ToDictionary(i => $"{i.Ip}:{i.Port}");

        var added = newInstances.Where(i => !oldInstanceMap.ContainsKey($"{i.Ip}:{i.Port}")).ToList();
        var removed = oldInstances.Where(i => !newInstanceMap.ContainsKey($"{i.Ip}:{i.Port}")).ToList();
        var modified = new List<Instance>();

        foreach (var newInstance in newInstances)
        {
            var key = $"{newInstance.Ip}:{newInstance.Port}";
            if (oldInstanceMap.TryGetValue(key, out var oldInstance))
            {
                // 检查是否有变化
                if (oldInstance.Weight != newInstance.Weight ||
                    oldInstance.Healthy != newInstance.Healthy ||
                    oldInstance.Enabled != newInstance.Enabled)
                {
                    modified.Add(newInstance);
                }
            }
        }

        return new NamingChangeEvent
        {
            ServiceName = serviceName,
            GroupName = groupName,
            Namespace = _options.Namespace,
            AddedInstances = added,
            RemovedInstances = removed,
            ModifiedInstances = modified,
            AllInstances = newInstances
        };
    }

    private async Task NotifyFuzzyWatchersAsync(string serviceName, string groupName, ServiceInfo serviceInfo)
    {
        foreach (var (key, listeners) in _fuzzyWatchers)
        {
            var parts = key.Split("@@");
            if (parts.Length != 2) continue;

            var servicePattern = parts[0];
            var groupPattern = parts[1];

            if (MatchPattern(serviceName, servicePattern) &&
                MatchPattern(groupName, groupPattern))
            {
                foreach (var listener in listeners.ToList())
                {
                    try
                    {
                        await listener.OnEventAsync(serviceInfo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "模糊订阅监听器执行异常");
                    }
                }
            }
        }
    }

    private static bool MatchPattern(string value, string pattern)
    {
        if (pattern == "*") return true;
        if (!pattern.Contains('*')) return value == pattern;

        var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(value, regex);
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

                    // 如果使用 gRPC，不需要轮询刷新
                    if (_useGrpc)
                    {
                        continue;
                    }

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
                var newServiceInfo = await GetServiceInfoByHttpAsync(serviceName, groupName, cancellationToken);

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
                    // 计算差异
                    var changeEvent = ComputeChangeEvent(serviceName, groupName, oldServiceInfo, newServiceInfo);

                    _serviceInfoCache[serviceKey] = newServiceInfo;

                    // 保存快照
                    await _serviceSnapshot.SaveSnapshotAsync(
                        serviceName, groupName, _options.Namespace, newServiceInfo, cancellationToken);

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

                    // 通知差异监听器
                    if (changeEvent.HasChanges && _changeListeners.TryGetValue(serviceKey, out var changeListeners))
                    {
                        foreach (var listener in changeListeners.ToList())
                        {
                            try
                            {
                                await listener.OnChangeAsync(changeEvent);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "服务差异监听器执行异常");
                            }
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
