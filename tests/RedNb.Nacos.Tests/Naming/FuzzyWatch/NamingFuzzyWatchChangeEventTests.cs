using Xunit;
using RedNb.Nacos.Core.Naming.FuzzyWatch;

namespace RedNb.Nacos.Tests.Naming.FuzzyWatch;

/// <summary>
/// Tests for <see cref="NamingFuzzyWatchChangeEvent"/>.
/// </summary>
public class NamingFuzzyWatchChangeEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange & Act
        var evt = new NamingFuzzyWatchChangeEvent(
            ns: "test-namespace",
            groupName: "test-group",
            serviceName: "test-service",
            changeType: ServiceChangedType.AddService,
            syncType: NamingFuzzyWatchSyncType.InitNotify);

        // Assert
        Assert.Equal("test-namespace", evt.Namespace);
        Assert.Equal("test-group", evt.GroupName);
        Assert.Equal("test-service", evt.ServiceName);
        Assert.Equal(ServiceChangedType.AddService, evt.ChangeType);
        Assert.Equal(NamingFuzzyWatchSyncType.InitNotify, evt.SyncType);
    }

    [Fact]
    public void ServiceChangedType_AddService_ShouldHaveCorrectValue()
    {
        Assert.Equal("ADD_SERVICE", ServiceChangedType.AddService);
    }

    [Fact]
    public void ServiceChangedType_DeleteService_ShouldHaveCorrectValue()
    {
        Assert.Equal("DELETE_SERVICE", ServiceChangedType.DeleteService);
    }

    [Fact]
    public void NamingFuzzyWatchSyncType_InitNotify_ShouldHaveCorrectValue()
    {
        Assert.Equal("FUZZY_WATCH_INIT_NOTIFY", NamingFuzzyWatchSyncType.InitNotify);
    }

    [Fact]
    public void NamingFuzzyWatchSyncType_ResourceChanged_ShouldHaveCorrectValue()
    {
        Assert.Equal("FUZZY_WATCH_RESOURCE_CHANGED", NamingFuzzyWatchSyncType.ResourceChanged);
    }

    [Fact]
    public void Event_WithDifferentChangedTypes_ShouldBeDistinguishable()
    {
        // Arrange
        var addEvent = new NamingFuzzyWatchChangeEvent("ns", "group", "service", ServiceChangedType.AddService, NamingFuzzyWatchSyncType.InitNotify);
        var deleteEvent = new NamingFuzzyWatchChangeEvent("ns", "group", "service", ServiceChangedType.DeleteService, NamingFuzzyWatchSyncType.InitNotify);

        // Assert
        Assert.NotEqual(addEvent.ChangeType, deleteEvent.ChangeType);
    }

    [Fact]
    public void GetServiceKey_ShouldReturnCorrectFormat()
    {
        // Arrange
        var evt = new NamingFuzzyWatchChangeEvent("ns", "group", "service", ServiceChangedType.AddService, NamingFuzzyWatchSyncType.InitNotify);

        // Act
        var key = evt.GetServiceKey();

        // Assert
        Assert.Contains("group", key);
        Assert.Contains("service", key);
    }

    [Fact]
    public void ToString_ShouldContainAllProperties()
    {
        // Arrange
        var evt = new NamingFuzzyWatchChangeEvent("ns", "group", "service", ServiceChangedType.AddService, NamingFuzzyWatchSyncType.InitNotify);

        // Act
        var str = evt.ToString();

        // Assert
        Assert.Contains("ns", str);
        Assert.Contains("group", str);
        Assert.Contains("service", str);
        Assert.Contains("ADD_SERVICE", str);
    }
}
