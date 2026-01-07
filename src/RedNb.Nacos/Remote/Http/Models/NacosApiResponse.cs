namespace RedNb.Nacos.Remote.Http.Models;

/// <summary>
/// Nacos v3 API 通用响应包装
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class NacosApiResponse<T>
{
    /// <summary>
    /// 响应码（0 表示成功）
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// 响应数据
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Code == 0;
}

/// <summary>
/// Nacos v3 配置获取响应数据
/// </summary>
public class ConfigGetResponseData
{
    /// <summary>
    /// 配置ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// 数据ID
    /// </summary>
    [JsonPropertyName("dataId")]
    public string? DataId { get; set; }

    /// <summary>
    /// 分组
    /// </summary>
    [JsonPropertyName("group")]
    public string? Group { get; set; }

    /// <summary>
    /// 配置内容
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// MD5
    /// </summary>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 加密数据Key
    /// </summary>
    [JsonPropertyName("encryptedDataKey")]
    public string? EncryptedDataKey { get; set; }
}
