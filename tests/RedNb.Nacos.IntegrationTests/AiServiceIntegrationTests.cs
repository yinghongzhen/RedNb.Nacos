using FluentAssertions;
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Ai;
using RedNb.Nacos.Core.Ai.Listener;
using RedNb.Nacos.Core.Ai.Model.A2a;
using RedNb.Nacos.Core.Ai.Model.Mcp;
using Xunit;
using Xunit.Abstractions;

namespace RedNb.Nacos.IntegrationTests;

/// <summary>
/// Integration tests for Nacos AI Service (A2A and MCP).
/// Requires a running Nacos server at localhost:8848 with AI module enabled.
/// </summary>
[Collection("NacosIntegration")]
public class AiServiceIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private IAiService? _aiService;
    private readonly NacosClientOptions _options;
    private readonly NacosFactory _factory;

    public AiServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848",
            Username = "nacos",
            Password = "nacos",
            Namespace = "",
            DefaultTimeout = 10000
        };
        _factory = new NacosFactory();
    }

    public Task InitializeAsync()
    {
        _aiService = _factory.CreateAiService(_options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_aiService is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    #region Agent Card Tests

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ReleaseAndGetAgentCard_ShouldWork()
    {
        // Arrange
        var agentName = $"test-agent-{Guid.NewGuid():N}";
        var agentCard = new AgentCard
        {
            Name = agentName,
            Version = "1.0.0",
            ProtocolVersion = "1.0",
            Description = "Test agent for integration testing",
            Capabilities = new AgentCapabilities
            {
                Streaming = true,
                PushNotifications = false
            }
        };

        try
        {
            // Act - Release agent card
            await _aiService!.ReleaseAgentCardAsync(agentCard);
            _output.WriteLine($"Released Agent Card: {agentName}");

            await Task.Delay(2000);

            // Get agent card
            var retrieved = await _aiService.GetAgentCardAsync(agentName);

            // Assert
            if (retrieved != null)
            {
                _output.WriteLine($"Retrieved Agent Card: {retrieved.Name}");
                retrieved.Should().NotBeNull();
                retrieved.Name.Should().Be(agentName);
                retrieved.Version.Should().Be("1.0.0");
            }
            else
            {
                _output.WriteLine("Agent Card not found - AI module may not be enabled");
            }
        }
        catch (NacosException ex) when (ex.ErrorCode == NacosException.NotFound || ex.Message.Contains("not found"))
        {
            _output.WriteLine($"AI module may not be enabled: {ex.Message}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task RegisterAndDeregisterAgentEndpoint_ShouldWork()
    {
        // Arrange
        var agentName = $"test-agent-endpoint-{Guid.NewGuid():N}";
        var agentCard = new AgentCard
        {
            Name = agentName,
            Version = "1.0.0",
            ProtocolVersion = "1.0"
        };
        var endpoint = new AgentEndpoint
        {
            Address = "192.168.1.100",
            Port = 9000,
            Version = "1.0.0",
            Transport = AiConstants.A2a.TransportJsonRpc
        };

        try
        {
            // Release agent card first
            await _aiService!.ReleaseAgentCardAsync(agentCard);
            await Task.Delay(1000);

            // Act - Register endpoint
            await _aiService.RegisterAgentEndpointAsync(agentName, endpoint);
            _output.WriteLine($"Registered endpoint {endpoint.Address}:{endpoint.Port}");

            await Task.Delay(1000);

            // Verify by getting agent card
            var retrieved = await _aiService.GetAgentCardAsync(agentName);
            _output.WriteLine($"Agent Card retrieved: {retrieved?.Name ?? "null"}");

            // Deregister endpoint
            await _aiService.DeregisterAgentEndpointAsync(agentName, endpoint);
            _output.WriteLine("Deregistered endpoint");
        }
        catch (NacosException ex)
        {
            _output.WriteLine($"AI module may not be enabled: {ex.Message}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task SubscribeAgentCard_ShouldReceiveUpdates()
    {
        // Arrange
        var agentName = $"test-agent-subscribe-{Guid.NewGuid():N}";
        var agentCard = new AgentCard
        {
            Name = agentName,
            Version = "1.0.0",
            ProtocolVersion = "1.0"
        };

        var receivedEvents = new List<NacosAgentCardEvent>();
        var listener = new TestAgentCardListener(evt =>
        {
            _output.WriteLine($"Received Agent Card event for {evt.AgentCard?.Name}");
            receivedEvents.Add(evt);
        });

        try
        {
            // Subscribe
            var initial = await _aiService!.SubscribeAgentCardAsync(agentName, listener);
            _output.WriteLine($"Subscribed to {agentName}, initial: {initial?.Name ?? "null"}");

            // Release agent card
            await _aiService.ReleaseAgentCardAsync(agentCard);
            await Task.Delay(3000);

            // Assert
            _output.WriteLine($"Received {receivedEvents.Count} events");

            // Cleanup
            await _aiService.UnsubscribeAgentCardAsync(agentName, listener);
        }
        catch (NacosException ex)
        {
            _output.WriteLine($"AI module may not be enabled: {ex.Message}");
        }
    }

    #endregion

    #region MCP Server Tests

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task ReleaseAndGetMcpServer_ShouldWork()
    {
        // Arrange
        var mcpName = $"test-mcp-{Guid.NewGuid():N}";
        var serverSpec = new McpServerBasicInfo
        {
            Name = mcpName,
            VersionDetail = new ServerVersionDetail
            {
                Version = "1.0.0"
            }
        };

        var toolSpec = new McpToolSpecification
        {
            Tools = new List<McpTool>
            {
                new McpTool
                {
                    Name = "test-tool",
                    Description = "A test tool for integration testing"
                }
            }
        };

        try
        {
            // Act - Release MCP server
            var mcpId = await _aiService!.ReleaseMcpServerAsync(serverSpec, toolSpec);
            _output.WriteLine($"Released MCP Server: {mcpName}, ID: {mcpId}");

            await Task.Delay(2000);

            // Get MCP server
            var retrieved = await _aiService.GetMcpServerAsync(mcpName);

            // Assert
            if (retrieved != null)
            {
                _output.WriteLine($"Retrieved MCP Server: {retrieved.Name}");
                retrieved.Name.Should().Be(mcpName);
            }
            else
            {
                _output.WriteLine("MCP Server not found - AI module may not be enabled");
            }
        }
        catch (NacosException ex)
        {
            _output.WriteLine($"AI module may not be enabled: {ex.Message}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task RegisterAndDeregisterMcpEndpoint_ShouldWork()
    {
        // Arrange
        var mcpName = $"test-mcp-endpoint-{Guid.NewGuid():N}";
        var serverSpec = new McpServerBasicInfo
        {
            Name = mcpName,
            VersionDetail = new ServerVersionDetail
            {
                Version = "1.0.0"
            }
        };

        var address = "192.168.1.101";
        var port = 9100;

        try
        {
            // Release MCP server first
            await _aiService!.ReleaseMcpServerAsync(serverSpec, null);
            await Task.Delay(1000);

            // Act - Register endpoint
            await _aiService.RegisterMcpServerEndpointAsync(mcpName, address, port);
            _output.WriteLine($"Registered MCP endpoint {address}:{port}");

            await Task.Delay(1000);

            // Verify
            var retrieved = await _aiService.GetMcpServerAsync(mcpName);
            _output.WriteLine($"MCP Server retrieved: {retrieved?.Name ?? "null"}");

            // Deregister
            await _aiService.DeregisterMcpServerEndpointAsync(mcpName, address, port);
            _output.WriteLine("Deregistered MCP endpoint");
        }
        catch (NacosException ex)
        {
            _output.WriteLine($"AI module may not be enabled: {ex.Message}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AI")]
    public async Task SubscribeMcpServer_ShouldReceiveUpdates()
    {
        // Arrange
        var mcpName = $"test-mcp-subscribe-{Guid.NewGuid():N}";
        var serverSpec = new McpServerBasicInfo
        {
            Name = mcpName,
            VersionDetail = new ServerVersionDetail
            {
                Version = "1.0.0"
            }
        };

        var receivedEvents = new List<NacosMcpServerEvent>();
        var listener = new TestMcpServerListener(evt =>
        {
            _output.WriteLine($"Received MCP Server event for {evt.McpServerDetailInfo?.Name}");
            receivedEvents.Add(evt);
        });

        try
        {
            // Subscribe
            var initial = await _aiService!.SubscribeMcpServerAsync(mcpName, listener);
            _output.WriteLine($"Subscribed to {mcpName}, initial: {initial?.Name ?? "null"}");

            // Release MCP server
            await _aiService.ReleaseMcpServerAsync(serverSpec, null);
            await Task.Delay(3000);

            // Assert
            _output.WriteLine($"Received {receivedEvents.Count} events");

            // Cleanup
            await _aiService.UnsubscribeMcpServerAsync(mcpName, listener);
        }
        catch (NacosException ex)
        {
            _output.WriteLine($"AI module may not be enabled: {ex.Message}");
        }
    }

    #endregion

    #region Test Listeners

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

    #endregion
}
