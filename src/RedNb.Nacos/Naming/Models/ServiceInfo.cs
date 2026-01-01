namespace RedNb.Nacos.Naming.Models;

/// <summary>
/// 服务信息
/// </summary>
public sealed class ServiceInfo
{
    /// <summary>
    /// 服务名称
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// 分组名称
    /// </summary>
    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = NacosConstants.DefaultGroup;

    /// <summary>
    /// 集群信息
    /// </summary>
    [JsonPropertyName("clusters")]
    public string Clusters { get; set; } = string.Empty;

    /// <summary>
    /// 缓存时间（毫秒）
    /// </summary>
    [JsonPropertyName("cacheMillis")]
    public long CacheMillis { get; set; } = 1000;

    /// <summary>
    /// 实例列表
    /// </summary>
    [JsonPropertyName("hosts")]
    public List<Instance> Hosts { get; set; } = new();

    /// <summary>
    /// 最后刷新时间
    /// </summary>
    [JsonPropertyName("lastRefTime")]
    public long LastRefTime { get; set; }

    /// <summary>
    /// 校验和
    /// </summary>
    [JsonPropertyName("checksum")]
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// 是否返回所有 IP
    /// </summary>
    [JsonPropertyName("allIPs")]
    public bool AllIPs { get; set; }

    /// <summary>
    /// 是否达到保护阈值
    /// </summary>
    [JsonPropertyName("reachProtectionThreshold")]
    public bool ReachProtectionThreshold { get; set; }

    /// <summary>
    /// 获取服务 Key
    /// </summary>
    public string GetServiceKey() => $"{GroupName}@@{Name}";

    /// <summary>
    /// 获取健康实例
    /// </summary>
    public List<Instance> GetHealthyInstances()
    {
        return Hosts.Where(h => h.Healthy && h.Enabled).ToList();
    }
}

/// <summary>
/// 服务列表响应
/// </summary>
public sealed class ServiceListResponse
{
    /// <summary>
    /// 服务数量
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// 服务名称列表
    /// </summary>
    [JsonPropertyName("doms")]
    public List<string> Services { get; set; } = new();
}
