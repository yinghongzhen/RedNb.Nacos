using System.Diagnostics;
using System.Runtime.CompilerServices;
using RedNb.Nacos.Config;
using RedNb.Nacos.Grpc;
using RedNb.Nacos.Grpc.Ai;
using RedNb.Nacos.Ai.Models.Mcp;
using Xunit;

namespace RedNb.Nacos.IntegrationTests;

public sealed class LocalFaultFactAttribute : FactAttribute
{
    public LocalFaultFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (Environment.GetEnvironmentVariable("NACOS_TEST_FAULT_CONTAINER") == null)
            Skip = "Local container fault injection is enabled only by NACOS_TEST_FAULT_CONTAINER.";
    }
}

[Collection("NacosIntegration")]
public class ConnectionRecoveryTests
{
    [LocalFaultFact]
    public async Task IdleRegistrationAndConfigSubscriptionRecoverAfterRestart()
    {
        var options = ConfigReliabilityTests.Options();
        Assert.True(options.ServerAddresses.StartsWith("localhost:") || options.ServerAddresses.StartsWith("127.0.0.1:"),
            "Fault injection is forbidden against remote servers.");
        var container = Environment.GetEnvironmentVariable("NACOS_TEST_FAULT_CONTAINER")!;
        Assert.Matches("^rednb-nacos-[a-z0-9-]+$", container);
        var name = "audit-rec-" + Guid.NewGuid().ToString("N")[..16];
        await using var naming = await NacosGrpcFactory.CreateNamingServiceAsync(options);
        await using var config = await NacosGrpcFactory.CreateConfigServiceAsync(options);
        var aiOptions = ConfigReliabilityTests.Options(); aiOptions.Namespace = "public";
        await using var ai = new NacosAiClient(aiOptions);
        var mcpName = name + "-mcp";
        var backend = name + "-backend";
        var mcpCreated = false;
        var updated = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = new ConfigReliabilityTests.Listener(info => { if (info.Content == "after") updated.TrySetResult(true); });
        try
        {
            await config.PublishConfigAsync(name, "DEFAULT_GROUP", "before");
            await ConfigReliabilityTests.Eventually(async () => await config.GetConfigAsync(name, "DEFAULT_GROUP", 5000) == "before");
            await config.AddListenerAsync(name, "DEFAULT_GROUP", listener);
            await naming.RegisterInstanceAsync(name, "127.0.0.1", 19234);
            await ai.ReleaseMcpServerAsync(new McpServerBasicInfo
            {
                Name = mcpName,
                Protocol = "mcp-sse",
                VersionDetail = new ServerVersionDetail { Version = "1.0.0" },
                RemoteServerConfig = new McpServerRemoteServiceConfig
                {
                    ServiceRef = new McpServiceRef { NamespaceId = "public", GroupName = "DEFAULT_GROUP", ServiceName = backend }
                }
            }, null, new McpEndpointSpec
            {
                Type = "REF",
                Data = new() { ["namespaceId"] = "public", ["groupName"] = "DEFAULT_GROUP", ["serviceName"] = backend }
            });
            mcpCreated = true;
            await ai.RegisterMcpServerEndpointAsync(mcpName, "127.0.0.1", 19235, "1.0.0");
            var start = new ProcessStartInfo("docker") { UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add("restart"); start.ArgumentList.Add(container);
            using var process = Process.Start(start)!;
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(90));
            Assert.Equal(0, process.ExitCode);
            await new NacosServerFixture().InitializeAsync();
            // Do not issue requests through the original clients to drive reconnection.
            await using var observer = await NacosGrpcFactory.CreateNamingServiceAsync(options);
            await ConfigReliabilityTests.Eventually(async () =>
                (await observer.GetAllInstancesAsync(name, false)).Any(i => i.Port == 19234), 60);
            await using var aiObserver = await NacosGrpcFactory.CreateNamingServiceAsync(aiOptions);
            await ConfigReliabilityTests.Eventually(async () =>
                (await aiObserver.GetAllInstancesAsync(backend, false)).Any(i => i.Port == 19235), 60);
            await using var publisher = await NacosGrpcFactory.CreateConfigServiceAsync(options);
            await publisher.PublishConfigAsync(name, "DEFAULT_GROUP", "after");
            await updated.Task.WaitAsync(TimeSpan.FromSeconds(45));
        }
        finally
        {
            config.RemoveListener(name, "DEFAULT_GROUP", listener);
            await naming.DeregisterInstanceAsync(name, "127.0.0.1", 19234);
            await config.RemoveConfigAsync(name, "DEFAULT_GROUP");
            if (mcpCreated)
            {
                try { await ai.DeregisterMcpServerEndpointAsync(mcpName, "127.0.0.1", 19235); }
                finally { await ai.DeleteMcpServerAsync(mcpName); }
            }
        }
    }
}
