namespace RedNb.Nacos.Remote.Grpc.Models;

/// <summary>
/// 实例请求响应
/// </summary>
public class InstanceResponse : NacosResponse
{
    public override string GetResponseType() => "InstanceResponse";
}

/// <summary>
/// 批量实例请求响应
/// </summary>
public class BatchInstanceResponse : NacosResponse
{
    public override string GetResponseType() => "BatchInstanceResponse";
}

/// <summary>
/// 服务查询响应
/// </summary>
public class QueryServiceResponse : NacosResponse
{
    /// <summary>
    /// 服务信息
    /// </summary>
    [JsonPropertyName("serviceInfo")]
    public GrpcServiceInfo? ServiceInfo { get; set; }

    public override string GetResponseType() => "QueryServiceResponse";
}

/// <summary>
/// 服务列表响应
/// </summary>
public class ServiceListResponse : NacosResponse
{
    /// <summary>
    /// 服务数量
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// 服务名称列表
    /// </summary>
    [JsonPropertyName("serviceNames")]
    public List<string>? ServiceNames { get; set; }

    public override string GetResponseType() => "ServiceListResponse";
}

/// <summary>
/// 订阅服务响应
/// </summary>
public class SubscribeServiceResponse : NacosResponse
{
    /// <summary>
    /// 服务信息
    /// </summary>
    [JsonPropertyName("serviceInfo")]
    public GrpcServiceInfo? ServiceInfo { get; set; }

    public override string GetResponseType() => "SubscribeServiceResponse";
}

/// <summary>
/// 通知订阅者响应
/// </summary>
public class NotifySubscriberResponse : NacosResponse
{
    public override string GetResponseType() => "NotifySubscriberResponse";
}
