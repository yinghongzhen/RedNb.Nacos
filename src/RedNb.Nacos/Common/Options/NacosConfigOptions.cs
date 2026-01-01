namespace RedNb.Nacos.Common.Options;

/// <summary>
/// 配置中心选项
/// </summary>
public sealed class NacosConfigOptions
{
    /// <summary>
    /// 配置监听列表
    /// </summary>
    public List<ConfigListenerItem> Listeners { get; set; } = new();

    /// <summary>
    /// 长轮询超时时间（毫秒）
    /// </summary>
    public int LongPollingTimeout { get; set; } = NacosConstants.DefaultLongPollingTimeoutMs;

    /// <summary>
    /// 是否启用快照
    /// </summary>
    public bool EnableSnapshot { get; set; } = true;

    /// <summary>
    /// 快照文件路径
    /// </summary>
    public string SnapshotPath { get; set; } = "nacos/snapshot";

    /// <summary>
    /// 是否启用容灾
    /// </summary>
    public bool EnableFailover { get; set; } = true;

    /// <summary>
    /// 容灾文件路径
    /// </summary>
    public string FailoverPath { get; set; } = "nacos/failover";

    /// <summary>
    /// 配置刷新间隔（毫秒）
    /// </summary>
    public int RefreshIntervalMs { get; set; } = 3000;
}

/// <summary>
/// 配置监听项
/// </summary>
public sealed class ConfigListenerItem
{
    /// <summary>
    /// 配置 ID
    /// </summary>
    public required string DataId { get; set; }

    /// <summary>
    /// 分组名称
    /// </summary>
    public string Group { get; set; } = NacosConstants.DefaultGroup;

    /// <summary>
    /// 是否可选（可选配置在获取失败时不抛出异常）
    /// </summary>
    public bool Optional { get; set; } = false;

    /// <summary>
    /// 配置类型（json, yaml, properties 等）
    /// </summary>
    public string? ConfigType { get; set; }
}
