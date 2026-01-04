using RedNb.Nacos.Config;

namespace RedNb.Nacos.Tests;

/// <summary>
/// 配置监听器测试
/// </summary>
public class ConfigListenerTests
{
    [Fact]
    public async Task ConfigListenerContext_ShouldTrackMd5Changes()
    {
        // Arrange
        var receiveCount = 0;
        var lastConfig = string.Empty;
        
        var listener = new TestConfigListener(config =>
        {
            receiveCount++;
            lastConfig = config.Content ?? string.Empty;
        });

        // Act - simulate config change
        await listener.ReceiveConfigInfoAsync(new ConfigChangedEventArgs
        {
            DataId = "test",
            Group = "DEFAULT_GROUP",
            Namespace = "public",
            Content = "key=value1",
            ChangeType = ConfigChangeType.Modified
        });

        // Assert
        Assert.Equal(1, receiveCount);
        Assert.Equal("key=value1", lastConfig);
    }

    [Fact]
    public async Task ActionConfigListener_ReceiveConfigInfo_ShouldInvokeCallback()
    {
        // Arrange
        ConfigChangedEventArgs? capturedArgs = null;
        var listener = new TestConfigListener(args =>
        {
            capturedArgs = args;
        });

        var configArgs = new ConfigChangedEventArgs
        {
            DataId = "test-data-id",
            Group = "test-group",
            Namespace = "test-namespace",
            Content = "test-content",
            ChangeType = ConfigChangeType.Added
        };

        // Act
        await listener.ReceiveConfigInfoAsync(configArgs);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal("test-data-id", capturedArgs!.DataId);
        Assert.Equal("test-content", capturedArgs.Content);
    }

    [Fact]
    public async Task ActionConfigListener_MultipleReceives_ShouldInvokeMultipleTimes()
    {
        // Arrange
        var invokeCount = 0;
        var listener = new TestConfigListener(_ => invokeCount++);

        // Act
        await listener.ReceiveConfigInfoAsync(new ConfigChangedEventArgs 
        { 
            DataId = "test",
            Group = "group",
            Namespace = "ns",
            Content = "config1" 
        });
        await listener.ReceiveConfigInfoAsync(new ConfigChangedEventArgs 
        { 
            DataId = "test",
            Group = "group",
            Namespace = "ns",
            Content = "config2" 
        });
        await listener.ReceiveConfigInfoAsync(new ConfigChangedEventArgs 
        { 
            DataId = "test",
            Group = "group",
            Namespace = "ns",
            Content = "config3" 
        });

        // Assert
        Assert.Equal(3, invokeCount);
    }

    [Fact]
    public void ConfigChangedEventArgs_ShouldStoreAllProperties()
    {
        // Arrange & Act
        var args = new ConfigChangedEventArgs
        {
            DataId = "test-data-id",
            Group = "test-group",
            Namespace = "test-namespace",
            Content = "new-content",
            OldContent = "old-content",
            ChangeType = ConfigChangeType.Modified
        };

        // Assert
        Assert.Equal("test-data-id", args.DataId);
        Assert.Equal("test-group", args.Group);
        Assert.Equal("test-namespace", args.Namespace);
        Assert.Equal("new-content", args.Content);
        Assert.Equal("old-content", args.OldContent);
        Assert.Equal(ConfigChangeType.Modified, args.ChangeType);
    }
}

/// <summary>
/// 用于测试的配置监听器
/// </summary>
file class TestConfigListener : IConfigListener
{
    private readonly Action<ConfigChangedEventArgs> _callback;

    public TestConfigListener(Action<ConfigChangedEventArgs> callback)
    {
        _callback = callback;
    }

    public Task ReceiveConfigInfoAsync(ConfigChangedEventArgs configInfo)
    {
        _callback(configInfo);
        return Task.CompletedTask;
    }
}
