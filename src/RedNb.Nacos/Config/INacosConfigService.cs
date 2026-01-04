namespace RedNb.Nacos.Config;

/// <summary>
/// Nacos 配置服务接口
/// </summary>
public interface INacosConfigService : IAsyncDisposable
{
    #region 配置读取

    /// <summary>
    /// 获取配置内容
    /// </summary>
    /// <param name="dataId">配置 ID</param>
    /// <param name="group">分组名称</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置内容</returns>
    Task<string?> GetConfigAsync(
        string dataId,
        string group,
        long timeoutMs = 3000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取配置并解析为指定类型
    /// </summary>
    Task<T?> GetConfigAsync<T>(
        string dataId,
        string group,
        long timeoutMs = 3000,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 获取配置并添加监听器
    /// </summary>
    Task<string?> GetConfigAndSignListenerAsync(
        string dataId,
        string group,
        IConfigListener listener,
        long timeoutMs = 3000,
        CancellationToken cancellationToken = default);

    #endregion

    #region 配置发布

    /// <summary>
    /// 发布配置
    /// </summary>
    Task<bool> PublishConfigAsync(
        string dataId,
        string group,
        string content,
        string? configType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布配置（带CAS）
    /// </summary>
    /// <param name="dataId">配置 ID</param>
    /// <param name="group">分组名称</param>
    /// <param name="content">配置内容</param>
    /// <param name="casMd5">CAS MD5值（当前配置的MD5）</param>
    /// <param name="configType">配置类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> PublishConfigCasAsync(
        string dataId,
        string group,
        string content,
        string casMd5,
        string? configType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除配置
    /// </summary>
    Task<bool> RemoveConfigAsync(
        string dataId,
        string group,
        CancellationToken cancellationToken = default);

    #endregion

    #region 配置监听

    /// <summary>
    /// 添加配置监听器
    /// </summary>
    Task AddListenerAsync(
        string dataId,
        string group,
        IConfigListener listener,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加配置监听器（Action 方式）
    /// </summary>
    Task AddListenerAsync(
        string dataId,
        string group,
        Action<string?> callback,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除配置监听器
    /// </summary>
    Task RemoveListenerAsync(
        string dataId,
        string group,
        IConfigListener listener,
        CancellationToken cancellationToken = default);

    #endregion

    #region 模糊订阅

    /// <summary>
    /// 模糊订阅配置
    /// </summary>
    /// <param name="dataIdPattern">配置 ID 模式（支持 * 通配符）</param>
    /// <param name="groupPattern">分组模式（支持 * 通配符）</param>
    /// <param name="listener">监听器</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task FuzzyWatchAsync(
        string dataIdPattern,
        string groupPattern,
        IConfigListener listener,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消模糊订阅
    /// </summary>
    Task CancelFuzzyWatchAsync(
        string dataIdPattern,
        string groupPattern,
        IConfigListener listener,
        CancellationToken cancellationToken = default);

    #endregion

    #region 健康检查

    /// <summary>
    /// 检查服务状态
    /// </summary>
    Task<string> GetServerStatusAsync(CancellationToken cancellationToken = default);

    #endregion
}
