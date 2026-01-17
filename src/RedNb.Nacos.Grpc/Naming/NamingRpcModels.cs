using System.Text.Json.Serialization;

namespace RedNb.Nacos.GrpcClient.Naming;

#region Base Request/Response

/// <summary>
/// Base class for all naming RPC requests.
/// </summary>
public abstract class NamingRpcRequest
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
    public virtual string Module => "naming";

    /// <summary>
    /// Namespace/Tenant.
    /// </summary>
    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }
}

/// <summary>
/// Base class for all naming RPC responses.
/// </summary>
public abstract class NamingRpcResponse
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

#region Instance Registration

/// <summary>
/// Request to register an instance.
/// </summary>
public class InstanceRequest : NamingRpcRequest
{
    public const string TYPE = "InstanceRequest";

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "registerInstance";

    [JsonPropertyName("instance")]
    public NamingInstance Instance { get; set; } = new();
}

/// <summary>
/// Response for instance registration.
/// </summary>
public class InstanceResponse : NamingRpcResponse
{
    public const string TYPE = "InstanceResponse";
}

/// <summary>
/// Request to batch register instances.
/// </summary>
public class BatchInstanceRequest : NamingRpcRequest
{
    public const string TYPE = "BatchInstanceRequest";

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "batchRegisterInstance";

    [JsonPropertyName("instances")]
    public List<NamingInstance> Instances { get; set; } = new();
}

/// <summary>
/// Response for batch instance registration.
/// </summary>
public class BatchInstanceResponse : NamingRpcResponse
{
    public const string TYPE = "BatchInstanceResponse";
}

#endregion

#region Service Query

/// <summary>
/// Request to query a service.
/// </summary>
public class ServiceQueryRequest : NamingRpcRequest
{
    public const string TYPE = "ServiceQueryRequest";

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("cluster")]
    public string? Cluster { get; set; }

    [JsonPropertyName("healthyOnly")]
    public bool HealthyOnly { get; set; }

    [JsonPropertyName("udpPort")]
    public int UdpPort { get; set; }
}

/// <summary>
/// Response for service query.
/// </summary>
public class ServiceQueryResponse : NamingRpcResponse
{
    public const string TYPE = "QueryServiceResponse";

    [JsonPropertyName("serviceInfo")]
    public NamingServiceInfo? ServiceInfo { get; set; }
}

#endregion

#region Service Subscription

/// <summary>
/// Request to subscribe to a service.
/// </summary>
public class SubscribeServiceRequest : NamingRpcRequest
{
    public const string TYPE = "SubscribeServiceRequest";

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("clusters")]
    public string? Clusters { get; set; }

    [JsonPropertyName("subscribe")]
    public bool Subscribe { get; set; } = true;
}

/// <summary>
/// Response for service subscription.
/// </summary>
public class SubscribeServiceResponse : NamingRpcResponse
{
    public const string TYPE = "SubscribeServiceResponse";

    [JsonPropertyName("serviceInfo")]
    public NamingServiceInfo? ServiceInfo { get; set; }
}

#endregion

#region Service List

/// <summary>
/// Request to list services.
/// </summary>
public class ServiceListRequest : NamingRpcRequest
{
    public const string TYPE = "ServiceListRequest";

    [JsonPropertyName("groupName")]
    public string? GroupName { get; set; }

    [JsonPropertyName("pageNo")]
    public int PageNo { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("selector")]
    public string? Selector { get; set; }
}

/// <summary>
/// Response for service list.
/// </summary>
public class ServiceListResponse : NamingRpcResponse
{
    public const string TYPE = "ServiceListResponse";

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("serviceNames")]
    public List<string>? ServiceNames { get; set; }
}

#endregion

#region Server Push Notifications

/// <summary>
/// Server push notification for service changes.
/// </summary>
public class NotifySubscriberRequest : NamingRpcRequest
{
    public const string TYPE = "NotifySubscriberRequest";

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("serviceInfo")]
    public NamingServiceInfo? ServiceInfo { get; set; }
}

/// <summary>
/// Response for notify subscriber (ack).
/// </summary>
public class NotifySubscriberResponse : NamingRpcResponse
{
    public const string TYPE = "NotifySubscriberResponse";
}

#endregion

#region Fuzzy Watch

/// <summary>
/// Request to fuzzy watch services.
/// </summary>
public class NamingFuzzyWatchRequest : NamingRpcRequest
{
    public const string TYPE = "NamingFuzzyWatchRequest";

    [JsonPropertyName("serviceNamePattern")]
    public string ServiceNamePattern { get; set; } = "*";

    [JsonPropertyName("groupNamePattern")]
    public string GroupNamePattern { get; set; } = "*";

    [JsonPropertyName("initializing")]
    public bool Initializing { get; set; } = true;

    [JsonPropertyName("receivedGroupKeys")]
    public HashSet<string> ReceivedGroupKeys { get; set; } = new();
}

/// <summary>
/// Response for fuzzy watch request.
/// </summary>
public class NamingFuzzyWatchResponse : NamingRpcResponse
{
    public const string TYPE = "NamingFuzzyWatchResponse";

    [JsonPropertyName("finishedPattern")]
    public string? FinishedPattern { get; set; }

    [JsonPropertyName("serviceChangedList")]
    public List<NamingFuzzyWatchChangeItem>? ServiceChangedList { get; set; }
}

/// <summary>
/// Item in fuzzy watch change list.
/// </summary>
public class NamingFuzzyWatchChangeItem
{
    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("changedType")]
    public string ChangedType { get; set; } = string.Empty;
}

/// <summary>
/// Request to cancel fuzzy watch.
/// </summary>
public class NamingFuzzyWatchCancelRequest : NamingRpcRequest
{
    public const string TYPE = "NamingFuzzyWatchCancelRequest";

    [JsonPropertyName("serviceNamePattern")]
    public string ServiceNamePattern { get; set; } = "*";

    [JsonPropertyName("groupNamePattern")]
    public string GroupNamePattern { get; set; } = "*";
}

/// <summary>
/// Response for cancel fuzzy watch.
/// </summary>
public class NamingFuzzyWatchCancelResponse : NamingRpcResponse
{
    public const string TYPE = "NamingFuzzyWatchCancelResponse";
}

/// <summary>
/// Server push notification for fuzzy watch changes.
/// </summary>
public class NamingFuzzyWatchNotifyRequest : NamingRpcRequest
{
    public const string TYPE = "NamingFuzzyWatchNotifyRequest";

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("changedType")]
    public string ChangedType { get; set; } = string.Empty;

    [JsonPropertyName("syncType")]
    public string SyncType { get; set; } = string.Empty;
}

/// <summary>
/// Response for fuzzy watch notify (ack).
/// </summary>
public class NamingFuzzyWatchNotifyResponse : NamingRpcResponse
{
    public const string TYPE = "NamingFuzzyWatchNotifyResponse";
}

#endregion

#region Persistent Instance (Non-ephemeral)

/// <summary>
/// Request for persistent instance operations.
/// </summary>
public class PersistentInstanceRequest : NamingRpcRequest
{
    public const string TYPE = "PersistentInstanceRequest";

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "registerInstance";

    [JsonPropertyName("instance")]
    public NamingInstance Instance { get; set; } = new();
}

#endregion

#region Data Models

/// <summary>
/// Instance information for gRPC communication.
/// </summary>
public class NamingInstance
{
    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("weight")]
    public double Weight { get; set; } = 1.0;

    [JsonPropertyName("healthy")]
    public bool Healthy { get; set; } = true;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("ephemeral")]
    public bool Ephemeral { get; set; } = true;

    [JsonPropertyName("clusterName")]
    public string? ClusterName { get; set; }

    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    [JsonPropertyName("instanceHeartBeatInterval")]
    public long InstanceHeartBeatInterval { get; set; }

    [JsonPropertyName("instanceHeartBeatTimeOut")]
    public long InstanceHeartBeatTimeOut { get; set; }

    [JsonPropertyName("ipDeleteTimeout")]
    public long IpDeleteTimeout { get; set; }

    [JsonPropertyName("instanceIdGenerator")]
    public string? InstanceIdGenerator { get; set; }
}

/// <summary>
/// Service information for gRPC communication.
/// </summary>
public class NamingServiceInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("groupName")]
    public string? GroupName { get; set; }

    [JsonPropertyName("clusters")]
    public string? Clusters { get; set; }

    [JsonPropertyName("cacheMillis")]
    public long CacheMillis { get; set; }

    [JsonPropertyName("lastRefTime")]
    public long LastRefTime { get; set; }

    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }

    [JsonPropertyName("allIPs")]
    public bool AllIPs { get; set; }

    [JsonPropertyName("reachProtectionThreshold")]
    public bool ReachProtectionThreshold { get; set; }

    [JsonPropertyName("hosts")]
    public List<NamingInstance>? Hosts { get; set; }

    /// <summary>
    /// Validates if this service info is valid.
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(Name) && Hosts != null;

    /// <summary>
    /// Gets the key for this service info.
    /// </summary>
    public string GetKey()
    {
        return GetKey(Name ?? "", GroupName ?? "", Clusters ?? "");
    }

    /// <summary>
    /// Gets a service key.
    /// </summary>
    public static string GetKey(string name, string groupName, string clusters)
    {
        if (!string.IsNullOrEmpty(clusters))
        {
            return $"{groupName}@@{name}@@{clusters}";
        }
        return $"{groupName}@@{name}";
    }
}

#endregion

#region Constants

/// <summary>
/// Instance operation type constants.
/// </summary>
public static class InstanceOperationType
{
    public const string RegisterInstance = "registerInstance";
    public const string DeregisterInstance = "deregisterInstance";
    public const string BatchRegisterInstance = "batchRegisterInstance";
}

/// <summary>
/// Naming changed type constants.
/// </summary>
public static class NamingChangedType
{
    public const string AddService = "ADD_SERVICE";
    public const string DeleteService = "DELETE_SERVICE";
}

/// <summary>
/// Naming sync type constants.
/// </summary>
public static class NamingSyncType
{
    public const string InitNotify = "FUZZY_WATCH_INIT_NOTIFY";
    public const string DiffSyncNotify = "FUZZY_WATCH_DIFF_SYNC_NOTIFY";
}

#endregion
