using FluentAssertions;
using RedNb.Nacos.Core.Config;
using Xunit;

namespace RedNb.Nacos.Tests.Config;

public class ConfigChangeEventTests
{
    [Fact]
    public void ConfigChangeEvent_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var evt = new ConfigChangeEvent();

        // Assert
        evt.DataId.Should().Be(string.Empty);
        evt.Group.Should().Be(string.Empty);
        evt.Tenant.Should().BeNull();
        evt.ChangeItems.Should().NotBeNull();
        evt.ChangeItems.Should().BeEmpty();
    }

    [Fact]
    public void ConfigChangeEvent_SetProperties_ShouldRetainValues()
    {
        // Arrange & Act
        var evt = new ConfigChangeEvent
        {
            DataId = "test-data-id",
            Group = "test-group",
            Tenant = "test-tenant"
        };

        // Assert
        evt.DataId.Should().Be("test-data-id");
        evt.Group.Should().Be("test-group");
        evt.Tenant.Should().Be("test-tenant");
    }

    [Fact]
    public void ConfigChangeEvent_AddChangeItems_ShouldWork()
    {
        // Arrange
        var evt = new ConfigChangeEvent
        {
            DataId = "config",
            Group = "DEFAULT_GROUP"
        };

        var item = new ConfigChangeItem("key1", "oldValue", "newValue", PropertyChangeType.Modified);

        // Act
        evt.ChangeItems.Add("key1", item);

        // Assert
        evt.ChangeItems.Should().HaveCount(1);
        evt.ChangeItems["key1"].Key.Should().Be("key1");
        evt.ChangeItems["key1"].OldValue.Should().Be("oldValue");
        evt.ChangeItems["key1"].NewValue.Should().Be("newValue");
        evt.ChangeItems["key1"].Type.Should().Be(PropertyChangeType.Modified);
    }
}

public class ConfigChangeItemTests
{
    [Fact]
    public void ConfigChangeItem_DefaultConstructor_ShouldHaveDefaultValues()
    {
        // Act
        var item = new ConfigChangeItem();

        // Assert
        item.Key.Should().Be(string.Empty);
        item.OldValue.Should().BeNull();
        item.NewValue.Should().BeNull();
        item.Type.Should().Be(PropertyChangeType.Added); // Default enum value
    }

    [Fact]
    public void ConfigChangeItem_ParameterizedConstructor_ShouldSetValues()
    {
        // Act
        var item = new ConfigChangeItem("myKey", "old", "new", PropertyChangeType.Deleted);

        // Assert
        item.Key.Should().Be("myKey");
        item.OldValue.Should().Be("old");
        item.NewValue.Should().Be("new");
        item.Type.Should().Be(PropertyChangeType.Deleted);
    }

    [Theory]
    [InlineData(PropertyChangeType.Added)]
    [InlineData(PropertyChangeType.Modified)]
    [InlineData(PropertyChangeType.Deleted)]
    public void ConfigChangeItem_AllChangeTypes_ShouldBeSupported(PropertyChangeType changeType)
    {
        // Act
        var item = new ConfigChangeItem
        {
            Key = "key",
            Type = changeType
        };

        // Assert
        item.Type.Should().Be(changeType);
    }

    [Fact]
    public void ConfigChangeItem_WithNullValues_ShouldWork()
    {
        // Act
        var item = new ConfigChangeItem("key", null, null, PropertyChangeType.Deleted);

        // Assert
        item.OldValue.Should().BeNull();
        item.NewValue.Should().BeNull();
    }
}
