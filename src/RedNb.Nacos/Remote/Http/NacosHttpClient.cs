using RedNb.Nacos.Auth;

namespace RedNb.Nacos.Remote.Http;

/// <summary>
/// Nacos HTTP 客户端实现
/// </summary>
public sealed class NacosHttpClient : INacosHttpClient
{
    private readonly NacosOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthService _authService;
    private readonly ILogger<NacosHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public NacosHttpClient(
        IOptions<NacosOptions> options,
        IHttpClientFactory httpClientFactory,
        IAuthService authService,
        ILogger<NacosHttpClient> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _authService = authService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(
        string path,
        Dictionary<string, string>? queryParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default) where T : class
    {
        var response = await SendRequestAsync(
            HttpMethod.Get,
            path,
            queryParams,
            null,
            requireAuth,
            cancellationToken);

        return await DeserializeResponseAsync<T>(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetStringAsync(
        string path,
        Dictionary<string, string>? queryParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default)
    {
        var response = await SendRequestAsync(
            HttpMethod.Get,
            path,
            queryParams,
            null,
            requireAuth,
            cancellationToken);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> PostAsync<T>(
        string path,
        Dictionary<string, string>? formParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default) where T : class
    {
        var response = await SendRequestAsync(
            HttpMethod.Post,
            path,
            null,
            formParams,
            requireAuth,
            cancellationToken);

        return await DeserializeResponseAsync<T>(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> PostStringAsync(
        string path,
        Dictionary<string, string>? formParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default)
    {
        var response = await SendRequestAsync(
            HttpMethod.Post,
            path,
            null,
            formParams,
            requireAuth,
            cancellationToken);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(
        string path,
        TRequest data,
        bool requireAuth = false,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        var serverAddress = _options.GetRandomServerAddress();
        var url = BuildUrl(serverAddress, path, null);

        var client = _httpClientFactory.CreateClient("Nacos");
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(data, options: JsonOptions)
        };

        await AddAuthHeaderAsync(request, requireAuth, cancellationToken);

        var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> PutAsync<T>(
        string path,
        Dictionary<string, string>? formParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default) where T : class
    {
        var response = await SendRequestAsync(
            HttpMethod.Put,
            path,
            null,
            formParams,
            requireAuth,
            cancellationToken);

        return await DeserializeResponseAsync<T>(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> DeleteAsync<T>(
        string path,
        Dictionary<string, string>? queryParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default) where T : class
    {
        var response = await SendRequestAsync(
            HttpMethod.Delete,
            path,
            queryParams,
            null,
            requireAuth,
            cancellationToken);

        return await DeserializeResponseAsync<T>(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string path,
        Dictionary<string, string>? queryParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default)
    {
        var response = await SendRequestAsync(
            HttpMethod.Delete,
            path,
            queryParams,
            null,
            requireAuth,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return content.Equals("true", StringComparison.OrdinalIgnoreCase)
            || content.Equals("ok", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpMethod method,
        string path,
        Dictionary<string, string>? queryParams,
        Dictionary<string, string>? formParams,
        bool requireAuth,
        CancellationToken cancellationToken)
    {
        var serverAddress = _options.GetRandomServerAddress();
        var url = BuildUrl(serverAddress, path, queryParams);

        _logger.LogDebug("{Method} {Url}", method, url);

        var client = _httpClientFactory.CreateClient("Nacos");
        var request = new HttpRequestMessage(method, url);

        if (formParams != null && formParams.Count > 0)
        {
            request.Content = new FormUrlEncodedContent(formParams);
        }

        await AddAuthHeaderAsync(request, requireAuth, cancellationToken);

        var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        return response;
    }

    private async Task AddAuthHeaderAsync(
        HttpRequestMessage request,
        bool requireAuth,
        CancellationToken cancellationToken)
    {
        if (!requireAuth && !_authService.IsAuthEnabled)
        {
            return;
        }

        var token = await _authService.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.TryAddWithoutValidation("accessToken", token);
        }
    }

    private static string BuildUrl(
        string serverAddress,
        string path,
        Dictionary<string, string>? queryParams)
    {
        var url = $"{serverAddress}{path}";

        if (queryParams != null && queryParams.Count > 0)
        {
            var queryString = string.Join("&",
                queryParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            url = $"{url}?{queryString}";
        }

        return url;
    }

    private async Task EnsureSuccessStatusCodeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogError(
            "Nacos 请求失败，状态码: {StatusCode}，响应: {Content}",
            response.StatusCode,
            content);

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new NacosAuthException($"未授权: {content}"),
            HttpStatusCode.Forbidden => new NacosException(ErrorCodes.Forbidden, $"禁止访问: {content}"),
            HttpStatusCode.NotFound => new NacosException(ErrorCodes.NotFound, $"资源未找到: {content}"),
            HttpStatusCode.ServiceUnavailable => new NacosConnectionException($"服务不可用: {content}"),
            _ => new NacosException((int)response.StatusCode, $"请求失败: {content}")
        };
    }

    private static async Task<T?> DeserializeResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken) where T : class
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        // 尝试直接解析为目标类型
        try
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (JsonException)
        {
            // 如果失败，尝试解析为通用响应
            return null;
        }
    }
}
