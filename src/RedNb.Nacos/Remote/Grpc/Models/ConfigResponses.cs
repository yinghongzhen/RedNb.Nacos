namespace RedNb.Nacos.Remote.Grpc.Models;

/// <summary>
/// 配置查询响应
/// </summary>
public class ConfigQueryResponse : NacosResponse
{
    /// <summary>
    /// 配置内容
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// MD5值
    /// </summary>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [JsonPropertyName("lastModified")]
    public long LastModified { get; set; }

    /// <summary>
    /// 是否存在
    /// </summary>
    [JsonPropertyName("isBeta")]
    public bool IsBeta { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    /// <summary>
    /// 加密数据密钥
    /// </summary>
    [JsonPropertyName("encryptedDataKey")]
    public string? EncryptedDataKey { get; set; }

    public override string GetResponseType() => "ConfigQueryResponse";
}

/// <summary>
/// 配置发布响应
/// </summary>
public class ConfigPublishResponse : NacosResponse
{
    public override string GetResponseType() => "ConfigPublishResponse";
}

/// <summary>
/// 配置删除响应
/// </summary>
public class ConfigRemoveResponse : NacosResponse
{
    public override string GetResponseType() => "ConfigRemoveResponse";
}

/// <summary>
/// 配置批量监听响应
/// </summary>
public class ConfigChangeBatchListenResponse : NacosResponse
{
    /// <summary>
    /// 变更的配置列表
    /// </summary>
    [JsonPropertyName("changedConfigs")]
    public List<ConfigContext>? ChangedConfigs { get; set; }

    public override string GetResponseType() => "ConfigChangeBatchListenResponse";
}

/// <summary>
/// 配置上下文
/// </summary>
public class ConfigContext
{
    /// <summary>
    /// 配置ID
    /// </summary>
    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// 分组名称
    /// </summary>
    [JsonPropertyName("group")]
    public string Group { get; set; } = NacosConstants.DefaultGroup;

    /// <summary>
    /// 命名空间
    /// </summary>
    [JsonPropertyName("tenant")]
    public string Tenant { get; set; } = string.Empty;
}
