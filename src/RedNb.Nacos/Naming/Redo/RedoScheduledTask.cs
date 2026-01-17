using Microsoft.Extensions.Logging;
using RedNb.Nacos.Naming.Redo.Data;
using RedNb.Nacos.Redo;

namespace RedNb.Nacos.Naming.Redo;

/// <summary>
/// Redo 调度任务
/// 参考 Java SDK: com.alibaba.nacos.client.naming.remote.gprc.redo.RedoScheduledTask
/// </summary>
public class RedoScheduledTask
{
    private readonly ILogger _logger;
    private readonly INamingGrpcClientProxy _clientProxy;
    private readonly NamingGrpcRedoService _redoService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientProxy">客户端代理</param>
    /// <param name="redoService">Redo 服务</param>
    public RedoScheduledTask(ILogger logger, INamingGrpcClientProxy clientProxy, NamingGrpcRedoService redoService)
    {
        _logger = logger;
        _clientProxy = clientProxy;
        _redoService = redoService;
    }

    /// <summary>
    /// 执行 Redo 任务
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!_redoService.IsConnected)
        {
            _logger.LogWarning("Grpc Connection is disconnect, skip current redo task");
            return;
        }

        try
        {
            await RedoForInstancesAsync(cancellationToken);
            await RedoForSubscribesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redo task run with unexpected exception");
        }
    }

    /// <summary>
    /// 实例 Redo 操作
    /// </summary>
    private async Task RedoForInstancesAsync(CancellationToken cancellationToken)
    {
        foreach (var each in _redoService.FindInstanceRedoData())
        {
            try
            {
                await RedoForInstanceAsync(each, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redo instance operation {RedoType} for {GroupName}@@{ServiceName} failed",
                    each.GetRedoType(), each.GroupName, each.ServiceName);
            }
        }
    }

    /// <summary>
    /// 单个实例 Redo 操作
    /// </summary>
    private async Task RedoForInstanceAsync(InstanceRedoData redoData, CancellationToken cancellationToken)
    {
        var redoType = redoData.GetRedoType();
        var serviceName = redoData.ServiceName;
        var groupName = redoData.GroupName;
        
        _logger.LogInformation("Redo instance operation {RedoType} for {GroupName}@@{ServiceName}",
            redoType, groupName, serviceName);

        switch (redoType)
        {
            case RedoType.Register:
                if (IsClientDisabled())
                {
                    return;
                }
                await ProcessRegisterRedoTypeAsync(redoData, serviceName, groupName, cancellationToken);
                break;

            case RedoType.Unregister:
                if (IsClientDisabled())
                {
                    return;
                }
                var instance = redoData.Get();
                if (instance != null)
                {
                    await _clientProxy.DoDeregisterServiceAsync(serviceName, groupName, instance, cancellationToken);
                }
                break;

            case RedoType.Remove:
                _redoService.RemoveInstanceForRedo(serviceName, groupName);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// 处理注册类型的 Redo
    /// </summary>
    private async Task ProcessRegisterRedoTypeAsync(InstanceRedoData redoData, string serviceName, string groupName, CancellationToken cancellationToken)
    {
        if (redoData is BatchInstanceRedoData batchRedoData)
        {
            // 执行批量注册
            await _clientProxy.DoBatchRegisterServiceAsync(serviceName, groupName, batchRedoData.Instances, cancellationToken);
            return;
        }

        var instance = redoData.Get();
        if (instance != null)
        {
            await _clientProxy.DoRegisterServiceAsync(serviceName, groupName, instance, cancellationToken);
        }
    }

    /// <summary>
    /// 订阅者 Redo 操作
    /// </summary>
    private async Task RedoForSubscribesAsync(CancellationToken cancellationToken)
    {
        foreach (var each in _redoService.FindSubscriberRedoData())
        {
            try
            {
                await RedoForSubscribeAsync(each, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redo subscriber operation {RedoType} for {GroupName}@@{ServiceName}#{Cluster} failed",
                    each.GetRedoType(), each.GroupName, each.ServiceName, each.Get());
            }
        }
    }

    /// <summary>
    /// 单个订阅者 Redo 操作
    /// </summary>
    private async Task RedoForSubscribeAsync(SubscriberRedoData redoData, CancellationToken cancellationToken)
    {
        var redoType = redoData.GetRedoType();
        var serviceName = redoData.ServiceName;
        var groupName = redoData.GroupName;
        var cluster = redoData.Get() ?? string.Empty;

        _logger.LogInformation("Redo subscriber operation {RedoType} for {GroupName}@@{ServiceName}#{Cluster}",
            redoType, groupName, serviceName, cluster);

        switch (redoType)
        {
            case RedoType.Register:
                if (IsClientDisabled())
                {
                    return;
                }
                await _clientProxy.DoSubscribeAsync(serviceName, groupName, cluster, cancellationToken);
                break;

            case RedoType.Unregister:
                if (IsClientDisabled())
                {
                    return;
                }
                await _clientProxy.DoUnsubscribeAsync(serviceName, groupName, cluster, cancellationToken);
                break;

            case RedoType.Remove:
                _redoService.RemoveSubscriberForRedo(serviceName, groupName, cluster);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// 判断客户端是否禁用
    /// </summary>
    private bool IsClientDisabled()
    {
        return !_clientProxy.IsEnabled;
    }
}
