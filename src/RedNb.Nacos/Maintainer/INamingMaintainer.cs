namespace RedNb.Nacos.Core.Maintainer;

/// <summary>
/// Nacos naming maintainer interface for operational management.
/// </summary>
public interface INamingMaintainer
{
    /// <summary>
    /// Gets system metrics.
    /// </summary>
    Task<MetricsInfo> GetMetricsAsync(bool onlyStatus = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets log level.
    /// </summary>
    Task<string> SetLogLevelAsync(string logName, string logLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates persistent instance health status.
    /// </summary>
    Task<string> UpdateInstanceHealthStatusAsync(
        string groupName,
        string serviceName,
        string ip,
        int port,
        bool healthy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available health checkers.
    /// </summary>
    Task<Dictionary<string, HealthCheckerInfo>> GetHealthCheckersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates cluster configuration.
    /// </summary>
    Task<string> UpdateClusterAsync(
        string groupName,
        string serviceName,
        ClusterInfo cluster,
        CancellationToken cancellationToken = default);
}
