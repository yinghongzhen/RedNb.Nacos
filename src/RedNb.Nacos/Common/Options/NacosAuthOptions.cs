namespace RedNb.Nacos.Common.Options;

/// <summary>
/// 鉴权选项
/// </summary>
public sealed class NacosAuthOptions
{
    /// <summary>
    /// 是否启用鉴权
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Token 刷新间隔（秒）
    /// </summary>
    public int TokenRefreshIntervalSeconds { get; set; } = 1800;

    /// <summary>
    /// Token 过期提前刷新时间（秒）
    /// </summary>
    public int TokenRefreshAheadSeconds { get; set; } = 300;

    /// <summary>
    /// Token 请求重试次数
    /// </summary>
    public int TokenRetryCount { get; set; } = 3;

    /// <summary>
    /// Token 请求重试间隔（毫秒）
    /// </summary>
    public int TokenRetryIntervalMs { get; set; } = 1000;
}
