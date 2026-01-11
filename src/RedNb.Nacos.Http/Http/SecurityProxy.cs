using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;

namespace RedNb.Nacos.Client.Http;

/// <summary>
/// Handles authentication with Nacos server.
/// </summary>
public class SecurityProxy : IDisposable
{
    private readonly NacosClientOptions _options;
    private readonly ILogger? _logger;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _loginLock = new(1, 1);
    
    private string? _accessToken;
    private long _tokenTtl;
    private long _lastRefreshTime;
    private bool _disposed;

    private const long TokenRefreshWindow = 120000; // 2 minutes before expiry

    public SecurityProxy(NacosClientOptions options, ILogger? logger = null)
    {
        _options = options;
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(options.DefaultTimeout)
        };
    }

    /// <summary>
    /// Gets the access token, refreshing if necessary.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password))
        {
            return null;
        }

        if (IsTokenValid())
        {
            return _accessToken;
        }

        await _loginLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (IsTokenValid())
            {
                return _accessToken;
            }

            await LoginAsync(cancellationToken);
            return _accessToken;
        }
        finally
        {
            _loginLock.Release();
        }
    }

    private bool IsTokenValid()
    {
        if (string.IsNullOrEmpty(_accessToken))
        {
            return false;
        }

        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var expiryTime = _lastRefreshTime + (_tokenTtl * 1000) - TokenRefreshWindow;
        return currentTime < expiryTime;
    }

    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        var servers = _options.GetServerAddressList();
        Exception? lastException = null;

        foreach (var server in servers)
        {
            try
            {
                var baseUrl = _options.GetBaseUrl(server);
                var loginUrl = $"{baseUrl}/v1/auth/login";

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "username", _options.Username! },
                    { "password", _options.Password! }
                });

                var response = await _httpClient.PostAsync(loginUrl, content, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseBody);
                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                    {
                        _accessToken = loginResponse.AccessToken;
                        _tokenTtl = loginResponse.TokenTtl;
                        _lastRefreshTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        _logger?.LogDebug("Successfully logged in to Nacos server, token TTL: {Ttl}s", _tokenTtl);
                        return;
                    }
                }

                _logger?.LogWarning("Login failed for server {Server}: {Response}", server, responseBody);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger?.LogWarning(ex, "Login failed for server {Server}", server);
            }
        }

        throw new NacosException(NacosException.NoRight, 
            $"Failed to login to Nacos server: {lastException?.Message}", lastException!);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _loginLock.Dispose();
        _disposed = true;
    }

    private class LoginResponse
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("tokenTtl")]
        public long TokenTtl { get; set; }

        [JsonPropertyName("globalAdmin")]
        public bool GlobalAdmin { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }
}
