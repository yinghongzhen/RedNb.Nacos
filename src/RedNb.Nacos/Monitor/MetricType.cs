namespace RedNb.Nacos.Monitor;

/// <summary>
/// 指标类型枚举
/// </summary>
public enum MetricType
{
    /// <summary>
    /// 计量器（当前值）
    /// </summary>
    Gauge,

    /// <summary>
    /// 计数器（累积值）
    /// </summary>
    Counter,

    /// <summary>
    /// 直方图（分布）
    /// </summary>
    Histogram
}
