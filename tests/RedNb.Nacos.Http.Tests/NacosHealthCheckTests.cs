using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using RedNb.Nacos.AspNetCore.HealthChecks;
using RedNb.Nacos.Config;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace RedNb.Nacos.Http.Tests;

public class NacosHealthCheckTests
{
    [Theory]
    [InlineData(false, 200, HealthStatus.Healthy)]
    [InlineData(true, 200, HealthStatus.Healthy)]
    [InlineData(false, 503, HealthStatus.Degraded)]
    [InlineData(true, 503, HealthStatus.Degraded)]
    public async Task HttpBranchRequestsConfiguredConsole(bool includeScheme, int status, HealthStatus expected)
    {
        using var server = TestHttpServer.Start();
        server.Given(Request.Create().WithPath("/v3/console/health/readiness").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(status));
        var address = $"localhost:{server.Port}";
        var options = new NacosClientOptions { ConsoleAddresses = includeScheme ? "http://" + address : address };

        var result = await new NacosHealthCheck(options).CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.Status);
        var request = Assert.Single(server.LogEntries).RequestMessage;
        Assert.Equal("/v3/console/health/readiness", request.Path);
        Assert.Equal("localhost", new Uri(request.Url!).Host);
        Assert.Equal(server.Port, new Uri(request.Url!).Port);
    }

    [Theory]
    [InlineData("UP", HealthStatus.Healthy)]
    [InlineData("DOWN", HealthStatus.Unhealthy)]
    public async Task ConfigServiceBranchUsesConnectionStatus(string status, HealthStatus expected)
    {
        var config = new Mock<IConfigService>(MockBehavior.Strict);
        config.Setup(x => x.GetServerStatus()).Returns(status);
        var result = await new NacosHealthCheck(config.Object).CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);
        Assert.Equal(expected, result.Status);
        config.Verify(x => x.GetServerStatus(), Times.Once);
        config.VerifyNoOtherCalls();
    }
}
