using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos;
using RedNb.Nacos.Utils;

namespace RedNb.Nacos.Http.Transport;

/// <summary>
/// Shared implementation of the Nacos server HTTP transport. The concrete
/// clients differ only in how a base URL is derived from a server address
/// (<see cref="BuildBaseUrl"/> — the console listener has no /nacos context
/// path), in construction-time server resolution, and in the console client's
/// fail-fast check.
/// </summary>
/// <remarks>
/// <para>
/// Behavior is otherwise identical between the concrete clients: same retry
/// loop, same body/header handling, same multipart support, same
/// <see cref="SecurityProxy"/>-issued access token. The intentional
/// differences are:
/// </para>
/// <list type="bullet">
///   <item>The server list is sourced either from
///   <see cref="NacosClientOptions.ServerAddresses"/> (via
///   <see cref="ServerListManager(NacosClientOptions)"/>) or from a
///   pre-resolved address list supplied by the subclass (via
///   <see cref="ServerListManager(IList{string})"/>).</item>
///   <item>Per-request URLs are derived through <see cref="BuildBaseUrl"/>:
///   <see cref="NacosClientOptions.GetBaseUrl"/> appends the
///   <c>ContextPath</c>, while <see cref="NacosClientOptions.GetConsoleBaseUrl"/>
///   omits it.</item>
/// </list>
/// </remarks>
public abstract class NacosHttpClientBase : IDisposable
{
    protected readonly HttpClient _httpClient;
    protected readonly NacosClientOptions _options;
    protected readonly ILogger? _logger;
    protected readonly ServerListManager _serverListManager;
    protected readonly SecurityProxy _securityProxy;
    protected bool _disposed;

    protected NacosHttpClientBase(NacosClientOptions options, ILogger? logger = null, ServerListManager? serverListManager = null)
    {
        _options = options;
        _logger = logger;
        _serverListManager = serverListManager ?? new ServerListManager(options);

        var handler = NacosTransport.CreateHandler(options);

        _httpClient = new HttpClient(handler)
        {
            // Set timeout to infinite - we'll control timeout per-request with CancellationToken
            Timeout = Timeout.InfiniteTimeSpan
        };

        try { _securityProxy = new SecurityProxy(options, logger); }
        catch { _httpClient.Dispose(); throw; }

        _httpClient.DefaultRequestHeaders.Add("Client-Version", NacosConstants.ClientVersion);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "RedNb.Nacos.Http");
    }

    /// <summary>
    /// Derives the base URL for a server address. <see cref="NacosConsoleHttpClient"/>
    /// overrides this to drop the /nacos context path.
    /// </summary>
    protected virtual string BuildBaseUrl(string server) => _options.GetBaseUrl(server);

    /// <summary>
    /// Sends a GET request.
    /// </summary>
    public async Task<string?> GetAsync(string path, Dictionary<string, string?>? parameters = null,
        long timeout = 0, CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Get, path, parameters, null, null, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a GET request with custom headers.
    /// </summary>
    public async Task<string?> GetWithHeadersAsync(string path, Dictionary<string, string?>? parameters = null,
        Dictionary<string, string>? headers = null, long timeout = 0,
        CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Get, path, parameters, null, headers, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request.
    /// </summary>
    public async Task<string?> PostAsync(string path, Dictionary<string, string?>? parameters = null,
        string? body = null, long timeout = 0, CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Post, path, parameters, body, null, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with custom headers.
    /// </summary>
    public async Task<string?> PostWithHeadersAsync(string path, Dictionary<string, string?>? parameters = null,
        string? body = null, Dictionary<string, string>? headers = null, long timeout = 0,
        CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Post, path, parameters, body, headers, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a PUT request.
    /// </summary>
    public async Task<string?> PutAsync(string path, Dictionary<string, string?>? parameters = null,
        string? body = null, long timeout = 0, CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Put, path, parameters, body, null, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a PUT request with custom headers.
    /// </summary>
    public async Task<string?> PutWithHeadersAsync(string path, Dictionary<string, string?>? parameters = null,
        string? body = null, Dictionary<string, string>? headers = null, long timeout = 0,
        CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Put, path, parameters, body, headers, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    public async Task<string?> DeleteAsync(string path, Dictionary<string, string?>? parameters = null,
        long timeout = 0, CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Delete, path, parameters, null, null, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request with custom headers.
    /// </summary>
    public async Task<string?> DeleteWithHeadersAsync(string path, Dictionary<string, string?>? parameters = null,
        Dictionary<string, string>? headers = null, long timeout = 0,
        CancellationToken cancellationToken = default)
    {
        return await RequestAsync(HttpMethod.Delete, path, parameters, null, headers, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a GET request and returns the raw response, including status code,
    /// response headers and binary body. A 304 (Not Modified) status is returned
    /// normally instead of throwing.
    /// </summary>
    public async Task<NacosRawResponse> GetRawAsync(string path, Dictionary<string, string?>? parameters = null,
        Dictionary<string, string>? headers = null, long timeout = 0, CancellationToken cancellationToken = default)
    {
        return await RequestRawAsync(HttpMethod.Get, path, parameters, null, headers, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with multipart/form-data content.
    /// </summary>
    public async Task<string?> PostMultipartAsync(string path, MultipartFormDataContent content,
        Dictionary<string, string?>? parameters = null, long timeout = 0, CancellationToken cancellationToken = default)
    {
        var raw = await RequestRawAsync(HttpMethod.Post, path, parameters, content, null, timeout, cancellationToken);
        return raw.BodyString;
    }

    /// <summary>
    /// Sends a POST request with multipart/form-data content and custom headers.
    /// Required for v3 multipart uploads that need to transmit <c>namespaceId</c> as
    /// the <c>X-Nacos-Namespace-Id</c> HTTP header instead of a form part.
    /// </summary>
    public async Task<string?> PostMultipartWithHeadersAsync(string path, MultipartFormDataContent content,
        Dictionary<string, string?>? parameters = null, Dictionary<string, string>? headers = null,
        long timeout = 0, CancellationToken cancellationToken = default)
    {
        var raw = await RequestRawAsync(HttpMethod.Post, path, parameters, content, headers, timeout, cancellationToken);
        return raw.BodyString;
    }

    /// <summary>
    /// Sends an HTTP request with automatic retry and server failover.
    /// </summary>
    private async Task<string?> RequestAsync(HttpMethod method, string path,
        Dictionary<string, string?>? parameters, string? body, Dictionary<string, string>? headers, long timeout,
        CancellationToken cancellationToken)
    {
        var servers = _serverListManager.GetServerList();
        if (servers.Count == 0)
        {
            throw new NacosException(NacosException.InvalidParam, "No available servers");
        }

        // Use default timeout if not specified
        var effectiveTimeout = timeout > 0 ? timeout : _options.DefaultTimeout;
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(effectiveTimeout));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        Exception? lastException = null;
        var maxRetry = Math.Max(1, servers.Count);

        for (var i = 0; i < maxRetry; i++)
        {
            var server = _serverListManager.GetNextServer();
            var baseUrl = BuildBaseUrl(server);


            try
            {
                var url = BuildUrl(baseUrl, path, parameters);
                _logger?.LogDebug("Sending {Method} request to {Url} with timeout {Timeout}ms", method, url, effectiveTimeout);

                using var request = new HttpRequestMessage(method, url);

                // Add authentication headers
                await AddAuthHeadersAsync(request, linkedCts.Token);

                if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, NacosConstants.ContentTypeFormUrlEncoded);
                }

                // Add custom headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                using var response = await _httpClient.SendAsync(request, linkedCts.Token);
                var content = await response.Content.ReadAsStringAsync(linkedCts.Token);

                if (response.IsSuccessStatusCode)
                {
                    if (path.Contains("/ai/", StringComparison.Ordinal)) NacosEnvelope.ThrowIfFailed(content, path);
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
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                // User requested cancellation - propagate immediately without retry
                throw;
            }
            catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
            {
                // The overall request budget has expired; do not restart it on another server.
                _serverListManager.MarkServerUnhealthy(server);
                lastException = new NacosException(NacosException.ServerError, "Request timeout", ex);
                _logger?.LogWarning("Request to {Server} timed out after {Timeout}ms", server, effectiveTimeout);
                throw lastException;
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

    /// <summary>
    /// Sends an HTTP request and returns the raw response with automatic retry and server failover.
    /// Unlike <see cref="RequestAsync"/>, a 304 (Not Modified) status is returned normally,
    /// and the response body is exposed as raw bytes together with the response headers.
    /// </summary>
    private async Task<NacosRawResponse> RequestRawAsync(HttpMethod method, string path,
        Dictionary<string, string?>? parameters, HttpContent? content, Dictionary<string, string>? headers,
        long timeout, CancellationToken cancellationToken)
    {
        var servers = _serverListManager.GetServerList();
        if (servers.Count == 0)
        {
            throw new NacosException(NacosException.InvalidParam, "No available servers");
        }

        var effectiveTimeout = timeout > 0 ? timeout : _options.DefaultTimeout;
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(effectiveTimeout));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        Exception? lastException = null;
        var maxRetry = Math.Max(1, servers.Count);

        // Buffer the content once so it can be reused across retries
        byte[]? contentBytes = null;
        string? contentType = null;
        if (content != null)
        {
            try { contentBytes = await content.ReadAsByteArrayAsync(linkedCts.Token); }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
            { throw new NacosException(NacosException.ServerError, "Request timeout", ex); }
            contentType = content.Headers.ContentType?.ToString();
        }

        for (var i = 0; i < maxRetry; i++)
        {
            var server = _serverListManager.GetNextServer();
            var baseUrl = BuildBaseUrl(server);


            try
            {
                var url = BuildUrl(baseUrl, path, parameters);
                _logger?.LogDebug("Sending raw {Method} request to {Url} with timeout {Timeout}ms", method, url, effectiveTimeout);

                using var request = new HttpRequestMessage(method, url);

                await AddAuthHeadersAsync(request, linkedCts.Token);

                if (contentBytes != null && (method == HttpMethod.Post || method == HttpMethod.Put))
                {
                    request.Content = new ByteArrayContent(contentBytes);
                    if (!string.IsNullOrEmpty(contentType))
                    {
                        request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
                    }
                }

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                using var response = await _httpClient.SendAsync(request, linkedCts.Token);
                var body = await response.Content.ReadAsByteArrayAsync(linkedCts.Token);

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotModified)
                {
                    if (path.Contains("/ai/", StringComparison.Ordinal) &&
                        response.Content.Headers.ContentType?.MediaType == "application/json")
                        NacosEnvelope.ThrowIfFailed(Encoding.UTF8.GetString(body), path);
                    _serverListManager.MarkServerHealthy(server);

                    var responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var header in response.Headers)
                    {
                        responseHeaders[header.Key] = string.Join(",", header.Value);
                    }
                    foreach (var header in response.Content.Headers)
                    {
                        responseHeaders[header.Key] = string.Join(",", header.Value);
                    }

                    return new NacosRawResponse((int)response.StatusCode, responseHeaders, body);
                }

                var errorContent = Encoding.UTF8.GetString(body);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new NacosException(NacosException.NoRight, $"Access denied: {errorContent}");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new NacosException(NacosException.NotFound, $"Not found: {path}");
                }

                throw new NacosException((int)response.StatusCode, $"Request failed: {errorContent}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _serverListManager.MarkServerUnhealthy(server);
                lastException = new NacosException(NacosException.ServerError, "Request timeout");
                _logger?.LogWarning("Request to {Server} timed out after {Timeout}ms", server, effectiveTimeout);
                throw lastException;
            }
            catch (HttpRequestException ex)
            {
                _serverListManager.MarkServerUnhealthy(server);
                lastException = new NacosException(NacosException.ServerError, ex.Message, ex);
                _logger?.LogWarning(ex, "Request to {Server} failed", server);
            }
            catch (NacosException ex) when (ex.ErrorCode is NacosException.NoRight or NacosException.NotFound or NacosException.InvalidParam)
            {
                throw;
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
