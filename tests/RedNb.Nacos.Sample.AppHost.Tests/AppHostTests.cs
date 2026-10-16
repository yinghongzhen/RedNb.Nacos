using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedNb.Nacos.AspNetCore.HealthChecks;
using RedNb.Nacos.Grpc;
using RedNb.Nacos.Sample.AppHost;
using Xunit;

namespace RedNb.Nacos.Sample.AppHost.Tests;

public class AppHostTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OneLaunchInitializesNacosStartsSamplesAndReloadsConfiguration(bool configuredPassword)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        var ct = timeout.Token;
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<AppHostMarker>(
            configuredPassword ? ["--Parameters:nacos-password=nacos-aspire-test-password"] : [], ct);
        var passwordParameter = builder.Resources.OfType<ParameterResource>().Single(r => r.Name == "nacos-password");
        await using var app = await builder.BuildAsync(ct);
        await app.StartAsync(ct);
        var password = await passwordParameter.GetValueAsync(ct);
        Assert.False(string.IsNullOrWhiteSpace(password));
        if (configuredPassword) Assert.Equal("nacos-aspire-test-password", password);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("webapi", ct);
        await app.ResourceNotifications.WaitForResourceAsync("console", KnownResourceStates.Running, ct);

        using var web = app.CreateHttpClient("webapi", "http");
        Assert.Contains("Hello from Aspire and Nacos", await web.GetStringAsync("/demo", ct));
        using var health = await web.GetAsync("/health", ct);
        health.EnsureSuccessStatusCode();

        var api = app.GetEndpoint("nacos", "api");
        var grpc = app.GetEndpoint("nacos", "grpc");
        var options = new NacosClientOptions
        {
            ServerAddresses = api.Authority,
            ConsoleAddresses = app.GetEndpoint("nacos", "console").AbsoluteUri,
            GrpcPortOffset = grpc.Port - api.Port,
            Username = "nacos",
            Password = password
        };
        // Exercise issue #9's actual HTTP branch against a real server, not the DI connection-state branch.
        Assert.Equal(HealthStatus.Healthy, (await new NacosHealthCheck(options).CheckHealthAsync(new HealthCheckContext(), ct)).Status);
        await using var naming = await NacosGrpcFactory.CreateNamingServiceAsync(options);
        var instance = Assert.Single(await naming.GetAllInstancesAsync("sample-webapi", ct));
        Assert.Equal("127.0.0.1", instance.Ip);
        Assert.Equal(app.GetEndpoint("webapi", "http").Port, instance.Port);
        await using var config = await NacosGrpcFactory.CreateConfigServiceAsync(options);
        Assert.True(await config.PublishConfigAsync("app-config", "DEFAULT_GROUP", """{"Demo":{"Message":"Aspire hot reload verified"}}""", "json", ct));
        using var reload = CancellationTokenSource.CreateLinkedTokenSource(ct);
        reload.CancelAfter(TimeSpan.FromSeconds(30));
        while (!(await web.GetStringAsync("/demo", reload.Token)).Contains("Aspire hot reload verified", StringComparison.Ordinal))
            await Task.Delay(200, reload.Token);

        // AI uses the local echo provider; a non-zero exit is a failed sample, not an accepted skip.
        var ai = await app.ResourceNotifications.WaitForResourceAsync("ai",
            e => e.Snapshot.State?.Text == KnownResourceStates.Exited || e.Snapshot.State?.Text == KnownResourceStates.Finished, ct);
        Assert.Equal(0, ai.Snapshot.ExitCode);
    }
}
