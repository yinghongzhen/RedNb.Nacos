using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Config;

namespace RedNb.Nacos.AspNetCore.HealthChecks;

/// <summary>
/// Extension methods for adding Nacos health checks.
/// </summary>
public static class NacosHealthCheckExtensions
{
    /// <summary>
    /// Adds a health check for Nacos server connectivity.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name. Defaults to "nacos".</param>
    /// <param name="failureStatus">The failure status. Defaults to Unhealthy.</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddNacos(
        this IHealthChecksBuilder builder,
        string name = "nacos",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new NacosHealthCheck(sp.GetService<IConfigService>()),
            failureStatus,
            tags));
    }

    /// <summary>
    /// Adds a health check for Nacos server with custom options.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="options">Nacos client options.</param>
    /// <param name="name">The health check name. Defaults to "nacos".</param>
    /// <param name="failureStatus">The failure status. Defaults to Unhealthy.</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddNacos(
        this IHealthChecksBuilder builder,
        NacosClientOptions options,
        string name = "nacos",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new NacosHealthCheck(options),
            failureStatus,
            tags));
    }
}
