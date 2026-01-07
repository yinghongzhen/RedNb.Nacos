namespace RedNb.Nacos.Remote.Grpc;

/// <summary>
/// gRPC 连接状态
/// </summary>
public enum EConnectionStatus
{
    /// <summary>
    /// 未连接
    /// </summary>
    Disconnected,

    /// <summary>
    /// 连接中
    /// </summary>
    Connecting,

    /// <summary>
    /// 已连接
    /// </summary>
    Connected,

    /// <summary>
    /// 重连中
    /// </summary>
    Reconnecting
}
