namespace RedNb.Nacos.Auth;

/// <summary>
/// Access Token 模型
/// </summary>
public sealed class AccessToken
{
    /// <summary>
    /// Token 值
    /// </summary>
    [JsonPropertyName("accessToken")]
    public string? Token { get; set; }

    /// <summary>
    /// Token 过期时间（秒）
    /// </summary>
    [JsonPropertyName("tokenTtl")]
    public long TokenTtl { get; set; }

    /// <summary>
    /// 是否全局管理员
    /// </summary>
    [JsonPropertyName("globalAdmin")]
    public bool GlobalAdmin { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Token 获取时间
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset ObtainedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 检查 Token 是否过期
    /// </summary>
    /// <param name="aheadSeconds">提前刷新秒数</param>
    /// <returns>是否过期</returns>
    public bool IsExpired(int aheadSeconds = 300)
    {
        if (string.IsNullOrEmpty(Token) || TokenTtl <= 0)
        {
            return true;
        }

        var expireAt = ObtainedAt.AddSeconds(TokenTtl - aheadSeconds);
        return DateTimeOffset.UtcNow >= expireAt;
    }
}
