using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedNb.Nacos.Config;
using RedNb.Nacos.Naming;
using RedNb.Nacos.Utils;

namespace RedNb.Nacos.AspNetCore.HealthChecks;

/// <summary>Registers singleton checks and reusable named HTTP pipelines.</summary>
public static class NacosHealthCheckExtensions
{
    /// <summary>The named client can be customized using services.AddHttpClient with this name.</summary>
    public static string GetHttpClientName(string healthCheckName = "nacos") => "RedNb.Nacos.Health." + healthCheckName;

    /// <summary>Checks Config, or Naming when Config is not registered. Missing clients do not trigger a Console probe.</summary>
    public static IHealthChecksBuilder AddNacos(this IHealthChecksBuilder builder, string name = "nacos",
        HealthStatus? failureStatus = null, IEnumerable<string>? tags = null)
    {
        var key = GetHttpClientName(name);
        builder.Services.AddKeyedSingleton<NacosHealthCheck>(key, (sp, _) =>
        {
            var config = sp.GetService<IConfigService>();
            return new NacosHealthCheck(new(), new() { Mode = NacosHealthCheckMode.ClientConnections,
                RequiredClients = config != null ? NacosHealthCheckClients.Config : NacosHealthCheckClients.Naming },
                configService: config, namingService: config == null ? sp.GetService<INamingService>() : null);
        });
        return builder.Add(new HealthCheckRegistration(name, sp => sp.GetRequiredKeyedService<NacosHealthCheck>(key), failureStatus, tags));
    }

    /// <summary>Checks Console availability with Any aggregation and the supplied transport settings.</summary>
    public static IHealthChecksBuilder AddNacos(this IHealthChecksBuilder builder, NacosClientOptions options,
        string name = "nacos", HealthStatus? failureStatus = null, IEnumerable<string>? tags = null) =>
        builder.AddNacos(options, new NacosHealthCheckOptions(), name, failureStatus, tags);

    /// <summary>Registers independently named checks with bounded probes and caller-configurable HTTP pipelines.</summary>
    public static IHealthChecksBuilder AddNacos(this IHealthChecksBuilder builder, NacosClientOptions options,
        NacosHealthCheckOptions healthOptions, string name = "nacos", HealthStatus? failureStatus = null, IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(healthOptions);
        var key = GetHttpClientName(name);
        builder.Services.AddHttpClient(key, client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => NacosTransport.CreateHandler(options));
        builder.Services.AddKeyedSingleton<NacosHealthCheck>(key, (sp, _) => NacosHealthCheck.FromFactory(options, healthOptions,
            sp.GetRequiredService<IHttpClientFactory>(), key,
            healthOptions.Mode == NacosHealthCheckMode.ClientConnections && healthOptions.RequiredClients.HasFlag(NacosHealthCheckClients.Config) ? sp.GetService<IConfigService>() : null,
            healthOptions.Mode == NacosHealthCheckMode.ClientConnections && healthOptions.RequiredClients.HasFlag(NacosHealthCheckClients.Naming) ? sp.GetService<INamingService>() : null));
        return builder.Add(new HealthCheckRegistration(name, sp => sp.GetRequiredKeyedService<NacosHealthCheck>(key), failureStatus, tags));
    }
}
