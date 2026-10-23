using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedNb.Nacos.AspNetCore.HealthChecks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public class HealthFailoverTests
{
    [Fact]
    public async Task HealthySecondConsoleCanServeApplicationAvailability()
    {
        using var first = TestHttpServer.Start();
        using var second = TestHttpServer.Start();
        first.Given(Request.Create().WithPath("/v3/console/health/readiness")).RespondWith(Response.Create().WithStatusCode(503));
        second.Given(Request.Create().WithPath("/v3/console/health/readiness")).RespondWith(Response.Create().WithStatusCode(200));
        var check = new NacosHealthCheck(new NacosClientOptions { ConsoleAddresses = $"localhost:{first.Port},localhost:{second.Port}" });
        var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }
}
