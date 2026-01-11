using FluentAssertions;
using Moq;
using RedNb.Nacos.Client.Config;
using RedNb.Nacos.Core.Config;
using Xunit;

namespace RedNb.Nacos.Http.Tests;

public class ConfigListenerManagerTests
{
    [Fact]
    public void AddListener_ShouldRegisterListener()
    {
        // Arrange
        var manager = new ConfigListenerManager();
        var mockListener = new Mock<IConfigChangeListener>();

        // Act
        manager.AddListener("dataId", "group", "tenant", mockListener.Object);
        var listeners = manager.GetListeners("dataId", "group", "tenant");

        // Assert
        listeners.Should().HaveCount(1);
        listeners.Should().Contain(mockListener.Object);
    }

    [Fact]
    public void AddListener_MultipleTimes_ShouldAddAll()
    {
        // Arrange
        var manager = new ConfigListenerManager();
        var mockListener1 = new Mock<IConfigChangeListener>();
        var mockListener2 = new Mock<IConfigChangeListener>();

        // Act
        manager.AddListener("dataId", "group", null, mockListener1.Object);
        manager.AddListener("dataId", "group", null, mockListener2.Object);
        var listeners = manager.GetListeners("dataId", "group", null);

        // Assert
        listeners.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveListener_ShouldRemoveListener()
    {
        // Arrange
        var manager = new ConfigListenerManager();
        var mockListener = new Mock<IConfigChangeListener>();
        manager.AddListener("dataId", "group", null, mockListener.Object);

        // Act
        manager.RemoveListener("dataId", "group", null, mockListener.Object);
        var listeners = manager.GetListeners("dataId", "group", null);

        // Assert
        listeners.Should().BeEmpty();
    }

    [Fact]
    public void GetListeners_NonExistent_ShouldReturnEmpty()
    {
        // Arrange
        var manager = new ConfigListenerManager();

        // Act
        var listeners = manager.GetListeners("nonexistent", "group", null);

        // Assert
        listeners.Should().BeEmpty();
    }

    [Fact]
    public void GetListeners_DifferentTenants_ShouldBeSeparate()
    {
        // Arrange
        var manager = new ConfigListenerManager();
        var mockListener1 = new Mock<IConfigChangeListener>();
        var mockListener2 = new Mock<IConfigChangeListener>();

        // Act
        manager.AddListener("dataId", "group", "tenant1", mockListener1.Object);
        manager.AddListener("dataId", "group", "tenant2", mockListener2.Object);

        var listeners1 = manager.GetListeners("dataId", "group", "tenant1");
        var listeners2 = manager.GetListeners("dataId", "group", "tenant2");

        // Assert
        listeners1.Should().HaveCount(1);
        listeners1.Should().Contain(mockListener1.Object);
        listeners2.Should().HaveCount(1);
        listeners2.Should().Contain(mockListener2.Object);
    }

    [Fact]
    public void GetAllListenedConfigs_ShouldReturnAll()
    {
        // Arrange
        var manager = new ConfigListenerManager();
        var mockListener1 = new Mock<IConfigChangeListener>();
        var mockListener2 = new Mock<IConfigChangeListener>();

        manager.AddListener("dataId1", "group1", null, mockListener1.Object);
        manager.AddListener("dataId2", "group2", "tenant", mockListener2.Object);

        // Act
        var configs = manager.GetAllListenedConfigs();

        // Assert
        configs.Should().HaveCount(2);
    }

    [Fact]
    public void UpdateMd5_ShouldStoreMd5()
    {
        // Arrange
        var manager = new ConfigListenerManager();
        var mockListener = new Mock<IConfigChangeListener>();
        manager.AddListener("dataId", "group", null, mockListener.Object);

        // Act
        manager.UpdateMd5("dataId", "group", null, "abc123md5");
        var md5 = manager.GetMd5("dataId", "group", null);

        // Assert
        md5.Should().Be("abc123md5");
    }

    [Fact]
    public void GetMd5_NonExistent_ShouldReturnNull()
    {
        // Arrange
        var manager = new ConfigListenerManager();

        // Act
        var md5 = manager.GetMd5("nonexistent", "group", null);

        // Assert
        md5.Should().BeNull();
    }

    [Fact]
    public void HasChanged_DifferentMd5_ShouldReturnTrue()
    {
        // Arrange
        var manager = new ConfigListenerManager();
        var mockListener = new Mock<IConfigChangeListener>();
        manager.AddListener("dataId", "group", null, mockListener.Object);
        manager.UpdateMd5("dataId", "group", null, "oldmd5");

        // Act
        var changed = manager.HasChanged("dataId", "group", null, "newmd5");

        // Assert
        changed.Should().BeTrue();
    }

    [Fact]
    public void HasChanged_SameMd5_ShouldReturnFalse()
    {
        // Arrange
        var manager = new ConfigListenerManager();
        var mockListener = new Mock<IConfigChangeListener>();
        manager.AddListener("dataId", "group", null, mockListener.Object);
        manager.UpdateMd5("dataId", "group", null, "samemd5");

        // Act
        var changed = manager.HasChanged("dataId", "group", null, "samemd5");

        // Assert
        changed.Should().BeFalse();
    }
}
