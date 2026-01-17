using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Naming.Cache;

namespace RedNb.Nacos.Failover;

/// <summary>
/// 命名服务故障转移反应器
/// 参考 Java SDK: com.alibaba.nacos.client.naming.backups.FailoverReactor
/// </summary>
public class NamingFailoverReactor : IDisposable
{
    private readonly ILogger _logger;
    private readonly IFailoverDataSource<ServiceInfo>? _failoverDataSource;
    private readonly InstancesDiffer _instancesDiffer;
    private readonly Func<ConcurrentDictionary<string, ServiceInfo>>? _serviceInfoMapGetter;
    private readonly string _notifierEventScope;

    private ConcurrentDictionary<string, ServiceInfo> _serviceMap = new();
    private bool _failoverSwitchEnable;
    private readonly CancellationTokenSource _cts = new();
    private Task? _refreshTask;

    /// <summary>
    /// 实例变更事件
    /// </summary>
    public event EventHandler<InstancesChangeEventArgs>? InstancesChanged;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="failoverDataSource">故障转移数据源</param>
    /// <param name="serviceInfoMapGetter">获取当前服务信息映射的委托</param>
    /// <param name="notifierEventScope">事件作用域</param>
    public NamingFailoverReactor(
        ILogger logger,
        IFailoverDataSource<ServiceInfo>? failoverDataSource,
        Func<ConcurrentDictionary<string, ServiceInfo>>? serviceInfoMapGetter = null,
        string notifierEventScope = "")
    {
        _logger = logger;
        _failoverDataSource = failoverDataSource;
        _serviceInfoMapGetter = serviceInfoMapGetter;
        _notifierEventScope = notifierEventScope;
        _instancesDiffer = new InstancesDiffer(logger);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init()
    {
        if (_failoverDataSource == null)
        {
            _logger.LogWarning("No failover data source configured, failover feature disabled");
            return;
        }

        _refreshTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, _cts.Token);
                    RefreshFailoverSwitch();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FailoverSwitchRefresher run err");
                }
            }
        }, _cts.Token);
    }

    /// <summary>
    /// 刷新故障转移开关
    /// </summary>
    private void RefreshFailoverSwitch()
    {
        if (_failoverDataSource == null)
        {
            return;
        }

        try
        {
            var fSwitch = _failoverDataSource.GetSwitch();
            if (!fSwitch.Enabled)
            {
                if (_failoverSwitchEnable)
                {
                    // 从启用变为禁用，需要通知实例变更
                    HandleFailoverSwitchOff();
                }
                _failoverSwitchEnable = false;
                return;
            }

            if (fSwitch.Enabled != _failoverSwitchEnable)
            {
                _logger.LogInformation("failover switch changed, new: {Enabled}", fSwitch.Enabled);
            }

            if (fSwitch.Enabled)
            {
                HandleFailoverSwitchOn();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing failover switch");
        }
    }

    /// <summary>
    /// 处理故障转移开关开启
    /// </summary>
    private void HandleFailoverSwitchOn()
    {
        if (_failoverDataSource == null)
        {
            return;
        }

        var failoverMap = new ConcurrentDictionary<string, ServiceInfo>();
        var failoverData = _failoverDataSource.GetFailoverData();

        foreach (var (key, data) in failoverData)
        {
            var newService = data.Data;
            _serviceMap.TryGetValue(key, out var oldService);

            var diff = _instancesDiffer.DoDiff(oldService, newService);
            if (diff.HasDifferent())
            {
                _logger.LogInformation("[NA] failoverdata isChangedServiceInfo. newService: {ServiceName}",
                    newService.Name);
                OnInstancesChanged(new InstancesChangeEventArgs(
                    _notifierEventScope,
                    newService.Name,
                    newService.GroupName,
                    newService.Clusters,
                    newService.Hosts,
                    diff));
            }

            failoverMap[key] = newService;
        }

        if (!failoverMap.IsEmpty)
        {
            _serviceMap = failoverMap;
        }

        _failoverSwitchEnable = true;
    }

    /// <summary>
    /// 处理故障转移开关关闭
    /// </summary>
    private void HandleFailoverSwitchOff()
    {
        var serviceInfoMap = _serviceInfoMapGetter?.Invoke();
        if (serviceInfoMap == null)
        {
            _serviceMap.Clear();
            _failoverSwitchEnable = false;
            return;
        }

        foreach (var (key, oldService) in _serviceMap)
        {
            if (serviceInfoMap.TryGetValue(key, out var newService))
            {
                var diff = _instancesDiffer.DoDiff(oldService, newService);
                if (diff.HasDifferent())
                {
                    OnInstancesChanged(new InstancesChangeEventArgs(
                        _notifierEventScope,
                        newService.Name,
                        newService.GroupName,
                        newService.Clusters,
                        newService.Hosts,
                        diff));
                }
            }
        }

        _serviceMap.Clear();
        _failoverSwitchEnable = false;
    }

    /// <summary>
    /// 检查故障转移开关是否启用
    /// </summary>
    public bool IsFailoverSwitch() => _failoverSwitchEnable;

    /// <summary>
    /// 检查指定服务的故障转移开关是否启用
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    public bool IsFailoverSwitch(string serviceName)
    {
        return _failoverSwitchEnable &&
               _serviceMap.TryGetValue(serviceName, out var service) &&
               service.IpCount() > 0;
    }

    /// <summary>
    /// 获取故障转移服务信息
    /// </summary>
    /// <param name="key">服务键</param>
    public ServiceInfo GetService(string key)
    {
        if (_serviceMap.TryGetValue(key, out var serviceInfo))
        {
            return serviceInfo;
        }

        return new ServiceInfo { Name = key };
    }

    /// <summary>
    /// 触发实例变更事件
    /// </summary>
    protected virtual void OnInstancesChanged(InstancesChangeEventArgs e)
    {
        InstancesChanged?.Invoke(this, e);
    }

    /// <summary>
    /// 关闭
    /// </summary>
    public void Shutdown()
    {
        _logger.LogInformation("{ClassName} do shutdown begin", GetType().Name);
        _cts.Cancel();
        _logger.LogInformation("{ClassName} do shutdown stop", GetType().Name);
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

/// <summary>
/// 实例变更事件参数
/// </summary>
public class InstancesChangeEventArgs : EventArgs
{
    /// <summary>
    /// 事件作用域
    /// </summary>
    public string EventScope { get; }

    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// 分组名称
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// 集群
    /// </summary>
    public string? Clusters { get; }

    /// <summary>
    /// 实例列表
    /// </summary>
    public List<Instance> Hosts { get; }

    /// <summary>
    /// 实例差异
    /// </summary>
    public InstancesDiff Diff { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public InstancesChangeEventArgs(
        string eventScope,
        string serviceName,
        string groupName,
        string? clusters,
        List<Instance> hosts,
        InstancesDiff diff)
    {
        EventScope = eventScope;
        ServiceName = serviceName;
        GroupName = groupName;
        Clusters = clusters;
        Hosts = hosts;
        Diff = diff;
    }
}
