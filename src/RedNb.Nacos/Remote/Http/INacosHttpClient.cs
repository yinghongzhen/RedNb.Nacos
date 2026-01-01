namespace RedNb.Nacos.Remote.Http;

/// <summary>
/// Nacos HTTP 客户端接口
/// </summary>
public interface INacosHttpClient
{
    /// <summary>
    /// 发送 GET 请求
    /// </summary>
    Task<T?> GetAsync<T>(
        string path,
        Dictionary<string, string>? queryParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 发送 GET 请求，返回字符串
    /// </summary>
    Task<string?> GetStringAsync(
        string path,
        Dictionary<string, string>? queryParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送 POST 请求
    /// </summary>
    Task<T?> PostAsync<T>(
        string path,
        Dictionary<string, string>? formParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 发送 POST 请求，返回字符串
    /// </summary>
    Task<string?> PostStringAsync(
        string path,
        Dictionary<string, string>? formParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送 POST JSON 请求
    /// </summary>
    Task<TResponse?> PostJsonAsync<TRequest, TResponse>(
        string path,
        TRequest data,
        bool requireAuth = false,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// 发送 PUT 请求
    /// </summary>
    Task<T?> PutAsync<T>(
        string path,
        Dictionary<string, string>? formParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 发送 DELETE 请求
    /// </summary>
    Task<T?> DeleteAsync<T>(
        string path,
        Dictionary<string, string>? queryParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 发送 DELETE 请求，返回布尔值
    /// </summary>
    Task<bool> DeleteAsync(
        string path,
        Dictionary<string, string>? queryParams = null,
        bool requireAuth = false,
        CancellationToken cancellationToken = default);
}
