using RedNb.Nacos.Remote.Grpc.Models;

namespace RedNb.Nacos.Remote.Grpc;

/// <summary>
/// Nacos gRPC 客户端接口
/// </summary>
public interface INacosGrpcClient : IAsyncDisposable
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 连接到服务器
    /// </summary>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送请求并等待响应
    /// </summary>
    Task<TResponse?> RequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : NacosRequest
        where TResponse : NacosResponse;

    /// <summary>
    /// 注册服务端推送处理器
    /// </summary>
    void RegisterPushHandler<TRequest>(Func<TRequest, Task<NacosResponse?>> handler)
        where TRequest : NacosRequest;

    /// <summary>
    /// 重新连接
    /// </summary>
    Task<bool> ReconnectAsync(CancellationToken cancellationToken = default);
}
