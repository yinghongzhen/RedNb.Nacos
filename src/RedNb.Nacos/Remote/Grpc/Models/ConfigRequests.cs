namespace RedNb.Nacos.Remote.Grpc.Models;

/// <summary>
/// 配置查询请求
/// </summary>
public class ConfigQueryRequest : ConfigRequest
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
    /// 标签
    /// </summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    public override string GetRequestType() => "ConfigQueryRequest";
}

/// <summary>
/// 配置发布请求
/// </summary>
public class ConfigPublishRequest : ConfigRequest
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
    /// 配置内容
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 配置类型
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// CAS MD5值（用于乐观锁）
    /// </summary>
    [JsonPropertyName("casMd5")]
    public string? CasMd5 { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    [JsonPropertyName("appName")]
    public string? AppName { get; set; }

    /// <summary>
    /// 额外信息
    /// </summary>
    [JsonPropertyName("additionMap")]
    public Dictionary<string, string>? AdditionMap { get; set; }

    public override string GetRequestType() => "ConfigPublishRequest";
}

/// <summary>
/// 配置删除请求
/// </summary>
public class ConfigRemoveRequest : ConfigRequest
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

    public override string GetRequestType() => "ConfigRemoveRequest";
}

/// <summary>
/// 配置监听请求
/// </summary>
public class ConfigBatchListenRequest : ConfigRequest
{
    /// <summary>
    /// 监听配置列表
    /// </summary>
    [JsonPropertyName("configListenContexts")]
    public List<ConfigListenContext> ConfigListenContexts { get; set; } = new();

    /// <summary>
    /// 是否监听（false表示取消监听）
    /// </summary>
    [JsonPropertyName("listen")]
    public bool Listen { get; set; } = true;

    public override string GetRequestType() => "ConfigBatchListenRequest";
}

/// <summary>
/// 配置监听上下文
/// </summary>
public class ConfigListenContext
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

    /// <summary>
    /// MD5值
    /// </summary>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }
}

/// <summary>
/// 配置变更通知请求（服务端推送）
/// </summary>
public class ConfigChangeNotifyRequest : ConfigRequest
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

    public override string GetRequestType() => "ConfigChangeNotifyRequest";
}
