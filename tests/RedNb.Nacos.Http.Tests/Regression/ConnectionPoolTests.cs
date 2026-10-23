using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.AspNetCore.HealthChecks;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public class ConnectionPoolTests
{
    [Fact]
    public async Task RepeatedChecksReuseOneTcpConnection()
    {
        var connections = new ConcurrentDictionary<string, byte>();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0));
        await using var app = builder.Build();
        app.MapGet("/v3/console/health/readiness", (HttpContext c) =>
        { connections.TryAdd(c.Connection.Id, 0); return Results.Ok(); });
        await app.StartAsync(TestContext.Current.CancellationToken);
        var check = new NacosHealthCheck(new NacosClientOptions { ConsoleAddresses = app.Urls.Single() });
        try
        {
            await check.CheckHealthAsync(new(), TestContext.Current.CancellationToken);
            await Task.Delay(100, TestContext.Current.CancellationToken);
            await check.CheckHealthAsync(new(), TestContext.Current.CancellationToken);
            Assert.Single(connections);
        }
        finally { (check as IDisposable)?.Dispose(); }
    }
}
