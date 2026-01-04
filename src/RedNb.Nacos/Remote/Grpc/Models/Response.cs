namespace RedNb.Nacos.Remote.Grpc.Models;

/// <summary>
/// gRPC 响应基类
/// </summary>
public abstract class NacosResponse
{
    /// <summary>
    /// 请求ID
    /// </summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    /// <summary>
    /// 结果码（200为成功）
    /// </summary>
    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    /// <summary>
    /// 错误码
    /// </summary>
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => ResultCode == 200;

    /// <summary>
    /// 获取响应类型名称
    /// </summary>
    public abstract string GetResponseType();
}

/// <summary>
/// 服务端检查响应
/// </summary>
public class ServerCheckResponse : NacosResponse
{
    /// <summary>
    /// 连接ID
    /// </summary>
    [JsonPropertyName("connectionId")]
    public string? ConnectionId { get; set; }

    public override string GetResponseType() => "ServerCheckResponse";
}

/// <summary>
/// 客户端检测响应
/// </summary>
public class ClientDetectionResponse : NacosResponse
{
    public override string GetResponseType() => "ClientDetectionResponse";
}

/// <summary>
/// 健康检查响应
/// </summary>
public class HealthCheckResponse : NacosResponse
{
    public override string GetResponseType() => "HealthCheckResponse";
}

/// <summary>
/// 连接设置响应
/// </summary>
public class ConnectResetResponse : NacosResponse
{
    public override string GetResponseType() => "ConnectResetResponse";
}

/// <summary>
/// 错误响应
/// </summary>
public class ErrorResponse : NacosResponse
{
    public override string GetResponseType() => "ErrorResponse";
}
