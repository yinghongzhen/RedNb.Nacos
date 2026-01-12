using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Maintainer;

/// <summary>
/// Service definition for creating/updating services.
/// </summary>
public class ServiceDefinition
{
    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    [JsonPropertyName("namespaceId")]
    public string? NamespaceId { get; set; }

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = "DEFAULT_GROUP";

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the service is ephemeral.
    /// </summary>
    [JsonPropertyName("ephemeral")]
    public bool Ephemeral { get; set; } = true;

    /// <summary>
    /// Gets or sets the protect threshold (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("protectThreshold")]
    public float ProtectThreshold { get; set; }

    /// <summary>
    /// Gets or sets the service metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the selector type.
    /// </summary>
    [JsonPropertyName("selector")]
    public SelectorDefinition? Selector { get; set; }
}

/// <summary>
/// Selector definition for service.
/// </summary>
public class SelectorDefinition
{
    /// <summary>
    /// Gets or sets the selector type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "none";

    /// <summary>
    /// Gets or sets the selector expression.
    /// </summary>
    [JsonPropertyName("expression")]
    public string? Expression { get; set; }

    /// <summary>
    /// Gets or sets the context type for label selector.
    /// </summary>
    [JsonPropertyName("contextType")]
    public string? ContextType { get; set; }

    /// <summary>
    /// Gets or sets the labels for label selector.
    /// </summary>
    [JsonPropertyName("labels")]
    public Dictionary<string, string>? Labels { get; set; }
}

/// <summary>
/// Service detail information.
/// </summary>
public class ServiceDetailInfo
{
    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    [JsonPropertyName("namespaceId")]
    public string? NamespaceId { get; set; }

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    [JsonPropertyName("groupName")]
    public string? GroupName { get; set; }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether the service is ephemeral.
    /// </summary>
    [JsonPropertyName("ephemeral")]
    public bool Ephemeral { get; set; }

    /// <summary>
    /// Gets or sets the protect threshold.
    /// </summary>
    [JsonPropertyName("protectThreshold")]
    public float ProtectThreshold { get; set; }

    /// <summary>
    /// Gets or sets the service metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the selector.
    /// </summary>
    [JsonPropertyName("selector")]
    public SelectorDefinition? Selector { get; set; }

    /// <summary>
    /// Gets or sets the cluster map.
    /// </summary>
    [JsonPropertyName("clusterMap")]
    public Dictionary<string, ClusterInfo>? ClusterMap { get; set; }

    /// <summary>
    /// Gets or sets the total instance count.
    /// </summary>
    [JsonPropertyName("ipCount")]
    public int IpCount { get; set; }

    /// <summary>
    /// Gets or sets the healthy instance count.
    /// </summary>
    [JsonPropertyName("healthyInstanceCount")]
    public int HealthyInstanceCount { get; set; }

    /// <summary>
    /// Gets or sets whether trigger protection threshold.
    /// </summary>
    [JsonPropertyName("triggerFlag")]
    public bool TriggerFlag { get; set; }
}

/// <summary>
/// Service view for listing.
/// </summary>
public class ServiceView
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    [JsonPropertyName("groupName")]
    public string? GroupName { get; set; }

    /// <summary>
    /// Gets or sets the cluster count.
    /// </summary>
    [JsonPropertyName("clusterCount")]
    public int ClusterCount { get; set; }

    /// <summary>
    /// Gets or sets the instance count.
    /// </summary>
    [JsonPropertyName("ipCount")]
    public int IpCount { get; set; }

    /// <summary>
    /// Gets or sets the healthy instance count.
    /// </summary>
    [JsonPropertyName("healthyInstanceCount")]
    public int HealthyInstanceCount { get; set; }

    /// <summary>
    /// Gets or sets whether trigger protection threshold.
    /// </summary>
    [JsonPropertyName("triggerFlag")]
    public bool TriggerFlag { get; set; }
}

/// <summary>
/// Subscriber information.
/// </summary>
public class SubscriberInfo
{
    /// <summary>
    /// Gets or sets the subscriber address.
    /// </summary>
    [JsonPropertyName("addrStr")]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the subscriber agent.
    /// </summary>
    [JsonPropertyName("agent")]
    public string? Agent { get; set; }

    /// <summary>
    /// Gets or sets the app name.
    /// </summary>
    [JsonPropertyName("app")]
    public string? App { get; set; }

    /// <summary>
    /// Gets or sets the IP address.
    /// </summary>
    [JsonPropertyName("ip")]
    public string? Ip { get; set; }

    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    [JsonPropertyName("namespaceId")]
    public string? NamespaceId { get; set; }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }
}

/// <summary>
/// Cluster information.
/// </summary>
public class ClusterInfo
{
    /// <summary>
    /// Gets or sets the cluster name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "DEFAULT";

    /// <summary>
    /// Gets or sets the health checker.
    /// </summary>
    [JsonPropertyName("healthChecker")]
    public HealthCheckerInfo? HealthChecker { get; set; }

    /// <summary>
    /// Gets or sets the cluster metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the default check port.
    /// </summary>
    [JsonPropertyName("defaultCheckPort")]
    public int DefaultCheckPort { get; set; } = 80;

    /// <summary>
    /// Gets or sets whether to use instance port for health check.
    /// </summary>
    [JsonPropertyName("useInstancePortForCheck")]
    public bool UseInstancePortForCheck { get; set; } = true;
}

/// <summary>
/// Health checker information.
/// </summary>
public class HealthCheckerInfo
{
    /// <summary>
    /// Gets or sets the health checker type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "TCP";

    /// <summary>
    /// Gets or sets the HTTP path for HTTP health check.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the HTTP headers for HTTP health check.
    /// </summary>
    [JsonPropertyName("headers")]
    public string? Headers { get; set; }

    /// <summary>
    /// Gets or sets the expected response codes.
    /// </summary>
    [JsonPropertyName("expectedResponseCode")]
    public int ExpectedResponseCode { get; set; } = 200;
}

/// <summary>
/// System metrics information.
/// </summary>
public class MetricsInfo
{
    /// <summary>
    /// Gets or sets the server status.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the service count.
    /// </summary>
    [JsonPropertyName("serviceCount")]
    public int ServiceCount { get; set; }

    /// <summary>
    /// Gets or sets the instance count.
    /// </summary>
    [JsonPropertyName("instanceCount")]
    public int InstanceCount { get; set; }

    /// <summary>
    /// Gets or sets the subscriber count.
    /// </summary>
    [JsonPropertyName("subscribeCount")]
    public int SubscribeCount { get; set; }

    /// <summary>
    /// Gets or sets the Raft notify task count.
    /// </summary>
    [JsonPropertyName("raftNotifyTaskCount")]
    public int RaftNotifyTaskCount { get; set; }

    /// <summary>
    /// Gets or sets the responsible service count.
    /// </summary>
    [JsonPropertyName("responsibleServiceCount")]
    public int ResponsibleServiceCount { get; set; }

    /// <summary>
    /// Gets or sets the responsible instance count.
    /// </summary>
    [JsonPropertyName("responsibleInstanceCount")]
    public int ResponsibleInstanceCount { get; set; }

    /// <summary>
    /// Gets or sets the client count.
    /// </summary>
    [JsonPropertyName("clientCount")]
    public int ClientCount { get; set; }

    /// <summary>
    /// Gets or sets the connection-based client count.
    /// </summary>
    [JsonPropertyName("connectionBasedClientCount")]
    public int ConnectionBasedClientCount { get; set; }

    /// <summary>
    /// Gets or sets the ephemeral IP count.
    /// </summary>
    [JsonPropertyName("ephemeralIpCount")]
    public int EphemeralIpCount { get; set; }

    /// <summary>
    /// Gets or sets the persistent IP count.
    /// </summary>
    [JsonPropertyName("persistentIpCount")]
    public int PersistentIpCount { get; set; }

    /// <summary>
    /// Gets or sets the CPU usage.
    /// </summary>
    [JsonPropertyName("cpu")]
    public float Cpu { get; set; }

    /// <summary>
    /// Gets or sets the load.
    /// </summary>
    [JsonPropertyName("load")]
    public float Load { get; set; }

    /// <summary>
    /// Gets or sets the memory usage.
    /// </summary>
    [JsonPropertyName("mem")]
    public float Mem { get; set; }
}

/// <summary>
/// Result of batch instance metadata operation.
/// </summary>
public class InstanceMetadataBatchResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    [JsonPropertyName("updated")]
    public bool Updated { get; set; }

    /// <summary>
    /// Gets or sets the list of updated instances.
    /// </summary>
    [JsonPropertyName("updatedInstances")]
    public List<string>? UpdatedInstances { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Generic page result.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
public class Page<T>
{
    /// <summary>
    /// Gets or sets the total count.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the list of items.
    /// </summary>
    [JsonPropertyName("list")]
    public List<T> List { get; set; } = new();

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [JsonPropertyName("pageNo")]
    public int PageNo { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total pages.
    /// </summary>
    [JsonPropertyName("pagesAvailable")]
    public int PagesAvailable { get; set; }

    /// <summary>
    /// Creates an empty page.
    /// </summary>
    public static Page<T> Empty() => new() { List = new List<T>() };
}
