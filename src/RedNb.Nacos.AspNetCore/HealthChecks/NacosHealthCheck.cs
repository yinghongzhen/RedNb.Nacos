using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedNb.Nacos;
using RedNb.Nacos.Config;

namespace RedNb.Nacos.AspNetCore.HealthChecks;

/// <summary>
/// Health check for Nacos server connectivity.
/// </summary>
public class NacosHealthCheck : IHealthCheck
{
    private readonly IConfigService? _configService;
    private readonly NacosClientOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="NacosHealthCheck"/> class.
    /// </summary>
    /// <param name="configService">Optional config service for connectivity check.</param>
    public NacosHealthCheck(IConfigService? configService = null)
    {
        _configService = configService;
        _options = new NacosClientOptions();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NacosHealthCheck"/> class with options.
    /// </summary>
    /// <param name="options">Nacos client options.</param>
    public NacosHealthCheck(NacosClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_configService != null)
            {
                var status = _configService.GetServerStatus();
                if (status == "UP")
                {
                    return HealthCheckResult.Healthy("Nacos server is healthy");
                }
                return HealthCheckResult.Unhealthy($"Nacos server status: {status}");
            }

            // If no config service, try HTTP connectivity check
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var serverAddress = _options.GetConsoleAddressList().First();
            using var response = await httpClient.GetAsync(
                $"{_options.GetConsoleBaseUrl(serverAddress).TrimEnd('/')}/v3/console/health/readiness",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Nacos server is reachable");
            }

            return HealthCheckResult.Degraded($"Nacos server returned status code: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to connect to Nacos server", ex);
        }
    }
}
