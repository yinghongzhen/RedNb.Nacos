using FluentAssertions;
using RedNb.Nacos.Http;
using RedNb.Nacos;
using RedNb.Nacos.Ai;
using RedNb.Nacos.Ai.Listener;
using RedNb.Nacos.Ai.Models.A2a;
using RedNb.Nacos.Ai.Models.Mcp;
using Xunit;
using static RedNb.Nacos.IntegrationTests.TestRetryHelpers;

namespace RedNb.Nacos.IntegrationTests;

[Collection("NacosIntegration")]
public class AiServiceIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private IAiService? _aiService;
    private readonly NacosClientOptions _options;

    public AiServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _options = new NacosClientOptions
        {
            ServerAddresses = NacosServerFixture.ServerAddress,
            ConsoleAddresses = NacosServerFixture.ConsoleAddress,
            Username = NacosServerFixture.Username,
            Password = NacosServerFixture.Password,
            Namespace = "",
            DefaultTimeout = 10000
        };
    }

    public ValueTask InitializeAsync()
    {
        _aiService = new NacosFactory().CreateAiService(_options);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_aiService is IAsyncDisposable d) await d.DisposeAsync();
    }

    // ---------- Agent Card (HTTP / console) ----------

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ReleaseAndGetAgentCard_RoundTripsViaConsole()
    {
        var agentName = $"it-agent-{Guid.NewGuid():N}";
        var card = new AgentCard
        {
            Name = agentName,
            Version = "1.0.0",
            ProtocolVersion = "0.3.7",
            PreferredTransport = "jsonrpc",
            Url = "http://127.0.0.1:9999"
        };

        await _aiService!.ReleaseAgentCardAsync(card);

        try
        {
            var retrieved = await WaitForAsync(
                _output,
                () => _aiService!.GetAgentCardAsync(agentName),
                c => c is not null);
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be(agentName);
            retrieved.Version.Should().Be("1.0.0");
        }
        finally
        {
            await DeleteWithRetryAsync(_output, () => _aiService!.DeleteAgentAsync(agentName));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task RegisterAndDeregisterAgentEndpoint_HttpChannel_ThrowsLoudly()
    {
        var agentName = $"it-agent-ep-{Guid.NewGuid():N}";
        var endpoint = new AgentEndpoint
        {
            Address = "127.0.0.1",
            Port = 9000,
            Version = "1.0.0",
            Transport = AiConstants.A2a.TransportJsonRpc
        };

        Func<Task> act = () => _aiService!.RegisterAgentEndpointAsync(agentName, endpoint);
        var ex = await act.Should().ThrowAsync<NacosException>();
        ex.Which.ErrorCode.Should().Be(NacosException.ServerError);
        ex.Which.Message.Should().Contain("not available over the HTTP channel");

        Func<Task> dereg = () => _aiService!.DeregisterAgentEndpointAsync(agentName, endpoint);
        var ex2 = await dereg.Should().ThrowAsync<NacosException>();
        ex2.Which.ErrorCode.Should().Be(NacosException.ServerError);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ListAgentCards_ReturnsReleasedAgent()
    {
        var agentName = $"it-agent-list-{Guid.NewGuid():N}";
        await _aiService!.ReleaseAgentCardAsync(new AgentCard
        {
            Name = agentName,
            Version = "1.0.0",
            ProtocolVersion = "0.3.7",
            PreferredTransport = "jsonrpc",
            Url = "http://127.0.0.1:9999"
        });

        try
        {
            // The console list endpoint requires an explicit search mode ("accurate"
            // or "blur") and matches agentName exactly, so a pageSize-100 scan of the
            // whole store is neither needed nor deterministic.
            var page = await WaitForAsync(
                _output,
                () => _aiService!.ListAgentCardsAsync(agentName: agentName, search: "accurate"),
                p => p.PageItems.Any(i => i.Name == agentName));
            page.PageItems.Should().Contain(i => i.Name == agentName);
        }
        finally
        {
            await DeleteWithRetryAsync(_output, () => _aiService!.DeleteAgentAsync(agentName));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ListAgentVersions_ReturnsVersions()
    {
        var agentName = $"it-agent-ver-{Guid.NewGuid():N}";
        await _aiService!.ReleaseAgentCardAsync(new AgentCard
        {
            Name = agentName,
            Version = "2.0.0",
            ProtocolVersion = "0.3.7",
            PreferredTransport = "jsonrpc",
            Url = "http://127.0.0.1:9999"
        });

        var versions = await WaitForAsync(
            _output,
            () => _aiService.ListAgentVersionsAsync(agentName),
            list => list is not null && list.Contains("2.0.0"));
        versions.Should().Contain("2.0.0");

        await DeleteWithRetryAsync(_output, () => _aiService.DeleteAgentAsync(agentName));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ListAgentVersionInfos_ReturnsRichMetadata()
    {
        var agentName = $"it-agent-verinfo-{Guid.NewGuid():N}";
        await _aiService!.ReleaseAgentCardAsync(new AgentCard
        {
            Name = agentName,
            Version = "2.0.0",
            ProtocolVersion = "0.3.7",
            PreferredTransport = "jsonrpc",
            Url = "http://127.0.0.1:9999"
        });

        var infos = await WaitForAsync(
            _output,
            () => _aiService.ListAgentVersionInfosAsync(agentName),
            list => list is not null && list.Count > 0);

        infos.Should().Contain(i => i.Version == "2.0.0");
        var info = infos.Single(i => i.Version == "2.0.0");
        info.Latest.Should().BeTrue();
        info.CreatedAt.Should().HaveValue();
        info.UpdatedAt.Should().HaveValue();

        await DeleteWithRetryAsync(_output, () => _aiService.DeleteAgentAsync(agentName));
    }

    // ---------- MCP Server (HTTP / console) ----------

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ReleaseAndGetMcpServer_RoundTripsViaConsole()
    {
        var mcpName = $"it-mcp-{Guid.NewGuid():N}";
        var spec = new McpServerBasicInfo
        {
            Name = mcpName,
            VersionDetail = new ServerVersionDetail { Version = "1.0.0" },
            Protocol = "stdio"
        };

        var mcpId = await _aiService!.ReleaseMcpServerAsync(spec, toolSpecification: null);
        mcpId.Should().NotBeNullOrEmpty();

        try
        {
            var retrieved = await WaitForAsync(
                _output,
                () => _aiService!.GetMcpServerAsync(mcpName),
                s => s is not null);
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be(mcpName);
            retrieved.VersionDetail!.Version.Should().Be("1.0.0");
            // Control for ReleaseMcpServer_WithToolSpecification_...: a release with no
            // tool specification must not report the TOOL capability.
            retrieved.ToolSpec.Should().BeNull();
        }
        finally
        {
            await DeleteWithRetryAsync(_output, () => _aiService!.DeleteMcpServerAsync(mcpName));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ReleaseMcpServer_WithToolSpecification_SucceedsAndMatchesChannelBehavior()
    {
        var mcpName = $"it-mcp-tools-{Guid.NewGuid():N}";
        var spec = new McpServerBasicInfo
        {
            Name = mcpName,
            VersionDetail = new ServerVersionDetail { Version = "1.0.0" },
            Protocol = "stdio"
        };
        var toolSpec = new McpToolSpecification
        {
            Tools = new List<McpTool>
            {
                new() { Name = "echo", Description = "Echo tool" }
            }
        };

        var mcpId = await _aiService!.ReleaseMcpServerAsync(spec, toolSpec);
        mcpId.Should().NotBeNullOrEmpty();

        try
        {
            // The HTTP console channel accepts and persists tool specifications
            // (field name `toolSpecification`, bound by the server's McpDetailForm),
            // so the tool must come back with the released server.
            var retrieved = await WaitForAsync(
                _output,
                () => _aiService!.GetMcpServerAsync(mcpName),
                s => s?.ToolSpec is not null);
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be(mcpName);
            retrieved.ToolSpec.Should().NotBeNull();
            retrieved.ToolSpec!.Tools.Should().Contain(t => t.Name == "echo");
        }
        finally
        {
            await DeleteWithRetryAsync(_output, () => _aiService!.DeleteMcpServerAsync(mcpName));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ReleaseMcpServer_WithEndpointSpecification_CarriesSpecOverConsole()
    {
        var mcpName = $"it-mcp-ep-{Guid.NewGuid():N}";
        var serviceName = $"svc-{Guid.NewGuid():N}";
        var spec = new McpServerBasicInfo
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
        };
        var endpointSpec = new McpEndpointSpec
        {
            Type = AiConstants.Mcp.EndpointTypeRef,
            Data = new Dictionary<string, string>
            {
                { "namespaceId", "public" },
                { "groupName", "DEFAULT_GROUP" },
                { "serviceName", serviceName }
            }
        };

        // A non-local server type is rejected by Nacos 3.2.4 with
        // "request parameter `endpointSpecification` is required if mcp server type
        // not `local`" (probe P6), so a successful release is itself proof that the
        // form field reached the release service.
        var mcpId = await _aiService!.ReleaseMcpServerAsync(spec, toolSpecification: null, endpointSpec);
        mcpId.Should().NotBeNullOrEmpty();

        try
        {
            var retrieved = await WaitForAsync(
                _output,
                () => _aiService.GetMcpServerAsync(mcpName),
                s => s?.RemoteServerConfig?.ServiceRef is not null);
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be(mcpName);
            retrieved.Protocol.Should().Be("mcp-sse");
            retrieved.RemoteServerConfig.Should().NotBeNull();
            retrieved.RemoteServerConfig!.ServiceRef!.ServiceName.Should().Be(serviceName);
            // The backend/frontend endpoint lists stay empty for a REF registration
            // (probe: both empty after release), so presence is asserted on the
            // service reference the endpoint spec resolves through.
        }
        finally
        {
            await DeleteWithRetryAsync(_output, () => _aiService.DeleteMcpServerAsync(mcpName));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ListMcpServers_ReturnsReleasedServer()
    {
        var mcpName = $"it-mcp-list-{Guid.NewGuid():N}";
        await _aiService!.ReleaseMcpServerAsync(new McpServerBasicInfo
        {
            Name = mcpName,
            VersionDetail = new ServerVersionDetail { Version = "1.0.0" },
            Protocol = "stdio"
        }, toolSpecification: null);

        try
        {
            // mcpName is an exact server-side filter, so no pageSize-100 scan.
            var page = await WaitForAsync(
                _output,
                () => _aiService!.ListMcpServersAsync(mcpName: mcpName),
                p => p.PageItems.Any(i => i.Name == mcpName));
            page.PageItems.Should().Contain(i => i.Name == mcpName);
        }
        finally
        {
            await DeleteWithRetryAsync(_output, () => _aiService!.DeleteMcpServerAsync(mcpName));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task RegisterAndDeregisterMcpEndpoint_HttpChannel_ThrowsLoudly()
    {
        Func<Task> reg = () => _aiService!.RegisterMcpServerEndpointAsync("it-mcp-x", "127.0.0.1", 9100);
        var ex = await reg.Should().ThrowAsync<NacosException>();
        ex.Which.ErrorCode.Should().Be(NacosException.ServerError);
        ex.Which.Message.Should().Contain("not available over the HTTP channel");
    }

    // ---------- MCP Server (subscribe — covered separately) ----------

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task SubscribeAgentCard_ReceivesUpdates()
    {
        var name = "audit-agent-watch-" + Guid.NewGuid().ToString("N");
        var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = new TestAgentCardListener(e => { if (e.AgentCard.Version == "1.0.1") received.TrySetResult(true); });
        await _aiService!.ReleaseAgentCardAsync(new AgentCard { Name = name, Version = "1.0.0", ProtocolVersion = "0.3.7", PreferredTransport = "jsonrpc", Url = "http://127.0.0.1:9999" });
        try
        {
            await _aiService.SubscribeAgentCardAsync(name, listener);
            await _aiService.ReleaseAgentCardAsync(new AgentCard { Name = name, Version = "1.0.1", ProtocolVersion = "0.3.7", PreferredTransport = "jsonrpc", Url = "http://127.0.0.1:9999" }, AiConstants.A2a.A2aEndpointTypeService, true);
            await received.Task.WaitAsync(TimeSpan.FromSeconds(35));
        }
        finally { await _aiService.UnsubscribeAgentCardAsync(name, listener); await DeleteWithRetryAsync(_output, () => _aiService.DeleteAgentAsync(name)); }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task SubscribeMcpServer_ReceivesUpdates()
    {
        var name = "audit-mcp-watch-" + Guid.NewGuid().ToString("N");
        var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = new TestMcpServerListener(e => { if (e.McpServerDetailInfo.VersionDetail?.Version == "1.0.1") received.TrySetResult(true); });
        await _aiService!.ReleaseMcpServerAsync(new McpServerBasicInfo { Name = name, Protocol = "stdio", VersionDetail = new ServerVersionDetail { Version = "1.0.0" } }, null);
        try
        {
            await _aiService.SubscribeMcpServerAsync(name, listener);
            await _aiService.ReleaseMcpServerAsync(new McpServerBasicInfo { Name = name, Protocol = "stdio", VersionDetail = new ServerVersionDetail { Version = "1.0.1" } }, null);
            await received.Task.WaitAsync(TimeSpan.FromSeconds(35));
        }
        finally { await _aiService.UnsubscribeMcpServerAsync(name, listener); await DeleteWithRetryAsync(_output, () => _aiService.DeleteMcpServerAsync(name)); }
    }

    // ---------- Test Listeners (unchanged) ----------

    private class TestAgentCardListener : AbstractNacosAgentCardListener
    {
        private readonly Action<NacosAgentCardEvent> _handler;

        public TestAgentCardListener(Action<NacosAgentCardEvent> handler)
        {
            _handler = handler;
        }

        public override void OnEvent(NacosAgentCardEvent evt)
        {
            _handler(evt);
        }
    }

    private class TestMcpServerListener : AbstractNacosMcpServerListener
    {
        private readonly Action<NacosMcpServerEvent> _handler;

        public TestMcpServerListener(Action<NacosMcpServerEvent> handler)
        {
            _handler = handler;
        }

        public override void OnEvent(NacosMcpServerEvent evt)
        {
            _handler(evt);
        }
    }
}
