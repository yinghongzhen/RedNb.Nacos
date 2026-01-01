namespace RedNb.Nacos.Auth;

/// <summary>
/// 鉴权服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 获取 Access Token
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Access Token</returns>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新 Token
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否需要鉴权
    /// </summary>
    bool IsAuthEnabled { get; }

    /// <summary>
    /// 清除缓存的 Token
    /// </summary>
    void ClearToken();
}
