namespace RedNb.Nacos.Remote.Grpc.Models;

/// <summary>
/// 实例注册请求
/// </summary>
public class InstanceRequest : NamingRequest
{
    /// <summary>
    /// 请求类型：注册/注销
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "registerInstance";

    /// <summary>
    /// 实例信息
    /// </summary>
    [JsonPropertyName("instance")]
    public GrpcInstance? Instance { get; set; }

    public override string GetRequestType() => "InstanceRequest";
}

/// <summary>
/// 批量实例请求
/// </summary>
public class BatchInstanceRequest : NamingRequest
{
    /// <summary>
    /// 请求类型：批量注册/批量注销
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "batchRegisterInstance";

    /// <summary>
    /// 实例列表
    /// </summary>
    [JsonPropertyName("instances")]
    public List<GrpcInstance>? Instances { get; set; }

    public override string GetRequestType() => "BatchInstanceRequest";
}

/// <summary>
/// gRPC 实例模型
/// </summary>
public class GrpcInstance
{
    /// <summary>
    /// 实例ID
    /// </summary>
    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// 端口
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }

    /// <summary>
    /// 权重
    /// </summary>
    [JsonPropertyName("weight")]
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// 是否健康
    /// </summary>
    [JsonPropertyName("healthy")]
    public bool Healthy { get; set; } = true;

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否临时实例
    /// </summary>
    [JsonPropertyName("ephemeral")]
    public bool Ephemeral { get; set; } = true;

    /// <summary>
    /// 集群名称
    /// </summary>
    [JsonPropertyName("clusterName")]
    public string ClusterName { get; set; } = NacosConstants.DefaultCluster;

    /// <summary>
    /// 服务名称
    /// </summary>
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    /// <summary>
    /// 元数据
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// 从 Instance 转换
    /// </summary>
    public static GrpcInstance FromInstance(Instance instance)
    {
        return new GrpcInstance
        {
            InstanceId = instance.InstanceId,
            Ip = instance.Ip,
            Port = instance.Port,
            Weight = instance.Weight,
            Healthy = instance.Healthy,
            Enabled = instance.Enabled,
            Ephemeral = instance.Ephemeral,
            ClusterName = instance.ClusterName,
            ServiceName = instance.ServiceName,
            Metadata = instance.Metadata.Count > 0 ? new Dictionary<string, string>(instance.Metadata) : null
        };
    }

    /// <summary>
    /// 转换为 Instance
    /// </summary>
    public Instance ToInstance()
    {
        return new Instance
        {
            InstanceId = InstanceId,
            Ip = Ip,
            Port = Port,
            Weight = Weight,
            Healthy = Healthy,
            Enabled = Enabled,
            Ephemeral = Ephemeral,
            ClusterName = ClusterName,
            ServiceName = ServiceName,
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : new()
        };
    }
}

/// <summary>
/// 服务查询请求
/// </summary>
public class ServiceQueryRequest : NamingRequest
{
    /// <summary>
    /// 集群名称
    /// </summary>
    [JsonPropertyName("cluster")]
    public string? Cluster { get; set; }

    /// <summary>
    /// 是否只返回健康实例
    /// </summary>
    [JsonPropertyName("healthyOnly")]
    public bool HealthyOnly { get; set; }

    /// <summary>
    /// UDP端口
    /// </summary>
    [JsonPropertyName("udpPort")]
    public int UdpPort { get; set; }

    public override string GetRequestType() => "ServiceQueryRequest";
}

/// <summary>
/// 服务列表查询请求
/// </summary>
public class ServiceListRequest : NamingRequest
{
    /// <summary>
    /// 页码
    /// </summary>
    [JsonPropertyName("pageNo")]
    public int PageNo { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 100;

    /// <summary>
    /// 选择器
    /// </summary>
    [JsonPropertyName("selector")]
    public string? Selector { get; set; }

    public override string GetRequestType() => "ServiceListRequest";
}

/// <summary>
/// 订阅服务请求
/// </summary>
public class SubscribeServiceRequest : NamingRequest
{
    /// <summary>
    /// 是否订阅（false表示取消订阅）
    /// </summary>
    [JsonPropertyName("subscribe")]
    public bool Subscribe { get; set; } = true;

    /// <summary>
    /// 集群
    /// </summary>
    [JsonPropertyName("clusters")]
    public string? Clusters { get; set; }

    public override string GetRequestType() => "SubscribeServiceRequest";
}

/// <summary>
/// 服务变更推送请求（服务端推送）
/// </summary>
public class NotifySubscriberRequest : NamingRequest
{
    /// <summary>
    /// 服务信息
    /// </summary>
    [JsonPropertyName("serviceInfo")]
    public GrpcServiceInfo? ServiceInfo { get; set; }

    public override string GetRequestType() => "NotifySubscriberRequest";
}

/// <summary>
/// gRPC 服务信息
/// </summary>
public class GrpcServiceInfo
{
    /// <summary>
    /// 服务名称
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 分组名称
    /// </summary>
    [JsonPropertyName("groupName")]
    public string? GroupName { get; set; }

    /// <summary>
    /// 集群
    /// </summary>
    [JsonPropertyName("clusters")]
    public string? Clusters { get; set; }

    /// <summary>
    /// 缓存毫秒数
    /// </summary>
    [JsonPropertyName("cacheMillis")]
    public long CacheMillis { get; set; }

    /// <summary>
    /// 实例列表
    /// </summary>
    [JsonPropertyName("hosts")]
    public List<GrpcInstance>? Hosts { get; set; }

    /// <summary>
    /// 最后刷新时间
    /// </summary>
    [JsonPropertyName("lastRefTime")]
    public long LastRefTime { get; set; }

    /// <summary>
    /// 校验和
    /// </summary>
    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }

    /// <summary>
    /// 是否全部健康
    /// </summary>
    [JsonPropertyName("allIPs")]
    public bool AllIPs { get; set; }

    /// <summary>
    /// 转换为 ServiceInfo
    /// </summary>
    public ServiceInfo ToServiceInfo()
    {
        return new ServiceInfo
        {
            Name = Name,
            GroupName = GroupName,
            Clusters = Clusters,
            CacheMillis = CacheMillis,
            Hosts = Hosts?.Select(h => h.ToInstance()).ToList() ?? new List<Instance>(),
            LastRefTime = LastRefTime,
            Checksum = Checksum,
            AllIPs = AllIPs
        };
    }
}
