namespace RedNb.Nacos.Common.Options;

/// <summary>
/// 服务注册选项
/// </summary>
public sealed class NacosNamingOptions
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// 分组名称
    /// </summary>
    public string GroupName { get; set; } = NacosConstants.DefaultGroup;

    /// <summary>
    /// 集群名称
    /// </summary>
    public string ClusterName { get; set; } = NacosConstants.DefaultCluster;

    /// <summary>
    /// IP 地址（留空自动获取）
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// 优先网络前缀（用于多网卡场景）
    /// </summary>
    public string? PreferredNetworks { get; set; }

    /// <summary>
    /// 端口号（0 表示自动获取）
    /// </summary>
    public int Port { get; set; } = 0;

    /// <summary>
    /// 权重
    /// </summary>
    public double Weight { get; set; } = 100;

    /// <summary>
    /// 是否启用注册
    /// </summary>
    public bool RegisterEnabled { get; set; } = true;

    /// <summary>
    /// 实例是否可用
    /// </summary>
    public bool InstanceEnabled { get; set; } = true;

    /// <summary>
    /// 是否是临时实例
    /// </summary>
    public bool Ephemeral { get; set; } = true;

    /// <summary>
    /// 是否使用 HTTPS
    /// </summary>
    public bool Secure { get; set; } = false;

    /// <summary>
    /// 负载均衡策略
    /// </summary>
    public LoadBalancerStrategy LoadBalancerStrategy { get; set; } = LoadBalancerStrategy.WeightedRandom;

    /// <summary>
    /// 心跳间隔（毫秒）
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 5000;

    /// <summary>
    /// 心跳超时（毫秒）
    /// </summary>
    public int HeartbeatTimeoutMs { get; set; } = 15000;

    /// <summary>
    /// 实例删除超时（毫秒）
    /// </summary>
    public int IpDeleteTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// 元数据
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// 负载均衡策略
/// </summary>
public enum LoadBalancerStrategy
{
    /// <summary>
    /// 随机
    /// </summary>
    Random,

    /// <summary>
    /// 轮询
    /// </summary>
    RoundRobin,

    /// <summary>
    /// 加权随机
    /// </summary>
    WeightedRandom,

    /// <summary>
    /// 加权轮询
    /// </summary>
    WeightedRoundRobin
}
