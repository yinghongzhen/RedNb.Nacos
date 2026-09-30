using FluentAssertions;
using RedNb.Nacos.Http;
using RedNb.Nacos;
using RedNb.Nacos.Ai;
using RedNb.Nacos.Ai.Models.A2a;
using RedNb.Nacos.Ai.Models.Mcp;
using RedNb.Nacos.Grpc;
using RedNb.Nacos.Grpc.Ai;
using Xunit;
using static RedNb.Nacos.IntegrationTests.TestRetryHelpers;

namespace RedNb.Nacos.IntegrationTests.Ai;

[Collection("NacosIntegration")]
public class GrpcAiServiceIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private IAiService? _grpcAi;
    private IAiService? _httpAi;

    public GrpcAiServiceIntegrationTests(ITestOutputHelper output) { _output = output; }

    public async ValueTask InitializeAsync()
    {
        var options = new NacosClientOptions
        {
            ServerAddresses = NacosServerFixture.ServerAddress,
            ConsoleAddresses = NacosServerFixture.ConsoleAddress,
            Username = NacosServerFixture.Username,
            Password = NacosServerFixture.Password,
            EnableGrpc = true,
            DefaultTimeout = 10000
        };
        _grpcAi = new NacosGrpcFactory().CreateAiService(options);
        if (_grpcAi is NacosGrpcAiService gs)
            await gs.InitializeAsync();

        // Shared HTTP-side service for cleanup. The Nacos 3.2.4 server does not
        // register gRPC handlers for McpServerDeleteRequest / AgentDeleteRequest
        // (only Query/Release/Endpoint/QueryPrompt/Agent are dispatched), so
        // teardown goes through the console-port HTTP admin API instead.
        _httpAi = new NacosFactory().CreateAiService(new NacosClientOptions
        {
            ServerAddresses = NacosServerFixture.ServerAddress,
            ConsoleAddresses = NacosServerFixture.ConsoleAddress,
            Username = NacosServerFixture.Username,
            Password = NacosServerFixture.Password,
            DefaultTimeout = 10000
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_grpcAi is IAsyncDisposable d) await d.DisposeAsync();
        if (_httpAi is IAsyncDisposable h) await h.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ReleaseAndGetMcpServer_ViaGrpc()
    {
        var mcpName = $"grpc-mcp-{Guid.NewGuid():N}";
        var spec = new McpServerBasicInfo
        {
            Name = mcpName,
            VersionDetail = new ServerVersionDetail { Version = "1.0.0" },
            Protocol = "stdio"
        };

        var released = await _grpcAi!.ReleaseMcpServerAsync(spec, toolSpecification: null);
        released.Should().NotBeNullOrEmpty();

        var got = await WaitForAsync(_output, () => _grpcAi.GetMcpServerAsync(mcpName), s => s is not null);
        got!.Name.Should().Be(mcpName);

        await DeleteWithRetryAsync(_output, () => _httpAi!.DeleteMcpServerAsync(mcpName));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task RegisterAndDeregisterMcpEndpoint_ViaGrpc()
    {
        var mcpName = $"grpc-mcp-ep-{Guid.NewGuid():N}";
        var serviceName = $"svc-{Guid.NewGuid():N}";
        var released = await _grpcAi!.ReleaseMcpServerAsync(new McpServerBasicInfo
        {
            Name = mcpName,
            VersionDetail = new ServerVersionDetail { Version = "1.0.0" },
            Protocol = "mcp-sse",
            RemoteServerConfig = new McpServerRemoteServiceConfig
            {
                ServiceRef = new McpServiceRef
                {
                    NamespaceId = "public",
                    GroupName = "DEFAULT_GROUP",
                    ServiceName = serviceName
                }
            }
        }, toolSpecification: null);
        released.Should().NotBeNullOrEmpty();

        try
        {
            await WaitForAsync(_output, () => _grpcAi.GetMcpServerAsync(mcpName), s => s is not null);

            await _grpcAi.RegisterMcpServerEndpointAsync(mcpName, "127.0.0.1", 9100, "1.0.0");
            var got = await WaitForAsync(_output, () => _grpcAi.GetMcpServerAsync(mcpName), s => s is not null);
            got.Should().NotBeNull();

            await _grpcAi.DeregisterMcpServerEndpointAsync(mcpName, "127.0.0.1", 9100);
        }
        finally
        {
            await DeleteWithRetryAsync(_output, () => _httpAi!.DeleteMcpServerAsync(mcpName));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ReleaseAndGetAgentCard_ViaGrpc()
    {
        var agentName = $"grpc-agent-{Guid.NewGuid():N}";
        var card = new AgentCard
        {
            Name = agentName,
            Version = "1.0.0",
            ProtocolVersion = "0.3.7",
            PreferredTransport = "jsonrpc",
            Url = "http://127.0.0.1:9999"
        };

        await _grpcAi!.ReleaseAgentCardAsync(card);

        var got = await WaitForAsync(_output, () => _grpcAi.GetAgentCardAsync(agentName), s => s is not null);
        got!.Name.Should().Be(agentName);

        await DeleteWithRetryAsync(_output, () => _httpAi!.DeleteAgentAsync(agentName));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task CrossChannel_ReleaseViaGrpc_VisibleViaHttpList()
    {
        var mcpName = $"cross-mcp-{Guid.NewGuid():N}";
        var released = await _grpcAi!.ReleaseMcpServerAsync(new McpServerBasicInfo
        {
            Name = mcpName,
            VersionDetail = new ServerVersionDetail { Version = "1.0.0" },
            Protocol = "stdio"
        }, toolSpecification: null);
        released.Should().NotBeNullOrEmpty();

        try
        {
            var page = await WaitForAsync(
                _output,
                () => _httpAi!.ListMcpServersAsync(pageSize: 100),
                p => p is not null && p.PageItems.Any(i => i.Name == mcpName));
            page.PageItems.Should().Contain(i => i.Name == mcpName);
        }
        finally
        {
            await DeleteWithRetryAsync(_output, () => _httpAi!.DeleteMcpServerAsync(mcpName));
        }
    }

    // Task 2 — BatchAgentEndpointRequest replaces the per-endpoint loop. Live
    // verification is gated: the Nacos 3.2.4 container is currently in a Derby
    // crash-loop (audit §1 — see .tmp/audit-server-api-3.2.4.md), so the test
    // would block on NacosServerFixture.InitializeAsync's 90s readiness probe.
    // Authored for re-enable once the container recovers.
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task RegisterAgentEndpoints_BatchOp_PreservesAgentCard()
    {
        var agentName = $"grpc-batch-{Guid.NewGuid():N}";
        var card = new AgentCard
        {
            Name = agentName,
            Version = "1.0.0",
            ProtocolVersion = "0.3.7",
            PreferredTransport = "jsonrpc",
            Url = "http://127.0.0.1:9999"
        };

        await _grpcAi!.ReleaseAgentCardAsync(card);

        try
        {
            await WaitForAsync(_output, () => _grpcAi.GetAgentCardAsync(agentName), s => s is not null);

            var endpoints = new List<AgentEndpoint>
            {
                new() { Address = "10.0.0.1", Port = 9001, Version = "1.0.0", Transport = AiConstants.A2a.TransportJsonRpc },
                new() { Address = "10.0.0.2", Port = 9002, Version = "1.0.0", Transport = AiConstants.A2a.TransportGrpc },
                new() { Address = "10.0.0.3", Port = 9003, Version = "1.0.0" }
            };

            // Single batch dispatch — the unit test asserts the wire shape
            // (one payload, three endpoints). Here we verify the live server
            // accepts it without throwing and the agent card remains queryable.
            await _grpcAi.RegisterAgentEndpointsAsync(agentName, endpoints);

            var stillVisible = await _grpcAi.GetAgentCardAsync(agentName);
            stillVisible.Should().NotBeNull();
            stillVisible!.Name.Should().Be(agentName);
        }
        finally
        {
            await DeleteWithRetryAsync(_output, () => _httpAi!.DeleteAgentAsync(agentName));
        }
    }
}
