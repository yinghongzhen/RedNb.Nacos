namespace RedNb.Nacos.Remote.Grpc.Models;

/// <summary>
/// gRPC 请求基类
/// </summary>
public abstract class NacosRequest
{
    /// <summary>
    /// 请求ID
    /// </summary>
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 模块名称
    /// </summary>
    [JsonPropertyName("module")]
    public virtual string Module => "internal";

    /// <summary>
    /// 请求头
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// 获取请求类型名称
    /// </summary>
    public abstract string GetRequestType();
}

/// <summary>
/// 内部请求基类
/// </summary>
public abstract class InternalRequest : NacosRequest
{
    public override string Module => "internal";
}

/// <summary>
/// 配置请求基类
/// </summary>
public abstract class ConfigRequest : NacosRequest
{
    public override string Module => "config";
    
    /// <summary>
    /// 命名空间
    /// </summary>
    [JsonPropertyName("tenant")]
    public string Tenant { get; set; } = string.Empty;
}

/// <summary>
/// 服务请求基类
/// </summary>
public abstract class NamingRequest : NacosRequest
{
    public override string Module => "naming";

    /// <summary>
    /// 命名空间
    /// </summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// 服务名称
    /// </summary>
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    /// <summary>
    /// 分组名称
    /// </summary>
    [JsonPropertyName("groupName")]
    public string? GroupName { get; set; }
}
