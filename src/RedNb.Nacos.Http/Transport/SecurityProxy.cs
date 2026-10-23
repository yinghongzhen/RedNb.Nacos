using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RedNb.Nacos;
using RedNb.Nacos.Http.Serialization;

namespace RedNb.Nacos.Http.Transport;

/// <summary>
/// Authenticates against the default Nacos 3.2.4 plugin using username/password,
/// caches the returned token, and refreshes it before expiry. AK/SK-only login
/// requires a different plugin and is explicitly rejected by this implementation.
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
        _httpClient = new HttpClient(RedNb.Nacos.Utils.NacosTransport.CreateHandler(options))
        {
            Timeout = TimeSpan.FromMilliseconds(options.DefaultTimeout)
        };
    }

    /// <summary>
    /// Gets the access token, refreshing if necessary.
    /// Returns <c>null</c> only when neither AccessKey/SecretKey nor
    /// Username/Password is configured (anonymous access).
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var loginStrategy = ResolveLoginStrategy();
        if (loginStrategy == LoginStrategy.None)
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

            await LoginAsync(loginStrategy, cancellationToken);
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

    private LoginStrategy ResolveLoginStrategy()
    {
        if (string.IsNullOrWhiteSpace(_options.Username) != string.IsNullOrWhiteSpace(_options.Password))
            throw new NacosException(NacosException.InvalidParam, "Username and Password must both be configured.");
        var hasAk = !string.IsNullOrWhiteSpace(_options.AccessKey)
            && !string.IsNullOrWhiteSpace(_options.SecretKey);
        var hasUp = !string.IsNullOrWhiteSpace(_options.Username)
            && !string.IsNullOrWhiteSpace(_options.Password);
        // Default Nacos 3.2.4 authenticates with username/password or a token.
        // Cloud-specific AK/SK schemes require a separate authentication plugin.
        if (hasUp) return LoginStrategy.UsernamePassword;
        if (hasAk) throw new NotSupportedException(
            "AK/SK login is not supported by the default Nacos 3.2.4 authentication plugin. Configure Username/Password.");
        return LoginStrategy.None;
    }

    private async Task LoginAsync(LoginStrategy strategy, CancellationToken cancellationToken)
    {
        switch (strategy)
        {
            case LoginStrategy.UsernamePassword:
                await LoginWithUsernamePasswordAsync(cancellationToken);
                break;
            default:
                throw new NacosException(
                    NacosException.InvalidParam,
                    "Either AccessKey/SecretKey or Username/Password must be configured");
        }
    }

    private async Task LoginWithUsernamePasswordAsync(CancellationToken cancellationToken)
    {
        var servers = _options.GetServerAddressList();
        Exception? lastException = null;

        foreach (var server in servers)
        {
            try
            {
                var baseUrl = _options.GetBaseUrl(server);
                var loginUrl = $"{baseUrl}/v3/auth/user/login";

                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "username", _options.Username! },
                    { "password", _options.Password! }
                });

                using var response = await _httpClient.PostAsync(loginUrl, content, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize(responseBody, NacosHttpInternalJsonContext.Default.LoginResponse);
                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                    {
                        _accessToken = loginResponse.AccessToken;
                        _tokenTtl = loginResponse.TokenTtl;
                        _lastRefreshTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        _logger?.LogDebug("Successfully logged in to Nacos server, token TTL: {Ttl}s", _tokenTtl);
                        return;
                    }
                }

                _logger?.LogWarning("Login failed for server {Server}, HTTP {Status}", server, response.StatusCode);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
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

    private enum LoginStrategy
    {
        None,
        UsernamePassword
    }
}

/// <summary>
/// Legacy deterministic signing utility. It is not used by the default
/// Nacos authentication implementation and does not imply AK/SK server support.
/// </summary>
public static class SignatureUtils
{
    /// <summary>
    /// Computes the AK/SK signature for a login request.
    /// </summary>
    /// <param name="accessKey">The configured AccessKey.</param>
    /// <param name="secretKey">The configured SecretKey.</param>
    /// <param name="timestamp">13-digit unix-millis timestamp as a string.</param>
    /// <returns>Base64-encoded HMAC-SHA1 signature.</returns>
    public static string SignRequest(string accessKey, string secretKey, string timestamp)
    {
        if (string.IsNullOrEmpty(accessKey)) throw new ArgumentException("accessKey is required", nameof(accessKey));
        if (string.IsNullOrEmpty(secretKey)) throw new ArgumentException("secretKey is required", nameof(secretKey));
        if (string.IsNullOrEmpty(timestamp)) throw new ArgumentException("timestamp is required", nameof(timestamp));

        var data = Encoding.UTF8.GetBytes(accessKey + timestamp);
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }
}
