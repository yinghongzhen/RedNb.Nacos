using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using RedNb.Nacos.AspNetCore.HealthChecks;
using RedNb.Nacos.Config;
using RedNb.Nacos.Naming;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public class HealthDiagnosticsTests
{
    [Theory]
    [InlineData(NacosHealthCheckStrategy.Any, false, true, HealthStatus.Healthy)]
    [InlineData(NacosHealthCheckStrategy.Any, true, false, HealthStatus.Healthy)]
    [InlineData(NacosHealthCheckStrategy.All, true, false, HealthStatus.Degraded)]
    [InlineData(NacosHealthCheckStrategy.All, false, false, HealthStatus.Unhealthy)]
    [InlineData(NacosHealthCheckStrategy.All, true, true, HealthStatus.Healthy)]
    public async Task AggregationRetainsEveryNodeResult(NacosHealthCheckStrategy strategy, bool first, bool second, HealthStatus expected)
    {
        using var handler = new ProbeHandler((r, _) => Task.FromResult(new HttpResponseMessage(
            (r.RequestUri!.Host == "one" ? first : second) ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)));
        using var client = new HttpClient(handler);
        using var check = new NacosHealthCheck(new() { ConsoleAddresses = "http://one:8080,https://two:8443" },
            new() { Strategy = strategy, ProbeAll = true }, client);
        var result = await check.CheckHealthAsync(new(), TestContext.Current.CancellationToken);
        Assert.Equal(expected, result.Status);
        var nodes = Assert.IsType<NacosNodeHealthResult[]>(result.Data["nodes"]);
        Assert.Equal(2, nodes.Length);
        Assert.Equal(first, nodes[0].Healthy); Assert.Equal(second, nodes[1].Healthy);
        Assert.All(nodes, n => Assert.Equal("Console", n.Component));
    }

    [Fact]
    public async Task ConsoleSuccessCannotHideServerFailureAndDiagnosticsRedactSecrets()
    {
        using var handler = new ProbeHandler((r, _) => Task.FromResult(new HttpResponseMessage(
            r.RequestUri!.AbsolutePath == "/nacos/v3/admin/core/state/readiness" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)));
        using var client = new HttpClient(handler);
        using var check = new NacosHealthCheck(new() { ServerAddresses = "http://user:secret@node:8848", ConsoleAddresses = "node:8080" },
            new() { Mode = NacosHealthCheckMode.ServerAndConsole, ProbeAll = true }, client);
        var result = await check.CheckHealthAsync(new(), TestContext.Current.CancellationToken);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        var nodes = Assert.IsType<NacosNodeHealthResult[]>(result.Data["nodes"]);
        Assert.Contains(nodes, n => n.Component == "Server" && !n.Healthy);
        Assert.Contains(nodes, n => n.Component == "Console" && n.Healthy);
        Assert.All(nodes, n => Assert.DoesNotContain("secret", n.Address));
    }

    [Fact]
    public async Task OneHealthyNodeCompletesAnyWithoutWaitingForSlowPeer()
    {
        using var handler = new ProbeHandler(async (r, ct) =>
        {
            if (r.RequestUri!.Host == "slow") await Task.Delay(10000, ct);
            return new(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        using var check = new NacosHealthCheck(new() { ConsoleAddresses = "slow:8080,fast:8080" }, new() { EndpointTimeout = TimeSpan.FromSeconds(5) }, client);
        var watch = Stopwatch.StartNew();
        Assert.Equal(HealthStatus.Healthy, (await check.CheckHealthAsync(new(), TestContext.Current.CancellationToken)).Status);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ConcurrentChecksRespectSharedConcurrencyAndExternalOwnership()
    {
        using var handler = new ProbeHandler(async (_, ct) => { await Task.Delay(30, ct); return new(HttpStatusCode.OK); });
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
        var check = new NacosHealthCheck(new() { ConsoleAddresses = "a:8080,b:8080,c:8080,d:8080" }, new() { ProbeAll = true, MaxConcurrency = 2 }, client);
        await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => check.CheckHealthAsync(new(), TestContext.Current.CancellationToken)));
        Assert.InRange(handler.Maximum, 1, 2);
        check.Dispose();
        Assert.Equal(0, handler.Disposals);
        Assert.Equal(TimeSpan.FromSeconds(20), client.Timeout);
        using var response = await client.GetAsync("http://still-owned-by-caller", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancellationAndWholeCheckDeadlineBoundQueuedRequests(bool callerCancels)
    {
        using var handler = new ProbeHandler(async (_, ct) => { await Task.Delay(10000, ct); return new(HttpStatusCode.OK); });
        using var client = new HttpClient(handler);
        using var check = new NacosHealthCheck(new() { ConsoleAddresses = "a:8080,b:8080,c:8080" }, new()
        { ProbeAll = true, MaxConcurrency = 1, TotalTimeout = TimeSpan.FromMilliseconds(200), EndpointTimeout = TimeSpan.FromSeconds(5) }, client);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        if (callerCancels) stop.CancelAfter(50);
        var watch = Stopwatch.StartNew();
        if (callerCancels) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => check.CheckHealthAsync(new(), stop.Token));
        else Assert.Equal(HealthStatus.Unhealthy, (await check.CheckHealthAsync(new(), stop.Token)).Status);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ClientModeChecksRegisteredCapabilitiesWithoutHttp()
    {
        var config = new Mock<IConfigService>(); config.Setup(x => x.GetServerStatus()).Returns("UP");
        var naming = new Mock<INamingService>(); naming.Setup(x => x.GetServerStatus()).Returns("DOWN");
        using var check = new NacosHealthCheck(new(), new() { Mode = NacosHealthCheckMode.ClientConnections, RequiredClients = NacosHealthCheckClients.All }, configService: config.Object, namingService: naming.Object);
        Assert.Equal(HealthStatus.Unhealthy, (await check.CheckHealthAsync(new(), TestContext.Current.CancellationToken)).Status);
        using var missing = new NacosHealthCheck(new(), new NacosHealthCheckOptions { Mode = NacosHealthCheckMode.ClientConnections });
        Assert.Equal(HealthStatus.Unhealthy, (await missing.CheckHealthAsync(new(), TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task HostCanConfigureNamedPipelineAndChecksReuseTheSameHandler()
    {
        var handler = new ProbeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var services = new ServiceCollection();
        services.AddSingleton<IConfigService>(_ => throw new InvalidOperationException("Console checks must not construct business clients."));
        services.AddSingleton<INamingService>(_ => throw new InvalidOperationException("Console checks must not construct business clients."));
        services.AddHealthChecks().AddNacos(new NacosClientOptions { ConsoleAddresses = "injected:8080" }, name: "custom");
        services.AddHttpClient(NacosHealthCheckExtensions.GetHttpClientName("custom")).ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();
        var health = provider.GetRequiredService<HealthCheckService>();
        Assert.Equal(HealthStatus.Healthy, (await health.CheckHealthAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(HealthStatus.Healthy, (await health.CheckHealthAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(2, handler.Calls); Assert.Equal(0, handler.Disposals);
    }

    [Fact]
    public async Task DefaultDiCheckPreservesConfigSemanticsWithoutForcingUnusedNaming()
    {
        var config = new Mock<IConfigService>(); config.Setup(c => c.GetServerStatus()).Returns("UP");
        var naming = new Mock<INamingService>(MockBehavior.Strict);
        var services = new ServiceCollection();
        services.AddSingleton(config.Object); services.AddSingleton(naming.Object);
        services.AddLogging();
        services.AddHealthChecks().AddNacos();
        await using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HealthStatus.Healthy, result.Status);
        naming.VerifyNoOtherCalls();
    }

    private sealed class ProbeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        private int _active;
        public int Calls, Maximum, Disposals;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            var active = Interlocked.Increment(ref _active);
            int old;
            do { old = Maximum; if (active <= old) break; } while (Interlocked.CompareExchange(ref Maximum, active, old) != old);
            try { return await send(request, ct); } finally { Interlocked.Decrement(ref _active); }
        }
        protected override void Dispose(bool disposing) { if (disposing) Interlocked.Increment(ref Disposals); base.Dispose(disposing); }
    }
}
