namespace RedNb.Nacos.Auth;

/// <summary>
/// 鉴权服务实现
/// </summary>
public sealed class AuthService : IAuthService, IDisposable
{
    private readonly NacosOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private AccessToken? _accessToken;
    private bool _disposed;

    public AuthService(
        IOptions<NacosOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<AuthService> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsAuthEnabled => _options.Auth.Enabled
        && !string.IsNullOrEmpty(_options.UserName)
        && !string.IsNullOrEmpty(_options.Password);

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAuthEnabled)
        {
            return null;
        }

        // 检查是否需要刷新
        if (_accessToken != null && !_accessToken.IsExpired(_options.Auth.TokenRefreshAheadSeconds))
        {
            return _accessToken.Token;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // 双重检查
            if (_accessToken != null && !_accessToken.IsExpired(_options.Auth.TokenRefreshAheadSeconds))
            {
                return _accessToken.Token;
            }

            await FetchTokenAsync(cancellationToken);
            return _accessToken?.Token;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAuthEnabled)
        {
            return;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await FetchTokenAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public void ClearToken()
    {
        _accessToken = null;
    }

    private async Task FetchTokenAsync(CancellationToken cancellationToken)
    {
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount < _options.Auth.TokenRetryCount)
        {
            try
            {
                var serverAddress = _options.GetRandomServerAddress();
                var url = $"{serverAddress}{EndpointConstants.Auth_Login}";

                _logger.LogDebug("正在获取 Nacos Token，服务器: {Server}", serverAddress);

                var client = _httpClientFactory.CreateClient("NacosAuth");
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["username"] = _options.UserName!,
                    ["password"] = _options.Password!
                });

                var response = await client.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new NacosAuthException(
                        $"获取 Token 失败，状态码: {response.StatusCode}，响应: {errorContent}");
                }

                var token = await response.Content.ReadFromJsonAsync<AccessToken>(cancellationToken);
                if (token == null || string.IsNullOrEmpty(token.Token))
                {
                    throw new NacosAuthException("获取 Token 失败，响应为空");
                }

                token.ObtainedAt = DateTimeOffset.UtcNow;
                _accessToken = token;

                _logger.LogInformation(
                    "成功获取 Nacos Token，用户: {Username}，有效期: {Ttl} 秒",
                    token.Username,
                    token.TokenTtl);

                return;
            }
            catch (Exception ex) when (ex is not NacosAuthException)
            {
                lastException = ex;
                retryCount++;

                _logger.LogWarning(
                    ex,
                    "获取 Nacos Token 失败，重试次数: {RetryCount}/{MaxRetry}",
                    retryCount,
                    _options.Auth.TokenRetryCount);

                if (retryCount < _options.Auth.TokenRetryCount)
                {
                    await Task.Delay(_options.Auth.TokenRetryIntervalMs, cancellationToken);
                }
            }
        }

        throw new NacosAuthException(
            $"获取 Nacos Token 失败，已重试 {retryCount} 次",
            lastException!);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _semaphore.Dispose();
    }
}
