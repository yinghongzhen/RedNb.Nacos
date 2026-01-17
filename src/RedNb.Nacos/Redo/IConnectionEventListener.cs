namespace RedNb.Nacos.Redo;

/// <summary>
/// Redo 服务连接事件监听器接口
/// 参考 Java SDK: com.alibaba.nacos.common.remote.client.ConnectionEventListener
/// </summary>
public interface IConnectionEventListener
{
    /// <summary>
    /// 获取是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 连接成功事件
    /// </summary>
    void OnConnected();

    /// <summary>
    /// 连接断开事件
    /// </summary>
    void OnDisconnect();
}
