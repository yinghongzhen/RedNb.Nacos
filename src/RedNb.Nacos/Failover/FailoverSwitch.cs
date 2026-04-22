namespace RedNb.Nacos.Failover;

/// <summary>
/// 故障转移开关
/// </summary>
public class FailoverSwitch
{
    /// <summary>
    /// 是否启用故障转移
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 创建启用状态的开关
    /// </summary>
    public static FailoverSwitch CreateEnabled() => new() { Enabled = true };

    /// <summary>
    /// 创建禁用状态的开关
    /// </summary>
    public static FailoverSwitch CreateDisabled() => new() { Enabled = false };
}
