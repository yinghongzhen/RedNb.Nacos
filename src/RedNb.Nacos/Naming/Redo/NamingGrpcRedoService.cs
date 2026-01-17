using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Naming.Redo.Data;
using RedNb.Nacos.Redo;

namespace RedNb.Nacos.Naming.Redo;

/// <summary>
/// 命名服务 gRPC Redo 服务
/// 当连接重新连接到服务器时，重新执行注册和订阅操作
/// 参考 Java SDK: com.alibaba.nacos.client.naming.remote.gprc.redo.NamingGrpcRedoService
/// </summary>
public class NamingGrpcRedoService : IDisposable
{
    private const string RedoThreadName = "nacos.client.naming.grpc.redo";
    private const long DefaultRedoDelayTime = 3000;
    private const int DefaultRedoThreadCount = 1;

    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, InstanceRedoData> _registeredInstances = new();
    private readonly ConcurrentDictionary<string, SubscriberRedoData> _subscribes = new();
    private readonly object _instanceLock = new();
    private readonly object _subscribeLock = new();
    
    private readonly long _redoDelayTime;
    private readonly CancellationTokenSource _cts = new();
    private Task? _redoTask;

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// 获取已注册的实例
    /// </summary>
    public ConcurrentDictionary<string, InstanceRedoData> RegisteredInstances => _registeredInstances;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="redoDelayTime">Redo 延迟时间（毫秒）</param>
    public NamingGrpcRedoService(ILogger logger, long redoDelayTime = DefaultRedoDelayTime)
    {
        _logger = logger;
        _redoDelayTime = redoDelayTime;
    }

    /// <summary>
    /// 启动 Redo 服务
    /// </summary>
    /// <param name="redoAction">执行 Redo 操作的委托</param>
    public void Start(Func<NamingGrpcRedoService, CancellationToken, Task> redoAction)
    {
        _redoTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay((int)_redoDelayTime, _cts.Token);
                    
                    if (!IsConnected)
                    {
                        _logger.LogWarning("Grpc Connection is disconnect, skip current redo task");
                        continue;
                    }

                    await redoAction(this, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redo task run with unexpected exception");
                }
            }
        }, _cts.Token);
    }

    /// <summary>
    /// 连接成功事件
    /// </summary>
    public void OnConnected()
    {
        IsConnected = true;
        _logger.LogInformation("Grpc connection connect");
    }

    /// <summary>
    /// 连接断开事件
    /// </summary>
    public void OnDisconnect()
    {
        IsConnected = false;
        _logger.LogWarning("Grpc connection disconnect, mark to redo");

        lock (_instanceLock)
        {
            foreach (var instanceRedoData in _registeredInstances.Values)
            {
                instanceRedoData.Registered = false;
            }
        }

        lock (_subscribeLock)
        {
            foreach (var subscriberRedoData in _subscribes.Values)
            {
                subscriberRedoData.Registered = false;
            }
        }

        _logger.LogWarning("mark to redo completed");
    }

    #region Instance Redo Methods

    /// <summary>
    /// 缓存实例以便重做
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="instance">实例</param>
    public void CacheInstanceForRedo(string serviceName, string groupName, Instance instance)
    {
        var key = GetGroupedName(serviceName, groupName);
        var redoData = InstanceRedoData.Build(serviceName, groupName, instance);
        lock (_instanceLock)
        {
            _registeredInstances[key] = redoData;
        }
    }

    /// <summary>
    /// 缓存批量实例以便重做
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="instances">实例列表</param>
    public void CacheInstanceForRedo(string serviceName, string groupName, List<Instance> instances)
    {
        var key = GetGroupedName(serviceName, groupName);
        var redoData = BatchInstanceRedoData.Build(serviceName, groupName, instances);
        lock (_instanceLock)
        {
            _registeredInstances[key] = redoData;
        }
    }

    /// <summary>
    /// 实例注册成功，标记为已注册
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    public void InstanceRegistered(string serviceName, string groupName)
    {
        var key = GetGroupedName(serviceName, groupName);
        lock (_instanceLock)
        {
            if (_registeredInstances.TryGetValue(key, out var redoData))
            {
                redoData.SetRegistered();
            }
        }
    }

    /// <summary>
    /// 实例注销，标记为正在注销
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    public void InstanceDeregister(string serviceName, string groupName)
    {
        var key = GetGroupedName(serviceName, groupName);
        lock (_instanceLock)
        {
            if (_registeredInstances.TryGetValue(key, out var redoData))
            {
                redoData.Unregistering = true;
                redoData.ExpectedRegistered = false;
            }
        }
    }

    /// <summary>
    /// 实例注销完成
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    public void InstanceDeregistered(string serviceName, string groupName)
    {
        var key = GetGroupedName(serviceName, groupName);
        lock (_instanceLock)
        {
            if (_registeredInstances.TryGetValue(key, out var redoData))
            {
                redoData.SetUnregistered();
            }
        }
    }

    /// <summary>
    /// 移除实例 Redo 数据
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    public void RemoveInstanceForRedo(string serviceName, string groupName)
    {
        var key = GetGroupedName(serviceName, groupName);
        lock (_instanceLock)
        {
            if (_registeredInstances.TryGetValue(key, out var redoData) && !redoData.ExpectedRegistered)
            {
                _registeredInstances.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// 查找需要 Redo 的实例数据
    /// </summary>
    /// <returns>需要 Redo 的实例数据集合</returns>
    public HashSet<InstanceRedoData> FindInstanceRedoData()
    {
        var result = new HashSet<InstanceRedoData>();
        lock (_instanceLock)
        {
            foreach (var each in _registeredInstances.Values)
            {
                if (each.IsNeedRedo())
                {
                    result.Add(each);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 根据 key 获取已注册的实例
    /// </summary>
    /// <param name="combinedServiceName">组合的服务名称</param>
    /// <returns>实例 Redo 数据</returns>
    public InstanceRedoData? GetRegisteredInstancesByKey(string combinedServiceName)
    {
        _registeredInstances.TryGetValue(combinedServiceName, out var result);
        return result;
    }

    #endregion

    #region Subscriber Redo Methods

    /// <summary>
    /// 缓存订阅者以便重做
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="cluster">集群</param>
    public void CacheSubscriberForRedo(string serviceName, string groupName, string cluster)
    {
        var key = GetSubscriberKey(serviceName, groupName, cluster);
        var redoData = SubscriberRedoData.Build(serviceName, groupName, cluster);
        lock (_subscribeLock)
        {
            _subscribes[key] = redoData;
        }
    }

    /// <summary>
    /// 订阅注册成功，标记为已注册
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="cluster">集群</param>
    public void SubscriberRegistered(string serviceName, string groupName, string cluster)
    {
        var key = GetSubscriberKey(serviceName, groupName, cluster);
        lock (_subscribeLock)
        {
            if (_subscribes.TryGetValue(key, out var redoData))
            {
                redoData.Registered = true;
            }
        }
    }

    /// <summary>
    /// 订阅注销，标记为正在注销
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="cluster">集群</param>
    public void SubscriberDeregister(string serviceName, string groupName, string cluster)
    {
        var key = GetSubscriberKey(serviceName, groupName, cluster);
        lock (_subscribeLock)
        {
            if (_subscribes.TryGetValue(key, out var redoData))
            {
                redoData.Unregistering = true;
                redoData.ExpectedRegistered = false;
            }
        }
    }

    /// <summary>
    /// 判断订阅者是否已注册
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="cluster">集群</param>
    /// <returns>是否已注册</returns>
    public bool IsSubscriberRegistered(string serviceName, string groupName, string cluster)
    {
        var key = GetSubscriberKey(serviceName, groupName, cluster);
        lock (_subscribeLock)
        {
            if (_subscribes.TryGetValue(key, out var redoData))
            {
                return redoData.Registered;
            }
        }
        return false;
    }

    /// <summary>
    /// 移除订阅者 Redo 数据
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="cluster">集群</param>
    public void RemoveSubscriberForRedo(string serviceName, string groupName, string cluster)
    {
        var key = GetSubscriberKey(serviceName, groupName, cluster);
        lock (_subscribeLock)
        {
            if (_subscribes.TryGetValue(key, out var redoData) && !redoData.ExpectedRegistered)
            {
                _subscribes.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// 查找需要 Redo 的订阅者数据
    /// </summary>
    /// <returns>需要 Redo 的订阅者数据集合</returns>
    public HashSet<SubscriberRedoData> FindSubscriberRedoData()
    {
        var result = new HashSet<SubscriberRedoData>();
        lock (_subscribeLock)
        {
            foreach (var each in _subscribes.Values)
            {
                if (each.IsNeedRedo())
                {
                    result.Add(each);
                }
            }
        }
        return result;
    }

    #endregion

    #region Helper Methods

    private static string GetGroupedName(string serviceName, string groupName)
    {
        return $"{groupName}@@{serviceName}";
    }

    private static string GetSubscriberKey(string serviceName, string groupName, string cluster)
    {
        var groupedName = GetGroupedName(serviceName, groupName);
        return string.IsNullOrEmpty(cluster) ? groupedName : $"{groupedName}@@{cluster}";
    }

    #endregion

    /// <summary>
    /// 关闭 Redo 服务
    /// </summary>
    public void Shutdown()
    {
        _logger.LogInformation("Shutdown grpc redo service executor");
        _registeredInstances.Clear();
        _subscribes.Clear();
        _cts.Cancel();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Shutdown();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
