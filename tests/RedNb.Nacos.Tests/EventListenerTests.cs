using RedNb.Nacos.Naming;

namespace RedNb.Nacos.Tests;

/// <summary>
/// 命名服务事件监听器测试
/// </summary>
public class EventListenerTests
{
    [Fact]
    public void NamingChangeEvent_ShouldStoreChanges()
    {
        // Arrange
        var added = new List<Instance>
        {
            new() { Ip = "192.168.1.1", Port = 8080 }
        };
        var removed = new List<Instance>
        {
            new() { Ip = "192.168.1.2", Port = 8080 }
        };
        var modified = new List<Instance>
        {
            new() { Ip = "192.168.1.3", Port = 8080 }
        };

        // Act
        var changeEvent = new NamingChangeEvent
        {
            ServiceName = "test-service",
            GroupName = "DEFAULT_GROUP",
            Namespace = "public",
            AddedInstances = added,
            RemovedInstances = removed,
            ModifiedInstances = modified
        };

        // Assert
        Assert.Single(changeEvent.AddedInstances);
        Assert.Single(changeEvent.RemovedInstances);
        Assert.Single(changeEvent.ModifiedInstances);
        Assert.Equal("192.168.1.1", changeEvent.AddedInstances[0].Ip);
        Assert.Equal("192.168.1.2", changeEvent.RemovedInstances[0].Ip);
        Assert.Equal("192.168.1.3", changeEvent.ModifiedInstances[0].Ip);
    }

    [Fact]
    public async Task NamingChangeListener_OnChange_ShouldInvokeCallback()
    {
        // Arrange
        NamingChangeEvent? capturedEvent = null;
        var listener = new TestNamingChangeListener(e =>
        {
            capturedEvent = e;
        });

        var changeEvent = new NamingChangeEvent
        {
            ServiceName = "test-service",
            GroupName = "DEFAULT_GROUP",
            Namespace = "public",
            AddedInstances = [new Instance { Ip = "192.168.1.1", Port = 8080 }]
        };

        // Act
        await listener.OnChangeAsync(changeEvent);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Single(capturedEvent!.AddedInstances);
    }

    [Fact]
    public async Task EventListener_OnEvent_ShouldInvokeCallback()
    {
        // Arrange
        var invokeCount = 0;
        ServiceInfo? capturedInfo = null;
        
        var listener = new TestEventListener(info =>
        {
            invokeCount++;
            capturedInfo = info;
        });

        var serviceInfo = new ServiceInfo
        {
            Name = "test-service",
            GroupName = "DEFAULT_GROUP",
            Hosts = new List<Instance>
            {
                new() { Ip = "192.168.1.1", Port = 8080 }
            }
        };

        // Act
        await listener.OnEventAsync(serviceInfo);

        // Assert
        Assert.Equal(1, invokeCount);
        Assert.NotNull(capturedInfo);
        Assert.Equal("test-service", capturedInfo!.Name);
    }

    [Fact]
    public async Task EventListener_OnEvent_ShouldHandleMultipleCalls()
    {
        // Arrange
        var invokeCount = 0;
        var listener = new TestEventListener(_ => invokeCount++);

        var serviceInfo = new ServiceInfo { Name = "test" };

        // Act
        await listener.OnEventAsync(serviceInfo);
        await listener.OnEventAsync(serviceInfo);
        await listener.OnEventAsync(serviceInfo);

        // Assert
        Assert.Equal(3, invokeCount);
    }

    [Fact]
    public void NamingChangeEvent_HasChanges_ShouldReturnTrueWhenChangesExist()
    {
        // Arrange & Act
        var eventWithChanges = new NamingChangeEvent
        {
            ServiceName = "test",
            GroupName = "group",
            Namespace = "ns",
            AddedInstances = [new Instance { Ip = "1.1.1.1", Port = 80 }]
        };

        var eventWithoutChanges = new NamingChangeEvent
        {
            ServiceName = "test",
            GroupName = "group",
            Namespace = "ns"
        };

        // Assert
        Assert.True(eventWithChanges.HasChanges);
        Assert.False(eventWithoutChanges.HasChanges);
    }
}

/// <summary>
/// 用于测试的事件监听器
/// </summary>
file class TestEventListener : IEventListener
{
    private readonly Action<ServiceInfo> _callback;

    public TestEventListener(Action<ServiceInfo> callback)
    {
        _callback = callback;
    }

    public Task OnEventAsync(ServiceInfo serviceInfo)
    {
        _callback(serviceInfo);
        return Task.CompletedTask;
    }
}

/// <summary>
/// 用于测试的命名变更监听器
/// </summary>
file class TestNamingChangeListener : INamingChangeListener
{
    private readonly Action<NamingChangeEvent> _callback;

    public TestNamingChangeListener(Action<NamingChangeEvent> callback)
    {
        _callback = callback;
    }

    public Task OnChangeAsync(NamingChangeEvent changeEvent)
    {
        _callback(changeEvent);
        return Task.CompletedTask;
    }
}
