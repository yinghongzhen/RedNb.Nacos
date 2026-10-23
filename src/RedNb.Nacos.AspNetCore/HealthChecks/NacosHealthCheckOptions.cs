namespace RedNb.Nacos.AspNetCore.HealthChecks;

/// <summary>The component measured by a health check.</summary>
public enum NacosHealthCheckMode
{
    /// <summary>Config/Naming client state, without substituting HTTP probes.</summary>
    ClientConnections,
    /// <summary>Console HTTP readiness.</summary>
    Console,
    /// <summary>Server HTTP readiness.</summary>
    Server,
    /// <summary>Server and Console HTTP readiness, reported independently.</summary>
    ServerAndConsole
}
/// <summary>Aggregation within each selected component.</summary>
public enum NacosHealthCheckStrategy
{
    /// <summary>One healthy node in every selected component is sufficient.</summary>
    Any,
    /// <summary>All healthy is Healthy; partial availability is Degraded; no availability is Unhealthy.</summary>
    All
}
/// <summary>The application capabilities that must be connected.</summary>
[Flags]
public enum NacosHealthCheckClients
{
    /// <summary>Configuration connectivity.</summary>
    Config = 1,
    /// <summary>Naming connectivity.</summary>
    Naming = 2,
    /// <summary>Both capabilities must be connected.</summary>
    All = Config | Naming
}

/// <summary>Probe settings copied when the check is constructed.</summary>
public sealed class NacosHealthCheckOptions
{
    /// <summary>Capabilities required by ClientConnections mode; defaults to Config for compatibility.</summary>
    public NacosHealthCheckClients RequiredClients { get; set; } = NacosHealthCheckClients.Config;
    /// <summary>The options-based check defaults to Console.</summary>
    public NacosHealthCheckMode Mode { get; set; } = NacosHealthCheckMode.Console;
    /// <summary>Aggregation defaults to Any.</summary>
    public NacosHealthCheckStrategy Strategy { get; set; } = NacosHealthCheckStrategy.Any;
    /// <summary>Finish all probes even when Any is satisfied, for complete diagnostics.</summary>
    public bool ProbeAll { get; set; }
    /// <summary>Per-node I/O deadline, excluding the wait for a probe slot.</summary>
    public TimeSpan EndpointTimeout { get; set; } = TimeSpan.FromSeconds(2);
    /// <summary>Whole-check deadline, including queued probes.</summary>
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>Maximum simultaneous probes across invocations of this check.</summary>
    public int MaxConcurrency { get; set; } = 4;
}
/// <summary>Safe diagnostics without response bodies, credentials or exception messages.</summary>
public sealed record NacosNodeHealthResult(string Component, string Address, bool Healthy, long ElapsedMilliseconds, int? HttpStatusCode, string? Failure);
