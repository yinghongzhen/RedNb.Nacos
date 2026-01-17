using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Naming.Redo;

/// <summary>
/// 命名服务 gRPC 客户端代理接口
/// 用于 Redo 任务执行实际的注册/注销/订阅操作
/// 参考 Java SDK: com.alibaba.nacos.client.naming.remote.gprc.NamingGrpcClientProxy
/// </summary>
public interface INamingGrpcClientProxy
{
    /// <summary>
    /// 是否启用
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 执行服务注册
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="instance">实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DoRegisterServiceAsync(string serviceName, string groupName, Instance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行批量服务注册
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="instances">实例列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DoBatchRegisterServiceAsync(string serviceName, string groupName, List<Instance> instances, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行服务注销
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="instance">实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DoDeregisterServiceAsync(string serviceName, string groupName, Instance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行订阅
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="cluster">集群</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DoSubscribeAsync(string serviceName, string groupName, string cluster, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行取消订阅
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="cluster">集群</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DoUnsubscribeAsync(string serviceName, string groupName, string cluster, CancellationToken cancellationToken = default);
}
