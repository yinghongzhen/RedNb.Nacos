namespace RedNb.Nacos.Common.Failover;

/// <summary>
/// 配置快照接口
/// </summary>
public interface IConfigSnapshot
{
    /// <summary>
    /// 保存配置快照
    /// </summary>
    Task SaveSnapshotAsync(string dataId, string group, string tenant, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取配置快照
    /// </summary>
    Task<string?> GetSnapshotAsync(string dataId, string group, string tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除配置快照
    /// </summary>
    Task DeleteSnapshotAsync(string dataId, string group, string tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取配置MD5
    /// </summary>
    Task<string?> GetMd5Async(string dataId, string group, string tenant, CancellationToken cancellationToken = default);
}

/// <summary>
/// 服务快照接口
/// </summary>
public interface IServiceSnapshot
{
    /// <summary>
    /// 保存服务快照
    /// </summary>
    Task SaveSnapshotAsync(string serviceName, string groupName, string tenant, ServiceInfo serviceInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取服务快照
    /// </summary>
    Task<ServiceInfo?> GetSnapshotAsync(string serviceName, string groupName, string tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除服务快照
    /// </summary>
    Task DeleteSnapshotAsync(string serviceName, string groupName, string tenant, CancellationToken cancellationToken = default);
}
