using Moq;
using Moq.Protected;
using RedNb.Nacos.Client.Ai;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Ai;
using RedNb.Nacos.Core.Ai.Listener;
using RedNb.Nacos.Core.Ai.Model.A2a;
using RedNb.Nacos.Core.Ai.Model.Mcp;
using System.Net;
using System.Text.Json;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Ai;

public class NacosAiServiceTests : IAsyncDisposable
{
    private readonly NacosClientOptions _options;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly JsonSerializerOptions _jsonOptions;
    private NacosAiService? _aiService;

    public NacosAiServiceTests()
    {
        _options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848",
            Namespace = "test-namespace"
        };
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_aiService != null)
        {
            await _aiService.DisposeAsync();
        }
    }

    #region Factory Tests

    [Fact]
    public void CreateAiService_WithOptions_ReturnsInstance()
    {
        // Arrange
        var factory = new RedNb.Nacos.Client.NacosFactory();
        var options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848"
        };

        // Act
        var service = factory.CreateAiService(options);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<NacosAiService>(service);
    }

    [Fact]
    public void CreateAiService_WithServerAddr_ReturnsInstance()
    {
        // Arrange
        var factory = new RedNb.Nacos.Client.NacosFactory();

        // Act
        var service = factory.CreateAiService("localhost:8848");

        // Assert
        Assert.NotNull(service);
        Assert.IsType<NacosAiService>(service);
    }

    [Fact]
    public void CreateAiServiceStatic_WithOptions_ReturnsInstance()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848"
        };

        // Act
        var service = RedNb.Nacos.Client.NacosFactory.CreateAiServiceStatic(options);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<NacosAiService>(service);
    }

    #endregion

    #region MCP Server Operation Tests

    [Fact]
    public async Task GetMcpServerAsync_WithEmptyName_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.GetMcpServerAsync(string.Empty));
        Assert.Contains("mcpName is required", ex.Message);
    }

    [Fact]
    public async Task ReleaseMcpServerAsync_WithNullSpec_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.ReleaseMcpServerAsync(null!, null));
        Assert.Contains("serverSpecification is required", ex.Message);
    }

    [Fact]
    public async Task ReleaseMcpServerAsync_WithEmptyName_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);
        var serverSpec = new McpServerBasicInfo { Name = "" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.ReleaseMcpServerAsync(serverSpec, null));
        Assert.Contains("serverSpecification.Name is required", ex.Message);
    }

    [Fact]
    public async Task ReleaseMcpServerAsync_WithMissingVersion_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);
        var serverSpec = new McpServerBasicInfo { Name = "test-mcp" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.ReleaseMcpServerAsync(serverSpec, null));
        Assert.Contains("serverSpecification.VersionDetail.Version is required", ex.Message);
    }

    [Fact]
    public async Task RegisterMcpServerEndpointAsync_WithEmptyName_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.RegisterMcpServerEndpointAsync("", "127.0.0.1", 8080));
        Assert.Contains("mcpName is required", ex.Message);
    }

    [Fact]
    public async Task RegisterMcpServerEndpointAsync_WithInvalidPort_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.RegisterMcpServerEndpointAsync("test-mcp", "127.0.0.1", 0));
        Assert.Contains("port must be between", ex.Message);
    }

    [Fact]
    public async Task DeregisterMcpServerEndpointAsync_WithEmptyAddress_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.DeregisterMcpServerEndpointAsync("test-mcp", "", 8080));
        Assert.Contains("address is required", ex.Message);
    }

    [Fact]
    public async Task SubscribeMcpServerAsync_WithNullListener_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.SubscribeMcpServerAsync("test-mcp", null!));
        Assert.Contains("listener is required", ex.Message);
    }

    #endregion

    #region Agent Card Operation Tests

    [Fact]
    public async Task GetAgentCardAsync_WithEmptyName_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.GetAgentCardAsync(string.Empty));
        Assert.Contains("agentName is required", ex.Message);
    }

    [Fact]
    public async Task ReleaseAgentCardAsync_WithNullAgentCard_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.ReleaseAgentCardAsync(null!));
        Assert.Contains("agentCard is required", ex.Message);
    }

    [Fact]
    public async Task ReleaseAgentCardAsync_WithEmptyName_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);
        var agentCard = new AgentCard { Name = "" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.ReleaseAgentCardAsync(agentCard));
        Assert.Contains("agentCard.Name is required", ex.Message);
    }

    [Fact]
    public async Task ReleaseAgentCardAsync_WithEmptyVersion_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);
        var agentCard = new AgentCard { Name = "test-agent", Version = "" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.ReleaseAgentCardAsync(agentCard));
        Assert.Contains("agentCard.Version is required", ex.Message);
    }

    [Fact]
    public async Task ReleaseAgentCardAsync_WithEmptyProtocolVersion_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);
        var agentCard = new AgentCard 
        { 
            Name = "test-agent", 
            Version = "1.0.0", 
            ProtocolVersion = "" 
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.ReleaseAgentCardAsync(agentCard));
        Assert.Contains("agentCard.ProtocolVersion is required", ex.Message);
    }

    #endregion

    #region Agent Endpoint Operation Tests

    [Fact]
    public async Task RegisterAgentEndpointAsync_WithEmptyName_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);
        var endpoint = new AgentEndpoint { Version = "1.0.0", Address = "127.0.0.1", Port = 8080 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.RegisterAgentEndpointAsync("", endpoint));
        Assert.Contains("agentName is required", ex.Message);
    }

    [Fact]
    public async Task RegisterAgentEndpointAsync_WithNullEndpoint_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.RegisterAgentEndpointAsync("test-agent", (AgentEndpoint)null!));
        Assert.Contains("endpoint is required", ex.Message);
    }

    [Fact]
    public async Task RegisterAgentEndpointAsync_WithEmptyVersion_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);
        var endpoint = new AgentEndpoint { Version = "", Address = "127.0.0.1", Port = 8080 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.RegisterAgentEndpointAsync("test-agent", endpoint));
        Assert.Contains("endpoint.Version is required", ex.Message);
    }

    [Fact]
    public async Task RegisterAgentEndpointsAsync_WithEmptyList_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.RegisterAgentEndpointsAsync("test-agent", new List<AgentEndpoint>()));
        Assert.Contains("endpoints cannot be empty", ex.Message);
    }

    [Fact]
    public async Task RegisterAgentEndpointsAsync_WithDifferentVersions_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);
        var endpoints = new List<AgentEndpoint>
        {
            new() { Version = "1.0.0", Address = "127.0.0.1", Port = 8080 },
            new() { Version = "2.0.0", Address = "127.0.0.1", Port = 8081 }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.RegisterAgentEndpointsAsync("test-agent", endpoints));
        Assert.Contains("must have the same version", ex.Message);
    }

    [Fact]
    public async Task DeregisterAgentEndpointAsync_WithInvalidPort_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.DeregisterAgentEndpointAsync("test-agent", "1.0.0", "127.0.0.1", 99999));
        Assert.Contains("port must be between", ex.Message);
    }

    #endregion

    #region Agent Card Subscription Tests

    [Fact]
    public async Task SubscribeAgentCardAsync_WithEmptyName_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);
        var listener = new TestAgentCardListener();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.SubscribeAgentCardAsync("", listener));
        Assert.Contains("agentName is required", ex.Message);
    }

    [Fact]
    public async Task SubscribeAgentCardAsync_WithNullListener_ThrowsException()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NacosException>(() => 
            _aiService.SubscribeAgentCardAsync("test-agent", null!));
        Assert.Contains("listener is required", ex.Message);
    }

    [Fact]
    public async Task UnsubscribeAgentCardAsync_WithNullListener_DoesNotThrow()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert (should not throw)
        await _aiService.UnsubscribeAgentCardAsync("test-agent", null!);
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public async Task ShutdownAsync_DisposesResources()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act
        await _aiService.ShutdownAsync();

        // Assert - disposing again should not throw
        await _aiService.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        _aiService = new NacosAiService(_options);

        // Act & Assert (should not throw)
        await _aiService.DisposeAsync();
        await _aiService.DisposeAsync();
    }

    #endregion

    #region Test Helpers

    private class TestAgentCardListener : AbstractNacosAgentCardListener
    {
        public List<NacosAgentCardEvent> ReceivedEvents { get; } = new();

        public override void OnEvent(NacosAgentCardEvent evt)
        {
            ReceivedEvents.Add(evt);
        }
    }

    private class TestMcpServerListener : AbstractNacosMcpServerListener
    {
        public List<NacosMcpServerEvent> ReceivedEvents { get; } = new();

        public override void OnEvent(NacosMcpServerEvent evt)
        {
            ReceivedEvents.Add(evt);
        }
    }

    #endregion
}
