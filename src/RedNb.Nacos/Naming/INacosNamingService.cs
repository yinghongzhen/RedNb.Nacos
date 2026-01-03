namespace RedNb.Nacos.Naming;

/// <summary>
/// Nacos 命名服务接口
/// </summary>
public interface INacosNamingService : IAsyncDisposable
{
    #region 服务注册

    /// <summary>
    /// 注册服务实例
    /// </summary>
    Task RegisterInstanceAsync(
        string serviceName,
        string groupName,
        Instance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 注册服务实例（简化版）
    /// </summary>
    Task RegisterInstanceAsync(
        string serviceName,
        string ip,
        int port,
        string? clusterName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 注销服务实例
    /// </summary>
    Task DeregisterInstanceAsync(
        string serviceName,
        string groupName,
        Instance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 注销服务实例（简化版）
    /// </summary>
    Task DeregisterInstanceAsync(
        string serviceName,
        string ip,
        int port,
        string? clusterName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实例信息
    /// </summary>
    Task UpdateInstanceAsync(
        string serviceName,
        string groupName,
        Instance instance,
        CancellationToken cancellationToken = default);

    #endregion

    #region 服务发现

    /// <summary>
    /// 获取所有服务实例
    /// </summary>
    Task<List<Instance>> GetAllInstancesAsync(
        string serviceName,
        string groupName = NacosConstants.DefaultGroup,
        bool subscribe = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取健康的服务实例
    /// </summary>
    Task<List<Instance>> GetHealthyInstancesAsync(
        string serviceName,
        string groupName = NacosConstants.DefaultGroup,
        bool subscribe = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 选择一个健康实例（负载均衡）
    /// </summary>
    Task<Instance?> SelectOneHealthyInstanceAsync(
        string serviceName,
        string groupName = NacosConstants.DefaultGroup,
        bool subscribe = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取服务信息
    /// </summary>
    Task<ServiceInfo?> GetServiceAsync(
        string serviceName,
        string groupName = NacosConstants.DefaultGroup,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取服务列表
    /// </summary>
    Task<List<string>> GetServicesAsync(
        string groupName = NacosConstants.DefaultGroup,
        int pageNo = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    #endregion

    #region 服务订阅

    /// <summary>
    /// 订阅服务
    /// </summary>
    Task SubscribeAsync(
        string serviceName,
        string groupName,
        IEventListener listener,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅服务（Action 方式）
    /// </summary>
    Task SubscribeAsync(
        string serviceName,
        string groupName,
        Action<ServiceInfo> callback,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消订阅
    /// </summary>
    Task UnsubscribeAsync(
        string serviceName,
        string groupName,
        IEventListener listener,
        CancellationToken cancellationToken = default);

    #endregion

    #region 健康检查

    /// <summary>
    /// 检查服务状态
    /// </summary>
    Task<string> GetServerStatusAsync(CancellationToken cancellationToken = default);

    #endregion

    #region 心跳

    /// <summary>
    /// 发送心跳
    /// </summary>
    Task<bool> SendHeartbeatAsync(
        string serviceName,
        string groupName,
        Instance instance,
        CancellationToken cancellationToken = default);

    #endregion
}
