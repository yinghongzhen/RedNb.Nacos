using RedNb.Nacos.Core.Ai;
using RedNb.Nacos.Core.Ai.Model.A2a;
using RedNb.Nacos.Core.Ai.Model.Mcp;
using System.Text.Json;
using Xunit;

namespace RedNb.Nacos.Tests.Ai;

public class AiModelTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    #region A2A Model Tests

    [Fact]
    public void AgentCard_SerializesCorrectly()
    {
        // Arrange
        var agentCard = new AgentCard
        {
            Name = "test-agent",
            Version = "1.0.0",
            ProtocolVersion = "0.1.0",
            Description = "Test agent description",
            Url = "https://agent.example.com"
        };

        // Act
        var json = JsonSerializer.Serialize(agentCard, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AgentCard>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("test-agent", deserialized.Name);
        Assert.Equal("1.0.0", deserialized.Version);
        Assert.Equal("0.1.0", deserialized.ProtocolVersion);
        Assert.Equal("Test agent description", deserialized.Description);
        Assert.Equal("https://agent.example.com", deserialized.Url);
    }

    [Fact]
    public void AgentCard_WithCapabilities_SerializesCorrectly()
    {
        // Arrange
        var agentCard = new AgentCard
        {
            Name = "test-agent",
            Version = "1.0.0",
            ProtocolVersion = "0.1.0",
            Capabilities = new AgentCapabilities
            {
                Streaming = true,
                PushNotifications = true
            }
        };

        // Act
        var json = JsonSerializer.Serialize(agentCard, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AgentCard>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized?.Capabilities);
        Assert.True(deserialized.Capabilities.Streaming);
        Assert.True(deserialized.Capabilities.PushNotifications);
    }

    [Fact]
    public void AgentCard_WithSkills_SerializesCorrectly()
    {
        // Arrange
        var agentCard = new AgentCard
        {
            Name = "test-agent",
            Version = "1.0.0",
            ProtocolVersion = "0.1.0",
            Skills = new List<AgentSkill>
            {
                new()
                {
                    Id = "skill-1",
                    Name = "Test Skill",
                    Description = "A test skill"
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(agentCard, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AgentCard>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized?.Skills);
        Assert.Single(deserialized.Skills);
        Assert.Equal("skill-1", deserialized.Skills[0].Id);
    }

    [Fact]
    public void AgentEndpoint_SerializesCorrectly()
    {
        // Arrange
        var endpoint = new AgentEndpoint
        {
            Address = "127.0.0.1",
            Port = 8080,
            Version = "1.0.0",
            Transport = "JSONRPC",
            Path = "/agent",
            SupportTls = true
        };

        // Act
        var json = JsonSerializer.Serialize(endpoint, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AgentEndpoint>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("127.0.0.1", deserialized.Address);
        Assert.Equal(8080, deserialized.Port);
        Assert.Equal("1.0.0", deserialized.Version);
        Assert.Equal("JSONRPC", deserialized.Transport);
        Assert.Equal("/agent", deserialized.Path);
        Assert.True(deserialized.SupportTls);
    }

    [Fact]
    public void AgentProvider_SerializesCorrectly()
    {
        // Arrange
        var provider = new AgentProvider
        {
            Organization = "Test Org",
            Url = "https://example.com"
        };

        // Act
        var json = JsonSerializer.Serialize(provider, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AgentProvider>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("Test Org", deserialized.Organization);
        Assert.Equal("https://example.com", deserialized.Url);
    }

    [Fact]
    public void AgentSkill_WithTags_SerializesCorrectly()
    {
        // Arrange
        var skill = new AgentSkill
        {
            Id = "search-skill",
            Name = "Search",
            Description = "Performs web search",
            Tags = new List<string> { "search", "web", "query" },
            Examples = new List<string> { "Search for X", "Find Y" }
        };

        // Act
        var json = JsonSerializer.Serialize(skill, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AgentSkill>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized?.Tags);
        Assert.Equal(3, deserialized.Tags.Count);
        Assert.Contains("search", deserialized.Tags);
        Assert.NotNull(deserialized.Examples);
        Assert.Equal(2, deserialized.Examples.Count);
    }

    [Fact]
    public void AgentAuthentication_SerializesCorrectly()
    {
        // Arrange
        var auth = new AgentAuthentication
        {
            Schemes = new List<string> { "apiKey", "bearer" },
            Credentials = "test-credential"
        };

        // Act
        var json = JsonSerializer.Serialize(auth, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AgentAuthentication>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized?.Schemes);
        Assert.Equal(2, deserialized.Schemes.Count);
        Assert.Equal("test-credential", deserialized.Credentials);
    }

    #endregion

    #region MCP Model Tests

    [Fact]
    public void McpServerBasicInfo_SerializesCorrectly()
    {
        // Arrange
        var serverInfo = new McpServerBasicInfo
        {
            Name = "test-mcp",
            Description = "Test MCP server",
            VersionDetail = new ServerVersionDetail
            {
                Version = "1.0.0",
                IsLatest = true,
                ReleaseDate = "2024-01-01T00:00:00Z"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(serverInfo, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<McpServerBasicInfo>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("test-mcp", deserialized.Name);
        Assert.NotNull(deserialized.VersionDetail);
        Assert.Equal("1.0.0", deserialized.VersionDetail.Version);
        Assert.True(deserialized.VersionDetail.IsLatest);
    }

    [Fact]
    public void McpServerDetailInfo_SerializesCorrectly()
    {
        // Arrange
        var detailInfo = new McpServerDetailInfo
        {
            Name = "test-mcp",
            Description = "Test MCP server",
            BackendEndpoints = new List<McpEndpointInfo>
            {
                new()
                {
                    Address = "mcp.example.com",
                    Port = 443,
                    Protocol = "https"
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(detailInfo, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<McpServerDetailInfo>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized?.BackendEndpoints);
        Assert.Single(deserialized.BackendEndpoints);
        Assert.Equal("mcp.example.com", deserialized.BackendEndpoints[0].Address);
    }

    [Fact]
    public void McpTool_SerializesCorrectly()
    {
        // Arrange
        var tool = new McpTool
        {
            Name = "search_tool",
            Description = "A search tool",
            InputSchema = new Dictionary<string, object>
            {
                { "type", "object" },
                { "properties", new Dictionary<string, object>
                    {
                        { "query", new Dictionary<string, string> { { "type", "string" } } }
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(tool, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<McpTool>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("search_tool", deserialized.Name);
        Assert.NotNull(deserialized.InputSchema);
        Assert.True(deserialized.InputSchema.ContainsKey("type"));
    }

    [Fact]
    public void McpToolSpecification_SerializesCorrectly()
    {
        // Arrange
        var toolSpec = new McpToolSpecification
        {
            Tools = new List<McpTool>
            {
                new() { Name = "tool1", Description = "First tool" },
                new() { Name = "tool2", Description = "Second tool" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(toolSpec, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<McpToolSpecification>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized?.Tools);
        Assert.Equal(2, deserialized.Tools.Count);
    }

    [Fact]
    public void McpEndpointSpec_WithDirectType_SerializesCorrectly()
    {
        // Arrange
        var endpointSpec = new McpEndpointSpec
        {
            Type = AiConstants.Mcp.EndpointTypeDirect,
            Data = new Dictionary<string, string>
            {
                { "address", "localhost" },
                { "port", "8080" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(endpointSpec, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<McpEndpointSpec>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(AiConstants.Mcp.EndpointTypeDirect, deserialized.Type);
        Assert.True(deserialized.Data.ContainsKey("address"));
    }

    [Fact]
    public void McpCapability_SerializesCorrectly()
    {
        // Arrange
        var capability = new McpCapability
        {
            Name = "streaming",
            Description = "Supports streaming responses"
        };

        // Act
        var json = JsonSerializer.Serialize(capability, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<McpCapability>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("streaming", deserialized.Name);
        Assert.Equal("Supports streaming responses", deserialized.Description);
    }

    [Fact]
    public void Icon_SerializesCorrectly()
    {
        // Arrange
        var icon = new Icon
        {
            Url = "https://example.com/icon.png",
            Type = "image/png"
        };

        // Act
        var json = JsonSerializer.Serialize(icon, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<Icon>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("https://example.com/icon.png", deserialized.Url);
        Assert.Equal("image/png", deserialized.Type);
    }

    [Fact]
    public void Package_SerializesCorrectly()
    {
        // Arrange
        var package = new Package
        {
            RegistryType = "npm",
            RegistryName = "npmjs",
            Name = "@example/mcp-server",
            Version = "1.0.0",
            Runtime = "node"
        };

        // Act
        var json = JsonSerializer.Serialize(package, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<Package>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("npm", deserialized.RegistryType);
        Assert.Equal("@example/mcp-server", deserialized.Name);
    }

    #endregion

    #region Constants Tests

    [Fact]
    public void AiConstants_HasCorrectMcpValues()
    {
        Assert.Equal("SSE", AiConstants.Mcp.McpProtocolSse);
        Assert.Equal("STREAMABLE", AiConstants.Mcp.McpProtocolStreamable);
        Assert.Equal("DIRECT", AiConstants.Mcp.McpEndpointTypeDirect);
        Assert.Equal("REF", AiConstants.Mcp.McpEndpointTypeRef);
    }

    [Fact]
    public void AiConstants_HasCorrectA2aValues()
    {
        Assert.Equal("URL", AiConstants.A2a.A2aEndpointTypeUrl);
        Assert.Equal("SERVICE", AiConstants.A2a.A2aEndpointTypeService);
        Assert.Equal("JSONRPC", AiConstants.A2a.TransportJsonRpc);
        Assert.Equal("GRPC", AiConstants.A2a.TransportGrpc);
        Assert.Equal("HTTP+JSON", AiConstants.A2a.TransportHttpJson);
    }

    #endregion
}
