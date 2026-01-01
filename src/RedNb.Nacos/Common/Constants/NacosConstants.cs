namespace RedNb.Nacos.Common.Constants;

/// <summary>
/// Nacos 常量定义
/// </summary>
public static class NacosConstants
{
    /// <summary>
    /// 默认分组
    /// </summary>
    public const string DefaultGroup = "DEFAULT_GROUP";

    /// <summary>
    /// 默认集群
    /// </summary>
    public const string DefaultCluster = "DEFAULT";

    /// <summary>
    /// 默认命名空间
    /// </summary>
    public const string DefaultNamespace = "public";

    /// <summary>
    /// 默认超时时间（毫秒）
    /// </summary>
    public const int DefaultTimeoutMs = 3000;

    /// <summary>
    /// 默认长轮询超时时间（毫秒）
    /// </summary>
    public const int DefaultLongPollingTimeoutMs = 30000;

    /// <summary>
    /// gRPC 端口偏移量
    /// </summary>
    public const int GrpcPortOffset = 1000;

    /// <summary>
    /// 默认权重
    /// </summary>
    public const double DefaultWeight = 1.0d;

    /// <summary>
    /// 配置监听分隔符
    /// </summary>
    public const char ConfigListeningSeparator = (char)2;

    /// <summary>
    /// 配置分隔符
    /// </summary>
    public const char ConfigSeparator = (char)1;

    /// <summary>
    /// 行分隔符
    /// </summary>
    public const string LineSeparator = "\x01";

    /// <summary>
    /// 字分隔符
    /// </summary>
    public const string WordSeparator = "\x02";
}
