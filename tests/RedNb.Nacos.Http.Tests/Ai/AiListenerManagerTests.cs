using RedNb.Nacos.Client.Ai;
using RedNb.Nacos.Core.Ai.Listener;
using RedNb.Nacos.Core.Ai.Model.A2a;
using RedNb.Nacos.Core.Ai.Model.Mcp;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Ai;

public class AiListenerManagerTests
{
    private readonly AiListenerManager _manager;

    public AiListenerManagerTests()
    {
        _manager = new AiListenerManager();
    }

    #region MCP Listener Tests

    [Fact]
    public void AddMcpListener_AddsListenerSuccessfully()
    {
        // Arrange
        var listener = new TestMcpServerListener();

        // Act
        _manager.AddMcpListener("mcp-server", "1.0.0", listener);

        // Assert
        Assert.True(_manager.HasMcpListeners("mcp-server", "1.0.0"));
    }

    [Fact]
    public void AddMcpListener_CanAddMultipleListeners()
    {
        // Arrange
        var listener1 = new TestMcpServerListener();
        var listener2 = new TestMcpServerListener();

        // Act
        _manager.AddMcpListener("mcp-server", "1.0.0", listener1);
        _manager.AddMcpListener("mcp-server", "1.0.0", listener2);

        // Assert
        var listeners = _manager.GetMcpListeners("mcp-server", "1.0.0");
        Assert.Equal(2, listeners.Count);
    }

    [Fact]
    public void RemoveMcpListener_RemovesListener()
    {
        // Arrange
        var listener = new TestMcpServerListener();
        _manager.AddMcpListener("mcp-server", "1.0.0", listener);

        // Act
        var shouldUnsubscribe = _manager.RemoveMcpListener("mcp-server", "1.0.0", listener);

        // Assert
        Assert.True(shouldUnsubscribe);
        Assert.False(_manager.HasMcpListeners("mcp-server", "1.0.0"));
    }

    [Fact]
    public void RemoveMcpListener_ReturnsFalseWhenOtherListenersExist()
    {
        // Arrange
        var listener1 = new TestMcpServerListener();
        var listener2 = new TestMcpServerListener();
        _manager.AddMcpListener("mcp-server", "1.0.0", listener1);
        _manager.AddMcpListener("mcp-server", "1.0.0", listener2);

        // Act
        var shouldUnsubscribe = _manager.RemoveMcpListener("mcp-server", "1.0.0", listener1);

        // Assert
        Assert.False(shouldUnsubscribe);
        Assert.True(_manager.HasMcpListeners("mcp-server", "1.0.0"));
    }

    [Fact]
    public void GetMcpListeners_ReturnsEmptyWhenNoListeners()
    {
        // Act
        var listeners = _manager.GetMcpListeners("non-existent", null);

        // Assert
        Assert.Empty(listeners);
    }

    [Fact]
    public void NotifyMcpListeners_NotifiesAllListeners()
    {
        // Arrange
        var listener1 = new TestMcpServerListener();
        var listener2 = new TestMcpServerListener();
        _manager.AddMcpListener("mcp-server", "1.0.0", listener1);
        _manager.AddMcpListener("mcp-server", "1.0.0", listener2);
        
        var serverInfo = new McpServerDetailInfo();

        // Act
        _manager.NotifyMcpListeners("mcp-server", "1.0.0", serverInfo);

        // Assert
        Assert.Single(listener1.ReceivedEvents);
        Assert.Single(listener2.ReceivedEvents);
    }

    [Fact]
    public void NotifyMcpListeners_HandlesListenerExceptions()
    {
        // Arrange
        var failingListener = new FailingMcpServerListener();
        var normalListener = new TestMcpServerListener();
        _manager.AddMcpListener("mcp-server", "1.0.0", failingListener);
        _manager.AddMcpListener("mcp-server", "1.0.0", normalListener);
        
        var serverInfo = new McpServerDetailInfo();

        // Act (should not throw)
        _manager.NotifyMcpListeners("mcp-server", "1.0.0", serverInfo);

        // Assert
        Assert.Single(normalListener.ReceivedEvents);
    }

    #endregion

    #region Agent Card Listener Tests

    [Fact]
    public void AddAgentListener_AddsListenerSuccessfully()
    {
        // Arrange
        var listener = new TestAgentCardListener();

        // Act
        _manager.AddAgentListener("agent", "1.0.0", listener);

        // Assert
        Assert.True(_manager.HasAgentListeners("agent", "1.0.0"));
    }

    [Fact]
    public void AddAgentListener_CanAddMultipleListeners()
    {
        // Arrange
        var listener1 = new TestAgentCardListener();
        var listener2 = new TestAgentCardListener();

        // Act
        _manager.AddAgentListener("agent", "1.0.0", listener1);
        _manager.AddAgentListener("agent", "1.0.0", listener2);

        // Assert
        var listeners = _manager.GetAgentListeners("agent", "1.0.0");
        Assert.Equal(2, listeners.Count);
    }

    [Fact]
    public void RemoveAgentListener_RemovesListener()
    {
        // Arrange
        var listener = new TestAgentCardListener();
        _manager.AddAgentListener("agent", "1.0.0", listener);

        // Act
        var shouldUnsubscribe = _manager.RemoveAgentListener("agent", "1.0.0", listener);

        // Assert
        Assert.True(shouldUnsubscribe);
        Assert.False(_manager.HasAgentListeners("agent", "1.0.0"));
    }

    [Fact]
    public void GetAgentListeners_ReturnsEmptyWhenNoListeners()
    {
        // Act
        var listeners = _manager.GetAgentListeners("non-existent", null);

        // Assert
        Assert.Empty(listeners);
    }

    [Fact]
    public void NotifyAgentListeners_NotifiesAllListeners()
    {
        // Arrange
        var listener1 = new TestAgentCardListener();
        var listener2 = new TestAgentCardListener();
        _manager.AddAgentListener("agent", "1.0.0", listener1);
        _manager.AddAgentListener("agent", "1.0.0", listener2);
        
        var agentCard = new AgentCardDetailInfo();

        // Act
        _manager.NotifyAgentListeners("agent", "1.0.0", agentCard);

        // Assert
        Assert.Single(listener1.ReceivedEvents);
        Assert.Single(listener2.ReceivedEvents);
    }

    #endregion

    #region Subscription Tracking Tests

    [Fact]
    public void GetAllSubscriptions_ReturnsAllSubscriptions()
    {
        // Arrange
        _manager.AddMcpListener("mcp-1", "1.0.0", new TestMcpServerListener());
        _manager.AddMcpListener("mcp-2", null, new TestMcpServerListener());
        _manager.AddAgentListener("agent-1", "2.0.0", new TestAgentCardListener());

        // Act
        var subscriptions = _manager.GetAllSubscriptions();

        // Assert
        Assert.Equal(3, subscriptions.Count);
        Assert.Contains(subscriptions, s => s.Name == "mcp-1" && s.Version == "1.0.0" && s.IsMcp);
        Assert.Contains(subscriptions, s => s.Name == "mcp-2" && s.Version == null && s.IsMcp);
        Assert.Contains(subscriptions, s => s.Name == "agent-1" && s.Version == "2.0.0" && !s.IsMcp);
    }

    [Fact]
    public void Clear_RemovesAllListeners()
    {
        // Arrange
        _manager.AddMcpListener("mcp-1", "1.0.0", new TestMcpServerListener());
        _manager.AddAgentListener("agent-1", "2.0.0", new TestAgentCardListener());

        // Act
        _manager.Clear();

        // Assert
        Assert.Empty(_manager.GetAllSubscriptions());
    }

    #endregion

    #region Key Building Tests

    [Fact]
    public void BuildMcpKey_WithVersion_ReturnsCorrectKey()
    {
        // Act
        var key = AiListenerManager.BuildMcpKey("mcp-server", "1.0.0");

        // Assert
        Assert.Equal("mcp-server@@1.0.0", key);
    }

    [Fact]
    public void BuildMcpKey_WithNullVersion_ReturnsKeyWithEmptyVersion()
    {
        // Act
        var key = AiListenerManager.BuildMcpKey("mcp-server", null);

        // Assert
        Assert.Equal("mcp-server@@", key);
    }

    [Fact]
    public void BuildAgentKey_WithVersion_ReturnsCorrectKey()
    {
        // Act
        var key = AiListenerManager.BuildAgentKey("agent", "2.0.0");

        // Assert
        Assert.Equal("agent@@2.0.0", key);
    }

    #endregion

    #region Test Helpers

    private class TestMcpServerListener : AbstractNacosMcpServerListener
    {
        public List<NacosMcpServerEvent> ReceivedEvents { get; } = new();

        public override void OnEvent(NacosMcpServerEvent evt)
        {
            ReceivedEvents.Add(evt);
        }
    }

    private class FailingMcpServerListener : AbstractNacosMcpServerListener
    {
        public override void OnEvent(NacosMcpServerEvent evt)
        {
            throw new InvalidOperationException("Simulated failure");
        }
    }

    private class TestAgentCardListener : AbstractNacosAgentCardListener
    {
        public List<NacosAgentCardEvent> ReceivedEvents { get; } = new();

        public override void OnEvent(NacosAgentCardEvent evt)
        {
            ReceivedEvents.Add(evt);
        }
    }

    #endregion
}
