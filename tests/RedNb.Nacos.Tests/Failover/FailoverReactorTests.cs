using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Failover;
using Xunit;

namespace RedNb.Nacos.Tests.Failover;

/// <summary>
/// Unit tests for FailoverReactor class.
/// </summary>
public class FailoverReactorTests : IDisposable
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly FailoverReactor<ServiceInfo> _reactor;

    public FailoverReactorTests()
    {
        _mockLogger = new Mock<ILogger>();
        _reactor = new FailoverReactor<ServiceInfo>(_mockLogger.Object);
    }

    public void Dispose()
    {
        _reactor.Dispose();
    }

    #region IsFailoverEnabled Tests

    [Fact]
    public void IsFailoverEnabled_Default_ShouldBeFalse()
    {
        // Assert
        _reactor.IsFailoverEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsFailoverEnabled_AfterEnable_ShouldBeTrue()
    {
        // Act
        _reactor.SetFailoverSwitch(true);

        // Assert
        _reactor.IsFailoverEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsFailoverEnabled_AfterDisable_ShouldBeFalse()
    {
        // Arrange
        _reactor.SetFailoverSwitch(true);

        // Act
        _reactor.SetFailoverSwitch(false);

        // Assert
        _reactor.IsFailoverEnabled.Should().BeFalse();
    }

    #endregion

    #region SetFailoverSwitch Tests

    [Fact]
    public void SetFailoverSwitch_Enable_ShouldUpdateState()
    {
        // Act
        _reactor.SetFailoverSwitch(true);

        // Assert
        _reactor.IsFailoverEnabled.Should().BeTrue();
    }

    [Fact]
    public void SetFailoverSwitch_MultipleCalls_ShouldHandleCorrectly()
    {
        // Act & Assert
        _reactor.SetFailoverSwitch(true);
        _reactor.IsFailoverEnabled.Should().BeTrue();

        _reactor.SetFailoverSwitch(true); // Same state
        _reactor.IsFailoverEnabled.Should().BeTrue();

        _reactor.SetFailoverSwitch(false);
        _reactor.IsFailoverEnabled.Should().BeFalse();

        _reactor.SetFailoverSwitch(false); // Same state
        _reactor.IsFailoverEnabled.Should().BeFalse();
    }

    #endregion

    #region GetFailoverData Tests

    [Fact]
    public void GetFailoverData_WhenDisabled_ShouldReturnNull()
    {
        // Arrange
        var serviceInfo = new ServiceInfo { Name = "test-service" };
        _reactor.SetFailoverData("key", FailoverData<ServiceInfo>.CreateForNaming("key", serviceInfo));

        // Act (failover is disabled by default)
        var result = _reactor.GetFailoverData("key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetFailoverData_WhenEnabled_ShouldReturnData()
    {
        // Arrange
        var serviceInfo = new ServiceInfo { Name = "test-service" };
        _reactor.SetFailoverData("key", FailoverData<ServiceInfo>.CreateForNaming("key", serviceInfo));
        _reactor.SetFailoverSwitch(true);

        // Act
        var result = _reactor.GetFailoverData("key");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("test-service");
    }

    [Fact]
    public void GetFailoverData_NonExistingKey_ShouldReturnNull()
    {
        // Arrange
        _reactor.SetFailoverSwitch(true);

        // Act
        var result = _reactor.GetFailoverData("non-existing-key");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region TryGetFailoverData Tests

    [Fact]
    public void TryGetFailoverData_WhenDisabled_ShouldReturnFalse()
    {
        // Arrange
        var serviceInfo = new ServiceInfo { Name = "test-service" };
        _reactor.SetFailoverData("key", FailoverData<ServiceInfo>.CreateForNaming("key", serviceInfo));

        // Act
        var result = _reactor.TryGetFailoverData("key", out var data);

        // Assert
        result.Should().BeFalse();
        data.Should().BeNull();
    }

    [Fact]
    public void TryGetFailoverData_WhenEnabledAndExists_ShouldReturnTrueAndData()
    {
        // Arrange
        var serviceInfo = new ServiceInfo { Name = "test-service" };
        _reactor.SetFailoverData("key", FailoverData<ServiceInfo>.CreateForNaming("key", serviceInfo));
        _reactor.SetFailoverSwitch(true);

        // Act
        var result = _reactor.TryGetFailoverData("key", out var data);

        // Assert
        result.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Name.Should().Be("test-service");
    }

    [Fact]
    public void TryGetFailoverData_WhenEnabledButNotExists_ShouldReturnFalse()
    {
        // Arrange
        _reactor.SetFailoverSwitch(true);

        // Act
        var result = _reactor.TryGetFailoverData("non-existing", out var data);

        // Assert
        result.Should().BeFalse();
        data.Should().BeNull();
    }

    #endregion

    #region SetFailoverData Tests

    [Fact]
    public void SetFailoverData_NewKey_ShouldAddData()
    {
        // Arrange
        var serviceInfo = new ServiceInfo { Name = "new-service" };
        _reactor.SetFailoverSwitch(true);

        // Act
        _reactor.SetFailoverData("new-key", FailoverData<ServiceInfo>.CreateForNaming("new-key", serviceInfo));

        // Assert
        var result = _reactor.GetFailoverData("new-key");
        result.Should().NotBeNull();
        result!.Name.Should().Be("new-service");
    }

    [Fact]
    public void SetFailoverData_ExistingKey_ShouldUpdateData()
    {
        // Arrange
        var originalService = new ServiceInfo { Name = "original-service" };
        var updatedService = new ServiceInfo { Name = "updated-service" };
        _reactor.SetFailoverSwitch(true);
        _reactor.SetFailoverData("key", FailoverData<ServiceInfo>.CreateForNaming("key", originalService));

        // Act
        _reactor.SetFailoverData("key", FailoverData<ServiceInfo>.CreateForNaming("key", updatedService));

        // Assert
        var result = _reactor.GetFailoverData("key");
        result!.Name.Should().Be("updated-service");
    }

    [Fact]
    public void SetFailoverData_ShouldRaiseDataChangedEvent()
    {
        // Arrange
        var eventRaised = false;
        string? changedKey = null;
        _reactor.DataChanged += (sender, args) =>
        {
            eventRaised = true;
            changedKey = args.Key;
        };
        var serviceInfo = new ServiceInfo { Name = "test-service" };

        // Act
        _reactor.SetFailoverData("event-key", FailoverData<ServiceInfo>.CreateForNaming("event-key", serviceInfo));

        // Assert
        eventRaised.Should().BeTrue();
        changedKey.Should().Be("event-key");
    }

    #endregion

    #region RemoveFailoverData Tests

    [Fact]
    public void RemoveFailoverData_ExistingKey_ShouldRemoveData()
    {
        // Arrange
        var serviceInfo = new ServiceInfo { Name = "test-service" };
        _reactor.SetFailoverSwitch(true);
        _reactor.SetFailoverData("key", FailoverData<ServiceInfo>.CreateForNaming("key", serviceInfo));

        // Act
        _reactor.RemoveFailoverData("key");

        // Assert
        var result = _reactor.GetFailoverData("key");
        result.Should().BeNull();
    }

    [Fact]
    public void RemoveFailoverData_NonExistingKey_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => _reactor.RemoveFailoverData("non-existing");
        action.Should().NotThrow();
    }

    [Fact]
    public void RemoveFailoverData_ShouldRaiseDataChangedEvent()
    {
        // Arrange
        var serviceInfo = new ServiceInfo { Name = "test-service" };
        _reactor.SetFailoverData("key", FailoverData<ServiceInfo>.CreateForNaming("key", serviceInfo));
        
        FailoverDataChangedEventArgs<ServiceInfo>? eventArgs = null;
        _reactor.DataChanged += (sender, args) => eventArgs = args;

        // Act
        _reactor.RemoveFailoverData("key");

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.Key.Should().Be("key");
        eventArgs.OldData.Should().NotBeNull();
        eventArgs.NewData.Should().BeNull();
    }

    #endregion

    #region StartAsync / StopAsync Tests

    [Fact]
    public async Task StartAsync_WithoutDataSource_ShouldCompleteImmediately()
    {
        // Act & Assert
        await _reactor.StartAsync();
        // Should complete without throwing
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        await _reactor.StartAsync();

        // Act & Assert
        await _reactor.StopAsync();
        // Should complete without throwing
    }

    [Fact]
    public async Task StartAsync_WithDataSource_ShouldStartRefreshLoop()
    {
        // Arrange
        var mockDataSource = new Mock<IFailoverDataSource<ServiceInfo>>();
        mockDataSource.Setup(x => x.GetSwitch()).Returns(FailoverSwitch.CreateDisabled());
        mockDataSource.Setup(x => x.GetFailoverData()).Returns(new Dictionary<string, FailoverData<ServiceInfo>>());

        using var reactor = new FailoverReactor<ServiceInfo>(_mockLogger.Object, mockDataSource.Object, 100);

        // Act
        await reactor.StartAsync();
        await Task.Delay(200); // Wait for at least one refresh cycle

        // Assert
        mockDataSource.Verify(x => x.GetSwitch(), Times.AtLeastOnce);

        // Cleanup
        await reactor.StopAsync();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_ShouldWorkCorrectly()
    {
        // Arrange
        var service1 = new ServiceInfo { Name = "service-1", Hosts = new List<Instance> { new() { Ip = "10.0.0.1", Port = 8080 } } };
        var service2 = new ServiceInfo { Name = "service-2", Hosts = new List<Instance> { new() { Ip = "10.0.0.2", Port = 8080 } } };

        // Act - Add data
        _reactor.SetFailoverData("service-1", FailoverData<ServiceInfo>.CreateForNaming("service-1", service1));
        _reactor.SetFailoverData("service-2", FailoverData<ServiceInfo>.CreateForNaming("service-2", service2));

        // Assert - Data not accessible when disabled
        _reactor.GetFailoverData("service-1").Should().BeNull();
        _reactor.GetFailoverData("service-2").Should().BeNull();

        // Act - Enable failover
        _reactor.SetFailoverSwitch(true);

        // Assert - Data accessible when enabled
        var result1 = _reactor.GetFailoverData("service-1");
        var result2 = _reactor.GetFailoverData("service-2");
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.Name.Should().Be("service-1");
        result2!.Name.Should().Be("service-2");

        // Act - Remove one service
        _reactor.RemoveFailoverData("service-1");

        // Assert - Removed service not accessible
        _reactor.GetFailoverData("service-1").Should().BeNull();
        _reactor.GetFailoverData("service-2").Should().NotBeNull();

        // Act - Disable failover
        _reactor.SetFailoverSwitch(false);

        // Assert - All data not accessible
        _reactor.GetFailoverData("service-2").Should().BeNull();
    }

    #endregion
}
