namespace RedNb.Nacos.Remote.Grpc.Models;

/// <summary>
/// 服务端检查请求
/// </summary>
public class ServerCheckRequest : InternalRequest
{
    public override string GetRequestType() => "ServerCheckRequest";
}

/// <summary>
/// 连接设置请求
/// </summary>
public class ConnectionSetupRequest : InternalRequest
{
    /// <summary>
    /// 客户端版本
    /// </summary>
    [JsonPropertyName("clientVersion")]
    public string ClientVersion { get; set; } = "Nacos-CSharp-Client:v1.0.0";

    /// <summary>
    /// 能力表
    /// </summary>
    [JsonPropertyName("abilities")]
    public ClientAbilities? Abilities { get; set; }

    /// <summary>
    /// 命名空间
    /// </summary>
    [JsonPropertyName("tenant")]
    public string Tenant { get; set; } = string.Empty;

    /// <summary>
    /// 标签
    /// </summary>
    [JsonPropertyName("labels")]
    public Dictionary<string, string> Labels { get; set; } = new();

    public override string GetRequestType() => "ConnectionSetupRequest";
}

/// <summary>
/// 客户端能力
/// </summary>
public class ClientAbilities
{
    /// <summary>
    /// 远程能力
    /// </summary>
    [JsonPropertyName("remoteAbility")]
    public RemoteAbility? RemoteAbility { get; set; }

    /// <summary>
    /// 配置能力
    /// </summary>
    [JsonPropertyName("configAbility")]
    public ConfigAbility? ConfigAbility { get; set; }

    /// <summary>
    /// 命名能力
    /// </summary>
    [JsonPropertyName("namingAbility")]
    public NamingAbility? NamingAbility { get; set; }
}

/// <summary>
/// 远程能力
/// </summary>
public class RemoteAbility
{
    /// <summary>
    /// 支持远程连接
    /// </summary>
    [JsonPropertyName("supportRemoteConnection")]
    public bool SupportRemoteConnection { get; set; } = true;
}

/// <summary>
/// 配置能力
/// </summary>
public class ConfigAbility
{
    /// <summary>
    /// 支持远程指标
    /// </summary>
    [JsonPropertyName("supportRemoteMetrics")]
    public bool SupportRemoteMetrics { get; set; } = true;
}

/// <summary>
/// 命名能力
/// </summary>
public class NamingAbility
{
    /// <summary>
    /// 支持增量实例
    /// </summary>
    [JsonPropertyName("supportDeltaPush")]
    public bool SupportDeltaPush { get; set; } = true;

    /// <summary>
    /// 支持远程指标
    /// </summary>
    [JsonPropertyName("supportRemoteMetric")]
    public bool SupportRemoteMetric { get; set; } = true;
}

/// <summary>
/// 健康检查请求
/// </summary>
public class HealthCheckRequest : InternalRequest
{
    public override string GetRequestType() => "HealthCheckRequest";
}

/// <summary>
/// 客户端检测请求（服务端推送）
/// </summary>
public class ClientDetectionRequest : InternalRequest
{
    public override string GetRequestType() => "ClientDetectionRequest";
}

/// <summary>
/// 连接重置请求（服务端推送）
/// </summary>
public class ConnectResetRequest : InternalRequest
{
    /// <summary>
    /// 服务器IP
    /// </summary>
    [JsonPropertyName("serverIp")]
    public string? ServerIp { get; set; }

    /// <summary>
    /// 服务器端口
    /// </summary>
    [JsonPropertyName("serverPort")]
    public string? ServerPort { get; set; }

    public override string GetRequestType() => "ConnectResetRequest";
}
