namespace RedNb.Nacos.Naming.Models;

/// <summary>
/// 服务实例
/// </summary>
public sealed class Instance
{
    /// <summary>
    /// 实例 ID
    /// </summary>
    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    /// <summary>
    /// IP 地址
    /// </summary>
    [JsonPropertyName("ip")]
    public required string Ip { get; set; }

    /// <summary>
    /// 端口号
    /// </summary>
    [JsonPropertyName("port")]
    public required int Port { get; set; }

    /// <summary>
    /// 权重
    /// </summary>
    [JsonPropertyName("weight")]
    public double Weight { get; set; } = 1.0d;

    /// <summary>
    /// 是否健康
    /// </summary>
    [JsonPropertyName("healthy")]
    public bool Healthy { get; set; } = true;

    /// <summary>
    /// 是否可用
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否是临时实例
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
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 元数据
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// 获取完整地址
    /// </summary>
    public string ToInetAddr() => $"{Ip}:{Port}";

    /// <summary>
    /// 获取实例 Key
    /// </summary>
    public string GetInstanceKey() => $"{Ip}#{Port}#{ClusterName}";
}
