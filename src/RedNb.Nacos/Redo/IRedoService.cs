namespace RedNb.Nacos.Redo;

/// <summary>
/// Redo 服务接口
/// </summary>
public interface IRedoService
{
    /// <summary>
    /// 获取是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 注册 redo 数据
    /// </summary>
    void RegisterRedoData<T>(RedoData<T> redoData) where T : class;

    /// <summary>
    /// 获取指定类型的所有 redo 数据
    /// </summary>
    IEnumerable<RedoData<T>> GetRedoData<T>() where T : class;

    /// <summary>
    /// 移除 redo 数据
    /// </summary>
    void RemoveRedoData<T>(string key) where T : class;

    /// <summary>
    /// 处理连接事件
    /// </summary>
    void OnConnected();

    /// <summary>
    /// 处理断开连接事件
    /// </summary>
    void OnDisconnected();

    /// <summary>
    /// 启动 redo 任务
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止 redo 任务
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
