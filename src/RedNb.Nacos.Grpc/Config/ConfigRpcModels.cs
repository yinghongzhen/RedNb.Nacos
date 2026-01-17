using System.Text.Json.Serialization;

namespace RedNb.Nacos.GrpcClient.Config;

#region Base Request/Response

/// <summary>
/// Base class for all config RPC requests.
/// </summary>
public abstract class ConfigRpcRequest
{
    /// <summary>
    /// Request ID for tracking.
    /// </summary>
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Module name.
    /// </summary>
    [JsonPropertyName("module")]
    public virtual string Module => "config";
}

/// <summary>
/// Base class for all config RPC responses.
/// </summary>
public abstract class ConfigRpcResponse
{
    /// <summary>
    /// Request ID for correlation.
    /// </summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    /// <summary>
    /// Result code (200 for success).
    /// </summary>
    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    /// <summary>
    /// Error code when failed.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Success flag.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Error message when failed.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Checks if this is a successful response.
    /// </summary>
    public bool IsSuccess => Success || ResultCode == 200;
}

#endregion

#region Config Query

/// <summary>
/// Request to query configuration.
/// </summary>
public class ConfigQueryRequest : ConfigRpcRequest
{
    public const string TYPE = "ConfigQueryRequest";

    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }
}

/// <summary>
/// Response for configuration query.
/// </summary>
public class ConfigQueryResponse : ConfigRpcResponse
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("encryptedDataKey")]
    public string? EncryptedDataKey { get; set; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    [JsonPropertyName("lastModified")]
    public long LastModified { get; set; }

    [JsonPropertyName("isBeta")]
    public bool IsBeta { get; set; }

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }
}

#endregion

#region Config Publish

/// <summary>
/// Request to publish configuration.
/// </summary>
public class ConfigPublishRequest : ConfigRpcRequest
{
    public const string TYPE = "ConfigPublishRequest";

    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("casMd5")]
    public string? CasMd5 { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("encryptedDataKey")]
    public string? EncryptedDataKey { get; set; }

    /// <summary>
    /// Additional attributes.
    /// </summary>
    [JsonPropertyName("additionMap")]
    public Dictionary<string, string>? AdditionMap { get; set; }
}

/// <summary>
/// Response for configuration publish.
/// </summary>
public class ConfigPublishResponse : ConfigRpcResponse
{
}

#endregion

#region Config Remove

/// <summary>
/// Request to remove configuration.
/// </summary>
public class ConfigRemoveRequest : ConfigRpcRequest
{
    public const string TYPE = "ConfigRemoveRequest";

    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }
}

/// <summary>
/// Response for configuration remove.
/// </summary>
public class ConfigRemoveResponse : ConfigRpcResponse
{
}

#endregion

#region Config Listen

/// <summary>
/// Context for a single config listen.
/// </summary>
public class ConfigListenContext
{
    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }
}

/// <summary>
/// Request to batch listen to configurations.
/// </summary>
public class ConfigBatchListenRequest : ConfigRpcRequest
{
    public const string TYPE = "ConfigBatchListenRequest";

    /// <summary>
    /// Whether this is a listen (true) or unlisten (false) request.
    /// </summary>
    [JsonPropertyName("listen")]
    public bool Listen { get; set; } = true;

    /// <summary>
    /// List of configurations to listen/unlisten.
    /// </summary>
    [JsonPropertyName("configListenContexts")]
    public List<ConfigListenContext> ConfigListenContexts { get; set; } = new();
}

/// <summary>
/// Response for batch listen request.
/// </summary>
public class ConfigBatchListenResponse : ConfigRpcResponse
{
    /// <summary>
    /// List of changed configurations.
    /// </summary>
    [JsonPropertyName("changedConfigs")]
    public List<ConfigListenContext>? ChangedConfigs { get; set; }
}

#endregion

#region Config Change Notify

/// <summary>
/// Server push notification for configuration changes.
/// </summary>
public class ConfigChangeNotifyRequest : ConfigRpcRequest
{
    public const string TYPE = "ConfigChangeNotifyRequest";

    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    [JsonPropertyName("contentPush")]
    public bool ContentPush { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("encryptedDataKey")]
    public string? EncryptedDataKey { get; set; }

    [JsonPropertyName("lastModified")]
    public long LastModified { get; set; }
}

/// <summary>
/// Response for config change notify (sent back to server as ack).
/// </summary>
public class ConfigChangeNotifyResponse : ConfigRpcResponse
{
    public const string TYPE = "ConfigChangeNotifyResponse";
}

#endregion

#region Fuzzy Watch

/// <summary>
/// Context for fuzzy listen pattern.
/// </summary>
public class ConfigFuzzyListenContext
{
    [JsonPropertyName("dataIdPattern")]
    public string DataIdPattern { get; set; } = "*";

    [JsonPropertyName("groupPattern")]
    public string GroupPattern { get; set; } = "*";
}

/// <summary>
/// Request to fuzzy watch configurations.
/// </summary>
public class ConfigFuzzyWatchRequest : ConfigRpcRequest
{
    public const string TYPE = "ConfigFuzzyWatchRequest";

    /// <summary>
    /// Whether this is a watch (true) or unwatch (false) request.
    /// </summary>
    [JsonPropertyName("watch")]
    public bool Watch { get; set; } = true;

    /// <summary>
    /// Fuzzy listen contexts.
    /// </summary>
    [JsonPropertyName("contexts")]
    public List<ConfigFuzzyListenContext> Contexts { get; set; } = new();
}

/// <summary>
/// Response for fuzzy watch request.
/// </summary>
public class ConfigFuzzyWatchResponse : ConfigRpcResponse
{
    /// <summary>
    /// Matching configuration keys.
    /// </summary>
    [JsonPropertyName("matchedGroupKeys")]
    public List<string>? MatchedGroupKeys { get; set; }
}

/// <summary>
/// Server push notification for fuzzy watch changes.
/// </summary>
public class ConfigFuzzyWatchChangeNotifyRequest : ConfigRpcRequest
{
    public const string TYPE = "ConfigFuzzyWatchChangeNotifyRequest";

    [JsonPropertyName("dataId")]
    public string DataId { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// Change type: ADD_CONFIG, DELETE_CONFIG, MODIFY_CONFIG.
    /// </summary>
    [JsonPropertyName("changedType")]
    public string ChangedType { get; set; } = string.Empty;

    /// <summary>
    /// Sync type: FUZZY_WATCH_DIFF_SYNC_NOTIFY, FUZZY_WATCH_INIT_NOTIFY.
    /// </summary>
    [JsonPropertyName("syncType")]
    public string SyncType { get; set; } = string.Empty;
}

/// <summary>
/// Response for fuzzy watch change notify.
/// </summary>
public class ConfigFuzzyWatchChangeNotifyResponse : ConfigRpcResponse
{
    public const string TYPE = "ConfigFuzzyWatchChangeNotifyResponse";
}

#endregion

#region Connection

/// <summary>
/// Connection setup request.
/// </summary>
public class ConnectionSetupRequest
{
    public const string TYPE = "ConnectionSetupRequest";

    [JsonPropertyName("clientVersion")]
    public string ClientVersion { get; set; } = "RedNb.Nacos/2.0.0";

    [JsonPropertyName("abilities")]
    public ClientAbilities? Abilities { get; set; }

    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    [JsonPropertyName("labels")]
    public Dictionary<string, string> Labels { get; set; } = new();
}

/// <summary>
/// Client abilities declaration.
/// </summary>
public class ClientAbilities
{
    [JsonPropertyName("remoteAbility")]
    public RemoteAbility? RemoteAbility { get; set; }

    [JsonPropertyName("configAbility")]
    public ConfigAbility? ConfigAbility { get; set; }

    [JsonPropertyName("namingAbility")]
    public NamingAbility? NamingAbility { get; set; }
}

/// <summary>
/// Remote ability.
/// </summary>
public class RemoteAbility
{
    [JsonPropertyName("supportRemoteConnection")]
    public bool SupportRemoteConnection { get; set; } = true;
}

/// <summary>
/// Config ability.
/// </summary>
public class ConfigAbility
{
    [JsonPropertyName("supportRemoteMetrics")]
    public bool SupportRemoteMetrics { get; set; }
}

/// <summary>
/// Naming ability.
/// </summary>
public class NamingAbility
{
    [JsonPropertyName("supportDeltaPush")]
    public bool SupportDeltaPush { get; set; }

    [JsonPropertyName("supportRemoteMetric")]
    public bool SupportRemoteMetric { get; set; }
}

/// <summary>
/// Health check request.
/// </summary>
public class HealthCheckRequest : ConfigRpcRequest
{
    public const string TYPE = "HealthCheckRequest";
}

/// <summary>
/// Health check response.
/// </summary>
public class HealthCheckResponse : ConfigRpcResponse
{
}

/// <summary>
/// Client detection request from server.
/// </summary>
public class ClientDetectionRequest : ConfigRpcRequest
{
    public const string TYPE = "ClientDetectionRequest";
}

/// <summary>
/// Client detection response.
/// </summary>
public class ClientDetectionResponse : ConfigRpcResponse
{
    public const string TYPE = "ClientDetectionResponse";
}

/// <summary>
/// Server check request.
/// </summary>
public class ServerCheckRequest : ConfigRpcRequest
{
    public const string TYPE = "ServerCheckRequest";
}

/// <summary>
/// Server check response.
/// </summary>
public class ServerCheckResponse : ConfigRpcResponse
{
    [JsonPropertyName("connectionId")]
    public string? ConnectionId { get; set; }
}

#endregion

#region Constants

/// <summary>
/// Config changed type constants.
/// </summary>
public static class ConfigChangedType
{
    public const string AddConfig = "ADD_CONFIG";
    public const string DeleteConfig = "DELETE_CONFIG";
    public const string ModifyConfig = "MODIFY_CONFIG";
}

/// <summary>
/// Sync type constants.
/// </summary>
public static class ConfigSyncType
{
    public const string FuzzyWatchDiffSyncNotify = "FUZZY_WATCH_DIFF_SYNC_NOTIFY";
    public const string FuzzyWatchInitNotify = "FUZZY_WATCH_INIT_NOTIFY";
}

#endregion
