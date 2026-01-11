using RedNb.Nacos.Client.Ai;
using RedNb.Nacos.Core.Ai.Model.A2a;
using RedNb.Nacos.Core.Ai.Model.Mcp;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Ai;

public class AiCacheHolderTests
{
    private readonly AiCacheHolder _cache;

    public AiCacheHolderTests()
    {
        _cache = new AiCacheHolder();
    }

    #region MCP Server Cache Tests

    [Fact]
    public void GetMcpServer_ReturnsNullWhenNotCached()
    {
        // Act
        var result = _cache.GetMcpServer("non-existent", null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UpdateMcpServer_CachesServer()
    {
        // Arrange
        var serverInfo = new McpServerDetailInfo { Name = "test-mcp" };

        // Act
        _cache.UpdateMcpServer("test-mcp", "1.0.0", serverInfo);
        var result = _cache.GetMcpServer("test-mcp", "1.0.0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-mcp", result.Name);
    }

    [Fact]
    public void UpdateMcpServer_WithNull_RemovesFromCache()
    {
        // Arrange
        var serverInfo = new McpServerDetailInfo { Name = "test-mcp" };
        _cache.UpdateMcpServer("test-mcp", "1.0.0", serverInfo);

        // Act
        _cache.UpdateMcpServer("test-mcp", "1.0.0", null);
        var result = _cache.GetMcpServer("test-mcp", "1.0.0");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RemoveMcpServer_RemovesFromCache()
    {
        // Arrange
        var serverInfo = new McpServerDetailInfo { Name = "test-mcp" };
        _cache.UpdateMcpServer("test-mcp", "1.0.0", serverInfo);

        // Act
        _cache.RemoveMcpServer("test-mcp", "1.0.0");
        var result = _cache.GetMcpServer("test-mcp", "1.0.0");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMcpKeys_ReturnsAllKeys()
    {
        // Arrange
        _cache.UpdateMcpServer("mcp-1", "1.0.0", new McpServerDetailInfo());
        _cache.UpdateMcpServer("mcp-2", "2.0.0", new McpServerDetailInfo());

        // Act
        var keys = _cache.GetMcpKeys();

        // Assert
        Assert.Equal(2, keys.Count);
    }

    [Fact]
    public void UpdateMcpServer_OverwritesExisting()
    {
        // Arrange
        var serverInfo1 = new McpServerDetailInfo { Name = "test-mcp", Description = "first" };
        var serverInfo2 = new McpServerDetailInfo { Name = "test-mcp", Description = "second" };

        // Act
        _cache.UpdateMcpServer("test-mcp", "1.0.0", serverInfo1);
        _cache.UpdateMcpServer("test-mcp", "1.0.0", serverInfo2);
        var result = _cache.GetMcpServer("test-mcp", "1.0.0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("second", result.Description);
    }

    [Fact]
    public void GetMcpServer_DifferentVersions_ReturnsDifferentCaches()
    {
        // Arrange
        var serverInfo1 = new McpServerDetailInfo { Name = "mcp", Description = "v1" };
        var serverInfo2 = new McpServerDetailInfo { Name = "mcp", Description = "v2" };
        _cache.UpdateMcpServer("mcp", "1.0.0", serverInfo1);
        _cache.UpdateMcpServer("mcp", "2.0.0", serverInfo2);

        // Act
        var result1 = _cache.GetMcpServer("mcp", "1.0.0");
        var result2 = _cache.GetMcpServer("mcp", "2.0.0");

        // Assert
        Assert.Equal("v1", result1?.Description);
        Assert.Equal("v2", result2?.Description);
    }

    #endregion

    #region Agent Card Cache Tests

    [Fact]
    public void GetAgentCard_ReturnsNullWhenNotCached()
    {
        // Act
        var result = _cache.GetAgentCard("non-existent", null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UpdateAgentCard_CachesCard()
    {
        // Arrange
        var agentCard = new AgentCardDetailInfo { Name = "test-agent" };

        // Act
        _cache.UpdateAgentCard("test-agent", "1.0.0", agentCard);
        var result = _cache.GetAgentCard("test-agent", "1.0.0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-agent", result.Name);
    }

    [Fact]
    public void UpdateAgentCard_WithNull_RemovesFromCache()
    {
        // Arrange
        var agentCard = new AgentCardDetailInfo { Name = "test-agent" };
        _cache.UpdateAgentCard("test-agent", "1.0.0", agentCard);

        // Act
        _cache.UpdateAgentCard("test-agent", "1.0.0", null);
        var result = _cache.GetAgentCard("test-agent", "1.0.0");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RemoveAgentCard_RemovesFromCache()
    {
        // Arrange
        var agentCard = new AgentCardDetailInfo { Name = "test-agent" };
        _cache.UpdateAgentCard("test-agent", "1.0.0", agentCard);

        // Act
        _cache.RemoveAgentCard("test-agent", "1.0.0");
        var result = _cache.GetAgentCard("test-agent", "1.0.0");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAgentKeys_ReturnsAllKeys()
    {
        // Arrange
        _cache.UpdateAgentCard("agent-1", "1.0.0", new AgentCardDetailInfo());
        _cache.UpdateAgentCard("agent-2", "2.0.0", new AgentCardDetailInfo());

        // Act
        var keys = _cache.GetAgentKeys();

        // Assert
        Assert.Equal(2, keys.Count);
    }

    [Fact]
    public void GetAgentCard_NullVersion_CachesSeparately()
    {
        // Arrange
        var agentCard1 = new AgentCardDetailInfo { Name = "agent", Description = "with version" };
        var agentCard2 = new AgentCardDetailInfo { Name = "agent", Description = "latest" };
        _cache.UpdateAgentCard("agent", "1.0.0", agentCard1);
        _cache.UpdateAgentCard("agent", null, agentCard2);

        // Act
        var result1 = _cache.GetAgentCard("agent", "1.0.0");
        var result2 = _cache.GetAgentCard("agent", null);

        // Assert
        Assert.Equal("with version", result1?.Description);
        Assert.Equal("latest", result2?.Description);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllCache()
    {
        // Arrange
        _cache.UpdateMcpServer("mcp-1", "1.0.0", new McpServerDetailInfo());
        _cache.UpdateAgentCard("agent-1", "1.0.0", new AgentCardDetailInfo());

        // Act
        _cache.Clear();

        // Assert
        Assert.Empty(_cache.GetMcpKeys());
        Assert.Empty(_cache.GetAgentKeys());
    }

    #endregion
}
