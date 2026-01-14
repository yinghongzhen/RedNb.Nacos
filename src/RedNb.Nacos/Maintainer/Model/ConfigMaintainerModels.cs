using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Maintainer;

#region Config Basic Models

/// <summary>
/// Configuration basic information.
/// </summary>
public class ConfigBasicInfo
{
    /// <summary>
    /// Gets or sets the configuration ID.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the data ID.
    /// </summary>
    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace ID (tenant).
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    [JsonPropertyName("appName")]
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the configuration type.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the MD5 hash of content.
    /// </summary>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    /// <summary>
    /// Gets or sets the encrypted data key.
    /// </summary>
    [JsonPropertyName("encryptedDataKey")]
    public string? EncryptedDataKey { get; set; }
}

/// <summary>
/// Configuration detail information.
/// </summary>
public class ConfigDetailInfo : ConfigBasicInfo
{
    /// <summary>
    /// Gets or sets the configuration content.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [JsonPropertyName("desc")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the configuration tags.
    /// </summary>
    [JsonPropertyName("configTags")]
    public string? ConfigTags { get; set; }

    /// <summary>
    /// Gets or sets the source user.
    /// </summary>
    [JsonPropertyName("srcUser")]
    public string? SrcUser { get; set; }

    /// <summary>
    /// Gets or sets the create time.
    /// </summary>
    [JsonPropertyName("createTime")]
    public long CreateTime { get; set; }

    /// <summary>
    /// Gets or sets the modify time.
    /// </summary>
    [JsonPropertyName("modifyTime")]
    public long ModifyTime { get; set; }

    /// <summary>
    /// Gets or sets the create IP.
    /// </summary>
    [JsonPropertyName("createIp")]
    public string? CreateIp { get; set; }

    /// <summary>
    /// Gets or sets the effect (gray/formal).
    /// </summary>
    [JsonPropertyName("effect")]
    public string? Effect { get; set; }

    /// <summary>
    /// Gets or sets the schema.
    /// </summary>
    [JsonPropertyName("schema")]
    public string? Schema { get; set; }
}

/// <summary>
/// Configuration clone information.
/// </summary>
public class ConfigCloneInfo
{
    /// <summary>
    /// Gets or sets the configuration ID to clone.
    /// </summary>
    [JsonPropertyName("cfgId")]
    public long ConfigId { get; set; }

    /// <summary>
    /// Gets or sets the target data ID.
    /// </summary>
    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target group.
    /// </summary>
    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;
}

/// <summary>
/// Same config policy for clone operation.
/// </summary>
public enum SameConfigPolicy
{
    /// <summary>
    /// Abort the operation if config exists.
    /// </summary>
    Abort,

    /// <summary>
    /// Skip the config if exists.
    /// </summary>
    Skip,

    /// <summary>
    /// Overwrite the config if exists.
    /// </summary>
    Overwrite
}

#endregion

#region Config Listener Models

/// <summary>
/// Configuration listener information.
/// </summary>
public class ConfigListenerInfo
{
    /// <summary>
    /// Gets or sets the list of listeners.
    /// </summary>
    [JsonPropertyName("lisentersGroupkeyStatus")]
    public Dictionary<string, string>? ListenersGroupKeyStatus { get; set; }

    /// <summary>
    /// Gets or sets the collect status from members.
    /// </summary>
    [JsonPropertyName("collectStatus")]
    public int CollectStatus { get; set; }
}

/// <summary>
/// Client subscription information.
/// </summary>
public class ClientSubscriptionInfo
{
    /// <summary>
    /// Gets or sets the client IP.
    /// </summary>
    [JsonPropertyName("ip")]
    public string? Ip { get; set; }

    /// <summary>
    /// Gets or sets the subscribed configs.
    /// </summary>
    [JsonPropertyName("subscribedConfigs")]
    public List<ConfigBasicInfo>? SubscribedConfigs { get; set; }
}

#endregion

#region Config History Models

/// <summary>
/// Configuration history basic information.
/// </summary>
public class ConfigHistoryBasicInfo
{
    /// <summary>
    /// Gets or sets the history ID.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the last ID.
    /// </summary>
    [JsonPropertyName("lastId")]
    public long LastId { get; set; }

    /// <summary>
    /// Gets or sets the data ID.
    /// </summary>
    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    [JsonPropertyName("appName")]
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the MD5 hash.
    /// </summary>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    /// <summary>
    /// Gets or sets the source IP.
    /// </summary>
    [JsonPropertyName("srcIp")]
    public string? SrcIp { get; set; }

    /// <summary>
    /// Gets or sets the source user.
    /// </summary>
    [JsonPropertyName("srcUser")]
    public string? SrcUser { get; set; }

    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    [JsonPropertyName("opType")]
    public string? OpType { get; set; }

    /// <summary>
    /// Gets or sets the publish type.
    /// </summary>
    [JsonPropertyName("publishType")]
    public string? PublishType { get; set; }

    /// <summary>
    /// Gets or sets the gray name.
    /// </summary>
    [JsonPropertyName("grayName")]
    public string? GrayName { get; set; }

    /// <summary>
    /// Gets or sets the external info.
    /// </summary>
    [JsonPropertyName("extInfo")]
    public string? ExtInfo { get; set; }

    /// <summary>
    /// Gets or sets the created time.
    /// </summary>
    [JsonPropertyName("createdTime")]
    public string? CreatedTime { get; set; }

    /// <summary>
    /// Gets or sets the last modified time.
    /// </summary>
    [JsonPropertyName("lastModifiedTime")]
    public string? LastModifiedTime { get; set; }
}

/// <summary>
/// Configuration history detail information.
/// </summary>
public class ConfigHistoryDetailInfo : ConfigHistoryBasicInfo
{
    /// <summary>
    /// Gets or sets the configuration content.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the encrypted data key.
    /// </summary>
    [JsonPropertyName("encryptedDataKey")]
    public string? EncryptedDataKey { get; set; }
}

#endregion

#region Beta Config Models

/// <summary>
/// Beta configuration information.
/// </summary>
public class BetaConfigInfo
{
    /// <summary>
    /// Gets or sets the data ID.
    /// </summary>
    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// Gets or sets the configuration content.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the beta IPs.
    /// </summary>
    [JsonPropertyName("betaIps")]
    public string? BetaIps { get; set; }

    /// <summary>
    /// Gets or sets the MD5 hash.
    /// </summary>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    [JsonPropertyName("appName")]
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the source user.
    /// </summary>
    [JsonPropertyName("srcUser")]
    public string? SrcUser { get; set; }

    /// <summary>
    /// Gets or sets the source IP.
    /// </summary>
    [JsonPropertyName("srcIp")]
    public string? SrcIp { get; set; }

    /// <summary>
    /// Gets or sets the create time.
    /// </summary>
    [JsonPropertyName("createTime")]
    public long CreateTime { get; set; }

    /// <summary>
    /// Gets or sets the modify time.
    /// </summary>
    [JsonPropertyName("modifyTime")]
    public long ModifyTime { get; set; }

    /// <summary>
    /// Gets or sets the encrypted data key.
    /// </summary>
    [JsonPropertyName("encryptedDataKey")]
    public string? EncryptedDataKey { get; set; }
}

/// <summary>
/// Gray configuration rule.
/// </summary>
public class GrayConfigRule
{
    /// <summary>
    /// Gets or sets the gray name.
    /// </summary>
    [JsonPropertyName("grayName")]
    public string GrayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the gray rule type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "ip";

    /// <summary>
    /// Gets or sets the gray rule expression.
    /// </summary>
    [JsonPropertyName("expr")]
    public string? Expression { get; set; }

    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

/// <summary>
/// Gray configuration information.
/// </summary>
public class GrayConfigInfo
{
    /// <summary>
    /// Gets or sets the data ID.
    /// </summary>
    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// Gets or sets the configuration content.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the gray name.
    /// </summary>
    [JsonPropertyName("grayName")]
    public string? GrayName { get; set; }

    /// <summary>
    /// Gets or sets the gray rule.
    /// </summary>
    [JsonPropertyName("grayRule")]
    public GrayConfigRule? GrayRule { get; set; }

    /// <summary>
    /// Gets or sets the MD5 hash.
    /// </summary>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    /// <summary>
    /// Gets or sets the source user.
    /// </summary>
    [JsonPropertyName("srcUser")]
    public string? SrcUser { get; set; }

    /// <summary>
    /// Gets or sets the create time.
    /// </summary>
    [JsonPropertyName("createTime")]
    public long CreateTime { get; set; }

    /// <summary>
    /// Gets or sets the modify time.
    /// </summary>
    [JsonPropertyName("modifyTime")]
    public long ModifyTime { get; set; }
}

#endregion

#region Config Import/Export Models

/// <summary>
/// Configuration import request.
/// </summary>
public class ConfigImportRequest
{
    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    public string NamespaceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy for same config.
    /// </summary>
    public SameConfigPolicy Policy { get; set; } = SameConfigPolicy.Abort;

    /// <summary>
    /// Gets or sets the file content (zip file bytes).
    /// </summary>
    public byte[] FileContent { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the source user.
    /// </summary>
    public string? SrcUser { get; set; }
}

/// <summary>
/// Configuration import result.
/// </summary>
public class ConfigImportResult
{
    /// <summary>
    /// Gets or sets whether the import was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the success count.
    /// </summary>
    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the skip count.
    /// </summary>
    [JsonPropertyName("skipCount")]
    public int SkipCount { get; set; }

    /// <summary>
    /// Gets or sets the failure count.
    /// </summary>
    [JsonPropertyName("failureCount")]
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the skip data.
    /// </summary>
    [JsonPropertyName("skipData")]
    public List<ConfigBasicInfo>? SkipData { get; set; }

    /// <summary>
    /// Gets or sets the failure data.
    /// </summary>
    [JsonPropertyName("failureData")]
    public List<ConfigBasicInfo>? FailureData { get; set; }

    /// <summary>
    /// Gets or sets the unrecognized data.
    /// </summary>
    [JsonPropertyName("unrecognizedData")]
    public List<string>? UnrecognizedData { get; set; }
}

/// <summary>
/// Configuration export request.
/// </summary>
public class ConfigExportRequest
{
    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    public string NamespaceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data ID pattern.
    /// </summary>
    public string? DataId { get; set; }

    /// <summary>
    /// Gets or sets the group pattern.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets the app name.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets specific config IDs to export.
    /// </summary>
    public List<long>? ConfigIds { get; set; }
}

/// <summary>
/// Clone result information.
/// </summary>
public class CloneResult
{
    /// <summary>
    /// Gets or sets the success count.
    /// </summary>
    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the skip count.
    /// </summary>
    [JsonPropertyName("skipCount")]
    public int SkipCount { get; set; }

    /// <summary>
    /// Gets or sets the failure count.
    /// </summary>
    [JsonPropertyName("failureCount")]
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the skip data.
    /// </summary>
    [JsonPropertyName("skipData")]
    public List<ConfigCloneInfo>? SkipData { get; set; }

    /// <summary>
    /// Gets or sets the failure data.
    /// </summary>
    [JsonPropertyName("failureData")]
    public List<ConfigCloneInfo>? FailureData { get; set; }
}

#endregion
