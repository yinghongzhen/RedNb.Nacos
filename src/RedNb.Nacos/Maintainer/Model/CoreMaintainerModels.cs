using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Maintainer;

#region Namespace Models

/// <summary>
/// Namespace information.
/// </summary>
public class NamespaceInfo
{
    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace show name.
    /// </summary>
    [JsonPropertyName("namespaceShowName")]
    public string? NamespaceShowName { get; set; }

    /// <summary>
    /// Gets or sets the namespace description.
    /// </summary>
    [JsonPropertyName("namespaceDesc")]
    public string? NamespaceDesc { get; set; }

    /// <summary>
    /// Gets or sets the quota.
    /// </summary>
    [JsonPropertyName("quota")]
    public int Quota { get; set; }

    /// <summary>
    /// Gets or sets the config count.
    /// </summary>
    [JsonPropertyName("configCount")]
    public int ConfigCount { get; set; }

    /// <summary>
    /// Gets or sets the namespace type (0: global, 1: default, 2: custom).
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }
}

/// <summary>
/// Namespace create/update request.
/// </summary>
public class NamespaceRequest
{
    /// <summary>
    /// Gets or sets the custom namespace ID.
    /// </summary>
    public string? CustomNamespaceId { get; set; }

    /// <summary>
    /// Gets or sets the namespace name.
    /// </summary>
    public string NamespaceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace description.
    /// </summary>
    public string? NamespaceDesc { get; set; }
}

#endregion

#region Cluster Member Models

/// <summary>
/// Cluster member information.
/// </summary>
public class ClusterMemberInfo
{
    /// <summary>
    /// Gets or sets the member IP.
    /// </summary>
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the member port.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the member state (UP, DOWN, SUSPICIOUS).
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the member address.
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the failure access count.
    /// </summary>
    [JsonPropertyName("failAccessCnt")]
    public int FailAccessCount { get; set; }

    /// <summary>
    /// Gets or sets the abilities.
    /// </summary>
    [JsonPropertyName("abilities")]
    public ClusterMemberAbilities? Abilities { get; set; }

    /// <summary>
    /// Gets or sets the extended info.
    /// </summary>
    [JsonPropertyName("extendInfo")]
    public Dictionary<string, object>? ExtendInfo { get; set; }
}

/// <summary>
/// Cluster member abilities.
/// </summary>
public class ClusterMemberAbilities
{
    /// <summary>
    /// Gets or sets the remote abilities.
    /// </summary>
    [JsonPropertyName("remoteAbility")]
    public RemoteAbilities? RemoteAbility { get; set; }
}

/// <summary>
/// Remote abilities configuration.
/// </summary>
public class RemoteAbilities
{
    /// <summary>
    /// Gets or sets whether support remote connection.
    /// </summary>
    [JsonPropertyName("supportRemoteConnection")]
    public bool SupportRemoteConnection { get; set; }

    /// <summary>
    /// Gets or sets whether support gRPC report.
    /// </summary>
    [JsonPropertyName("grpcReportEnabled")]
    public bool GrpcReportEnabled { get; set; }
}

#endregion

#region Client Connection Models

/// <summary>
/// Client connection information.
/// </summary>
public class ClientConnectionInfo
{
    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client IP.
    /// </summary>
    [JsonPropertyName("clientIp")]
    public string? ClientIp { get; set; }

    /// <summary>
    /// Gets or sets the client port.
    /// </summary>
    [JsonPropertyName("clientPort")]
    public int ClientPort { get; set; }

    /// <summary>
    /// Gets or sets the connect type.
    /// </summary>
    [JsonPropertyName("connectType")]
    public string? ConnectType { get; set; }

    /// <summary>
    /// Gets or sets the app name.
    /// </summary>
    [JsonPropertyName("appName")]
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the SDK version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the last active time.
    /// </summary>
    [JsonPropertyName("lastActiveTime")]
    public long LastActiveTime { get; set; }

    /// <summary>
    /// Gets or sets the create time.
    /// </summary>
    [JsonPropertyName("createTime")]
    public long CreateTime { get; set; }
}

/// <summary>
/// Client detail information.
/// </summary>
public class ClientDetailInfo : ClientConnectionInfo
{
    /// <summary>
    /// Gets or sets the published services.
    /// </summary>
    [JsonPropertyName("publishedServices")]
    public List<ClientPublishedService>? PublishedServices { get; set; }

    /// <summary>
    /// Gets or sets the subscribed services.
    /// </summary>
    [JsonPropertyName("subscribedServices")]
    public List<ClientSubscribedService>? SubscribedServices { get; set; }
}

/// <summary>
/// Client published service information.
/// </summary>
public class ClientPublishedService
{
    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the group.
    /// </summary>
    [JsonPropertyName("group")]
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the IP.
    /// </summary>
    [JsonPropertyName("ip")]
    public string? Ip { get; set; }

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the cluster.
    /// </summary>
    [JsonPropertyName("cluster")]
    public string? Cluster { get; set; }
}

/// <summary>
/// Client subscribed service information.
/// </summary>
public class ClientSubscribedService
{
    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the group.
    /// </summary>
    [JsonPropertyName("group")]
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the subscriber info.
    /// </summary>
    [JsonPropertyName("subscriberInfo")]
    public string? SubscriberInfo { get; set; }
}

#endregion

#region System Info Models

/// <summary>
/// Server state information.
/// </summary>
public class ServerStateInfo
{
    /// <summary>
    /// Gets or sets the server state.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }
}

/// <summary>
/// Server switch information.
/// </summary>
public class ServerSwitchInfo
{
    /// <summary>
    /// Gets or sets the switch name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the master or slave mode.
    /// </summary>
    [JsonPropertyName("masters")]
    public Dictionary<string, bool>? Masters { get; set; }

    /// <summary>
    /// Gets or sets the ad weight map.
    /// </summary>
    [JsonPropertyName("adWeightMap")]
    public Dictionary<string, object>? AdWeightMap { get; set; }

    /// <summary>
    /// Gets or sets the default push cache milliseconds.
    /// </summary>
    [JsonPropertyName("defaultPushCacheMillis")]
    public long DefaultPushCacheMillis { get; set; }

    /// <summary>
    /// Gets or sets the client beat interval.
    /// </summary>
    [JsonPropertyName("clientBeatInterval")]
    public long ClientBeatInterval { get; set; }

    /// <summary>
    /// Gets or sets the default cache milliseconds.
    /// </summary>
    [JsonPropertyName("defaultCacheMillis")]
    public long DefaultCacheMillis { get; set; }

    /// <summary>
    /// Gets or sets the distro threshold.
    /// </summary>
    [JsonPropertyName("distroThreshold")]
    public float DistroThreshold { get; set; }

    /// <summary>
    /// Gets or sets whether health check is enabled.
    /// </summary>
    [JsonPropertyName("healthCheckEnabled")]
    public bool HealthCheckEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether auto change health check is enabled.
    /// </summary>
    [JsonPropertyName("autoChangeHealthCheckEnabled")]
    public bool AutoChangeHealthCheckEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether distro is enabled.
    /// </summary>
    [JsonPropertyName("distroEnabled")]
    public bool DistroEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether push is enabled.
    /// </summary>
    [JsonPropertyName("enableStandalone")]
    public bool EnableStandalone { get; set; }

    /// <summary>
    /// Gets or sets whether push is enabled.
    /// </summary>
    [JsonPropertyName("pushEnabled")]
    public bool PushEnabled { get; set; }

    /// <summary>
    /// Gets or sets the check times.
    /// </summary>
    [JsonPropertyName("checkTimes")]
    public int CheckTimes { get; set; }

    /// <summary>
    /// Gets or sets the http health params.
    /// </summary>
    [JsonPropertyName("httpHealthParams")]
    public HealthParams? HttpHealthParams { get; set; }

    /// <summary>
    /// Gets or sets the tcp health params.
    /// </summary>
    [JsonPropertyName("tcpHealthParams")]
    public HealthParams? TcpHealthParams { get; set; }

    /// <summary>
    /// Gets or sets the mysql health params.
    /// </summary>
    [JsonPropertyName("mysqlHealthParams")]
    public HealthParams? MysqlHealthParams { get; set; }

    /// <summary>
    /// Gets or sets the incremental list.
    /// </summary>
    [JsonPropertyName("incrementalList")]
    public List<string>? IncrementalList { get; set; }

    /// <summary>
    /// Gets or sets the server status synchronization period millis.
    /// </summary>
    [JsonPropertyName("serverStatusSynchronizationPeriodMillis")]
    public long ServerStatusSynchronizationPeriodMillis { get; set; }

    /// <summary>
    /// Gets or sets the service status synchronization period millis.
    /// </summary>
    [JsonPropertyName("serviceStatusSynchronizationPeriodMillis")]
    public long ServiceStatusSynchronizationPeriodMillis { get; set; }

    /// <summary>
    /// Gets or sets whether all DOM names have been read.
    /// </summary>
    [JsonPropertyName("disableAddIP")]
    public bool DisableAddIp { get; set; }

    /// <summary>
    /// Gets or sets whether to send beat only.
    /// </summary>
    [JsonPropertyName("sendBeatOnly")]
    public bool SendBeatOnly { get; set; }

    /// <summary>
    /// Gets or sets the limited URL map.
    /// </summary>
    [JsonPropertyName("limitedUrlMap")]
    public Dictionary<string, int>? LimitedUrlMap { get; set; }

    /// <summary>
    /// Gets or sets the distro server expire millis.
    /// </summary>
    [JsonPropertyName("distroServerExpiredMillis")]
    public long DistroServerExpiredMillis { get; set; }

    /// <summary>
    /// Gets or sets the push go version.
    /// </summary>
    [JsonPropertyName("pushGoVersion")]
    public string? PushGoVersion { get; set; }

    /// <summary>
    /// Gets or sets the push java version.
    /// </summary>
    [JsonPropertyName("pushJavaVersion")]
    public string? PushJavaVersion { get; set; }

    /// <summary>
    /// Gets or sets the push python version.
    /// </summary>
    [JsonPropertyName("pushPythonVersion")]
    public string? PushPythonVersion { get; set; }

    /// <summary>
    /// Gets or sets the push c version.
    /// </summary>
    [JsonPropertyName("pushCVersion")]
    public string? PushCVersion { get; set; }

    /// <summary>
    /// Gets or sets whether empty service is enabled.
    /// </summary>
    [JsonPropertyName("enableAuthentication")]
    public bool EnableAuthentication { get; set; }

    /// <summary>
    /// Gets or sets the overridden server status.
    /// </summary>
    [JsonPropertyName("overriddenServerStatus")]
    public string? OverriddenServerStatus { get; set; }

    /// <summary>
    /// Gets or sets whether default instance is ephemeral.
    /// </summary>
    [JsonPropertyName("defaultInstanceEphemeral")]
    public bool DefaultInstanceEphemeral { get; set; }

    /// <summary>
    /// Gets or sets the health check white list.
    /// </summary>
    [JsonPropertyName("healthCheckWhiteList")]
    public List<string>? HealthCheckWhiteList { get; set; }

    /// <summary>
    /// Gets or sets the checksum.
    /// </summary>
    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }
}

/// <summary>
/// Health parameters.
/// </summary>
public class HealthParams
{
    /// <summary>
    /// Gets or sets the max.
    /// </summary>
    [JsonPropertyName("max")]
    public int Max { get; set; }

    /// <summary>
    /// Gets or sets the min.
    /// </summary>
    [JsonPropertyName("min")]
    public int Min { get; set; }

    /// <summary>
    /// Gets or sets the factor.
    /// </summary>
    [JsonPropertyName("factor")]
    public float Factor { get; set; }
}

#endregion

#region Connection Models

/// <summary>
/// Connection information.
/// </summary>
public class ConnectionInfo
{
    /// <summary>
    /// Gets or sets the connection ID.
    /// </summary>
    [JsonPropertyName("connectionId")]
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client IP.
    /// </summary>
    [JsonPropertyName("clientIp")]
    public string? ClientIp { get; set; }

    /// <summary>
    /// Gets or sets the client port.
    /// </summary>
    [JsonPropertyName("clientPort")]
    public int ClientPort { get; set; }

    /// <summary>
    /// Gets or sets the connect type.
    /// </summary>
    [JsonPropertyName("connectType")]
    public string? ConnectType { get; set; }

    /// <summary>
    /// Gets or sets the meta info.
    /// </summary>
    [JsonPropertyName("metaInfo")]
    public ConnectionMetaInfo? MetaInfo { get; set; }
}

/// <summary>
/// Connection meta information.
/// </summary>
public class ConnectionMetaInfo
{
    /// <summary>
    /// Gets or sets the client IP.
    /// </summary>
    [JsonPropertyName("clientIp")]
    public string? ClientIp { get; set; }

    /// <summary>
    /// Gets or sets the client version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the connection ID.
    /// </summary>
    [JsonPropertyName("connectionId")]
    public string? ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets the create time.
    /// </summary>
    [JsonPropertyName("createTime")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// Gets or sets the last active time.
    /// </summary>
    [JsonPropertyName("lastActiveTime")]
    public long LastActiveTime { get; set; }

    /// <summary>
    /// Gets or sets the app name.
    /// </summary>
    [JsonPropertyName("appName")]
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the tenant.
    /// </summary>
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }

    /// <summary>
    /// Gets or sets the labels.
    /// </summary>
    [JsonPropertyName("labels")]
    public Dictionary<string, string>? Labels { get; set; }
}

/// <summary>
/// Connection list result.
/// </summary>
public class ConnectionListResult
{
    /// <summary>
    /// Gets or sets the connection list.
    /// </summary>
    [JsonPropertyName("connectionList")]
    public List<ConnectionInfo>? ConnectionList { get; set; }

    /// <summary>
    /// Gets or sets the total count.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

#endregion
