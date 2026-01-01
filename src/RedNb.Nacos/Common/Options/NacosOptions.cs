namespace RedNb.Nacos.Common.Options;

/// <summary>
/// Nacos 主配置选项
/// </summary>
public sealed class NacosOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "RedNb:Nacos";

    /// <summary>
    /// 服务端地址列表
    /// </summary>
    public required List<string> ServerAddresses { get; set; }

    /// <summary>
    /// 命名空间 ID
    /// </summary>
    public string Namespace { get; set; } = NacosConstants.DefaultNamespace;

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Access Key（阿里云 MSE）
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// Secret Key（阿里云 MSE）
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// 默认超时时间（毫秒）
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = NacosConstants.DefaultTimeoutMs;

    /// <summary>
    /// 是否使用 gRPC
    /// </summary>
    public bool UseGrpc { get; set; } = true;

    /// <summary>
    /// gRPC 端口偏移量（相对于 HTTP 端口）
    /// </summary>
    public int GrpcPortOffset { get; set; } = NacosConstants.GrpcPortOffset;

    /// <summary>
    /// 配置中心选项
    /// </summary>
    public NacosConfigOptions Config { get; set; } = new();

    /// <summary>
    /// 服务注册选项
    /// </summary>
    public NacosNamingOptions Naming { get; set; } = new();

    /// <summary>
    /// 鉴权选项
    /// </summary>
    public NacosAuthOptions Auth { get; set; } = new();

    /// <summary>
    /// 获取随机服务器地址
    /// </summary>
    public string GetRandomServerAddress()
    {
        if (ServerAddresses == null || ServerAddresses.Count == 0)
        {
            throw new NacosException("ServerAddresses 不能为空");
        }

        var index = Random.Shared.Next(ServerAddresses.Count);
        return ServerAddresses[index].TrimEnd('/');
    }
}
