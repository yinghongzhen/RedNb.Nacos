using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Utils;

namespace RedNb.Nacos.Client.Http;

/// <summary>
/// HTTP client for Nacos server communication.
/// </summary>
public class NacosHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly NacosClientOptions _options;
    private readonly ILogger? _logger;
    private readonly ServerListManager _serverListManager;
    private readonly SecurityProxy _securityProxy;
    private bool _disposed;

    public NacosHttpClient(NacosClientOptions options, ILogger? logger = null)
    {
        _options = options;
        _logger = logger;
        _serverListManager = new ServerListManager(options);
        _securityProxy = new SecurityProxy(options, logger);

        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(_options.DefaultTimeout)
        };

        _httpClient.DefaultRequestHeaders.Add("Client-Version", "RedNb.Nacos/1.0.0");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "RedNb.Nacos.Client");
    }

    /// <summary>
    /// Sends a GET request.
    /// </summary>
    public async Task<string?> GetAsync(string path, Dictionary<string, string?>? parameters = null, 
        long timeout = 0, CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Get, path, parameters, null, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request.
    /// </summary>
    public async Task<string?> PostAsync(string path, Dictionary<string, string?>? parameters = null,
        string? body = null, long timeout = 0, CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Post, path, parameters, body, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a PUT request.
    /// </summary>
    public async Task<string?> PutAsync(string path, Dictionary<string, string?>? parameters = null,
        string? body = null, long timeout = 0, CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Put, path, parameters, body, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    public async Task<string?> DeleteAsync(string path, Dictionary<string, string?>? parameters = null,
        long timeout = 0, CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Delete, path, parameters, null, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends an HTTP request with automatic retry and server failover.
    /// </summary>
    private async Task<string?> RequestAsync(HttpMethod method, string path, 
        Dictionary<string, string?>? parameters, string? body, long timeout, 
        CancellationToken cancellationToken)
    {
        var servers = _serverListManager.GetServerList();
        if (servers.Count == 0)
        {
            throw new NacosException(NacosException.InvalidParam, "No available servers");
        }

        Exception? lastException = null;
        var maxRetry = Math.Max(1, servers.Count);

        for (var i = 0; i < maxRetry; i++)
        {
            var server = _serverListManager.GetNextServer();
            var baseUrl = _options.GetBaseUrl(server);
            
            try
            {
                var url = BuildUrl(baseUrl, path, parameters);
                _logger?.LogDebug("Sending {Method} request to {Url}", method, url);

                using var request = new HttpRequestMessage(method, url);
                
                // Add authentication headers
                await AddAuthHeadersAsync(request, cancellationToken);

                if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, NacosConstants.ContentTypeFormUrlEncoded);
                }

                using var cts = timeout > 0 
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) 
                    : null;
                
                if (cts != null)
                {
                    cts.CancelAfter(TimeSpan.FromMilliseconds(timeout));
                }

                var response = await _httpClient.SendAsync(request, cts?.Token ?? cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _serverListManager.MarkServerHealthy(server);
                    return content;
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new NacosException(NacosException.NoRight, $"Access denied: {content}");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new NacosException(NacosException.NotFound, $"Not found: {path}");
                }

                throw new NacosException((int)response.StatusCode, $"Request failed: {content}");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _serverListManager.MarkServerUnhealthy(server);
                lastException = new NacosException(NacosException.ServerError, "Request timeout", ex);
                _logger?.LogWarning(ex, "Request to {Server} timed out", server);
            }
            catch (HttpRequestException ex)
            {
                _serverListManager.MarkServerUnhealthy(server);
                lastException = new NacosException(NacosException.ServerError, ex.Message, ex);
                _logger?.LogWarning(ex, "Request to {Server} failed", server);
            }
            catch (NacosException ex) when (ex.ErrorCode is NacosException.NoRight or NacosException.NotFound or NacosException.InvalidParam)
            {
                throw; // Don't retry for these errors
            }
            catch (Exception ex)
            {
                _serverListManager.MarkServerUnhealthy(server);
                lastException = ex;
                _logger?.LogWarning(ex, "Request to {Server} failed with unexpected error", server);
            }
        }

        throw lastException ?? new NacosException(NacosException.ServerError, "All servers failed");
    }

    private static string BuildUrl(string baseUrl, string path, Dictionary<string, string?>? parameters)
    {
        var url = $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        
        if (parameters != null && parameters.Count > 0)
        {
            var queryString = NacosUtils.BuildQueryString(parameters);
            url = $"{url}?{queryString}";
        }

        return url;
    }

    private async Task AddAuthHeadersAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _securityProxy.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Add(NacosConstants.AccessToken, token);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _securityProxy.Dispose();
        _disposed = true;
    }
}
