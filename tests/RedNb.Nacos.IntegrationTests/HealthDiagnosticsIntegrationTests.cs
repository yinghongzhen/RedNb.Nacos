using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedNb.Nacos.AspNetCore.HealthChecks;
using Xunit;

namespace RedNb.Nacos.IntegrationTests;

[Collection("NacosIntegration")]
public class HealthDiagnosticsIntegrationTests
{
    [Theory]
    [InlineData(NacosHealthCheckStrategy.Any)]
    [InlineData(NacosHealthCheckStrategy.All)]
    public async Task ServerAndConsoleHaveIndependentRealReadinessEndpoints(NacosHealthCheckStrategy strategy)
    {
        using var check = new NacosHealthCheck(new NacosClientOptions
        { ServerAddresses = NacosServerFixture.ServerAddress, ConsoleAddresses = NacosServerFixture.ConsoleAddress },
            new NacosHealthCheckOptions { Mode = NacosHealthCheckMode.ServerAndConsole, Strategy = strategy, ProbeAll = true });
        var result = await check.CheckHealthAsync(new(), TestContext.Current.CancellationToken);
        Assert.Equal(HealthStatus.Healthy, result.Status);
        var nodes = Assert.IsType<NacosNodeHealthResult[]>(result.Data["nodes"]);
        Assert.Equal(2, nodes.Length);
        Assert.All(nodes, n => Assert.Equal(200, n.HttpStatusCode));
    }
}
