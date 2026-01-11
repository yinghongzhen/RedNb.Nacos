using Xunit;
using RedNb.Nacos.Core.Config.FuzzyWatch;

namespace RedNb.Nacos.Tests.Config.FuzzyWatch;

/// <summary>
/// Tests for <see cref="ConfigFuzzyWatchChangeEvent"/>.
/// </summary>
public class ConfigFuzzyWatchChangeEventTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange & Act
        var evt = new ConfigFuzzyWatchChangeEvent(
            ns: "test-namespace",
            group: "test-group",
            dataId: "test-data-id",
            changedType: ConfigChangedType.AddConfig,
            syncType: FuzzyWatchSyncType.InitNotify);

        // Assert
        Assert.Equal("test-namespace", evt.Namespace);
        Assert.Equal("test-group", evt.Group);
        Assert.Equal("test-data-id", evt.DataId);
        Assert.Equal(ConfigChangedType.AddConfig, evt.ChangedType);
        Assert.Equal(FuzzyWatchSyncType.InitNotify, evt.SyncType);
    }

    [Fact]
    public void ConfigChangedType_AddConfig_ShouldHaveCorrectValue()
    {
        Assert.Equal("ADD_CONFIG", ConfigChangedType.AddConfig);
    }

    [Fact]
    public void ConfigChangedType_DeleteConfig_ShouldHaveCorrectValue()
    {
        Assert.Equal("DELETE_CONFIG", ConfigChangedType.DeleteConfig);
    }

    [Fact]
    public void ConfigChangedType_ModifyConfig_ShouldHaveCorrectValue()
    {
        Assert.Equal("MODIFY_CONFIG", ConfigChangedType.ModifyConfig);
    }

    [Fact]
    public void FuzzyWatchSyncType_InitNotify_ShouldHaveCorrectValue()
    {
        Assert.Equal("FUZZY_WATCH_INIT_NOTIFY", FuzzyWatchSyncType.InitNotify);
    }

    [Fact]
    public void FuzzyWatchSyncType_ResourceChanged_ShouldHaveCorrectValue()
    {
        Assert.Equal("FUZZY_WATCH_RESOURCE_CHANGED", FuzzyWatchSyncType.ResourceChanged);
    }

    [Fact]
    public void Event_WithDifferentChangedTypes_ShouldBeDistinguishable()
    {
        // Arrange
        var addEvent = new ConfigFuzzyWatchChangeEvent("ns", "group", "dataId", ConfigChangedType.AddConfig, FuzzyWatchSyncType.InitNotify);
        var deleteEvent = new ConfigFuzzyWatchChangeEvent("ns", "group", "dataId", ConfigChangedType.DeleteConfig, FuzzyWatchSyncType.InitNotify);
        var modifyEvent = new ConfigFuzzyWatchChangeEvent("ns", "group", "dataId", ConfigChangedType.ModifyConfig, FuzzyWatchSyncType.InitNotify);

        // Assert
        Assert.NotEqual(addEvent.ChangedType, deleteEvent.ChangedType);
        Assert.NotEqual(addEvent.ChangedType, modifyEvent.ChangedType);
        Assert.NotEqual(deleteEvent.ChangedType, modifyEvent.ChangedType);
    }

    [Fact]
    public void GroupKey_ShouldReturnCorrectFormat()
    {
        // Arrange
        var evt = new ConfigFuzzyWatchChangeEvent("ns", "group", "dataId", ConfigChangedType.AddConfig, FuzzyWatchSyncType.InitNotify);

        // Assert
        Assert.Equal("dataId@@group@@ns", evt.GroupKey);
    }

    [Fact]
    public void Build_ShouldCreateInstance()
    {
        // Act
        var evt = ConfigFuzzyWatchChangeEvent.Build("ns", "group", "dataId", ConfigChangedType.AddConfig, FuzzyWatchSyncType.InitNotify);

        // Assert
        Assert.NotNull(evt);
        Assert.Equal("ns", evt.Namespace);
        Assert.Equal("group", evt.Group);
        Assert.Equal("dataId", evt.DataId);
    }

    [Fact]
    public void ToString_ShouldContainAllProperties()
    {
        // Arrange
        var evt = new ConfigFuzzyWatchChangeEvent("ns", "group", "dataId", ConfigChangedType.AddConfig, FuzzyWatchSyncType.InitNotify);

        // Act
        var str = evt.ToString();

        // Assert
        Assert.Contains("ns", str);
        Assert.Contains("group", str);
        Assert.Contains("dataId", str);
        Assert.Contains("ADD_CONFIG", str);
    }
}
