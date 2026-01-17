using FluentAssertions;
using Moq;
using RedNb.Nacos.Core.Maintainer;
using Xunit;

namespace RedNb.Nacos.Tests.Maintainer;

/// <summary>
/// Unit tests for IConfigMaintainer interface behavior.
/// </summary>
public class ConfigMaintainerTests
{
    private readonly Mock<IConfigMaintainer> _mockMaintainer;

    public ConfigMaintainerTests()
    {
        _mockMaintainer = new Mock<IConfigMaintainer>();
    }

    #region GetConfigAsync Tests

    [Fact]
    public async Task GetConfigAsync_WithDataId_ShouldReturnConfig()
    {
        // Arrange
        const string dataId = "test-config.yaml";
        var expectedConfig = new ConfigDetailInfo
        {
            DataId = dataId,
            Group = "DEFAULT_GROUP",
            Content = "key: value",
            Type = "yaml"
        };
        _mockMaintainer.Setup(x => x.GetConfigAsync(dataId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _mockMaintainer.Object.GetConfigAsync(dataId);

        // Assert
        result.Should().NotBeNull();
        result!.DataId.Should().Be(dataId);
        result.Content.Should().Be("key: value");
    }

    [Fact]
    public async Task GetConfigAsync_WithDataIdAndGroup_ShouldReturnConfig()
    {
        // Arrange
        const string dataId = "test-config.yaml";
        const string group = "test-group";
        var expectedConfig = new ConfigDetailInfo
        {
            DataId = dataId,
            Group = group,
            Content = "config content"
        };
        _mockMaintainer.Setup(x => x.GetConfigAsync(dataId, group, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _mockMaintainer.Object.GetConfigAsync(dataId, group);

        // Assert
        result.Should().NotBeNull();
        result!.Group.Should().Be(group);
    }

    [Fact]
    public async Task GetConfigAsync_WithNamespace_ShouldReturnConfig()
    {
        // Arrange
        const string dataId = "test-config.yaml";
        const string group = "DEFAULT_GROUP";
        const string namespaceId = "test-ns";
        var expectedConfig = new ConfigDetailInfo
        {
            DataId = dataId,
            Group = group,
            Tenant = namespaceId
        };
        _mockMaintainer.Setup(x => x.GetConfigAsync(dataId, group, namespaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _mockMaintainer.Object.GetConfigAsync(dataId, group, namespaceId);

        // Assert
        result.Should().NotBeNull();
        result!.Tenant.Should().Be(namespaceId);
    }

    #endregion

    #region PublishConfigAsync Tests

    [Fact]
    public async Task PublishConfigAsync_WithContent_ShouldReturnTrue()
    {
        // Arrange
        const string dataId = "test-config.yaml";
        const string content = "key: value";
        _mockMaintainer.Setup(x => x.PublishConfigAsync(dataId, content, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockMaintainer.Object.PublishConfigAsync(dataId, content);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PublishConfigAsync_WithGroupAndContent_ShouldReturnTrue()
    {
        // Arrange
        const string dataId = "test-config.yaml";
        const string group = "test-group";
        const string content = "key: value";
        _mockMaintainer.Setup(x => x.PublishConfigAsync(dataId, group, content, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockMaintainer.Object.PublishConfigAsync(dataId, group, content);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PublishConfigAsync_WithFullParams_ShouldReturnTrue()
    {
        // Arrange
        const string dataId = "test-config.yaml";
        const string group = "test-group";
        const string namespaceId = "test-ns";
        const string content = "key: value";
        const string type = "yaml";
        _mockMaintainer.Setup(x => x.PublishConfigAsync(
            dataId, group, namespaceId, content, type, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockMaintainer.Object.PublishConfigAsync(
            dataId, group, namespaceId, content, type);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region DeleteConfigAsync Tests

    [Fact]
    public async Task DeleteConfigAsync_WithDataId_ShouldReturnTrue()
    {
        // Arrange
        const string dataId = "test-config.yaml";
        _mockMaintainer.Setup(x => x.DeleteConfigAsync(dataId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockMaintainer.Object.DeleteConfigAsync(dataId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteConfigAsync_WithGroupAndDataId_ShouldReturnTrue()
    {
        // Arrange
        const string dataId = "test-config.yaml";
        const string group = "test-group";
        _mockMaintainer.Setup(x => x.DeleteConfigAsync(dataId, group, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockMaintainer.Object.DeleteConfigAsync(dataId, group);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteConfigAsync_WithNamespace_ShouldReturnTrue()
    {
        // Arrange
        const string dataId = "test-config.yaml";
        const string group = "DEFAULT_GROUP";
        const string namespaceId = "test-ns";
        _mockMaintainer.Setup(x => x.DeleteConfigAsync(dataId, group, namespaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockMaintainer.Object.DeleteConfigAsync(dataId, group, namespaceId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ListConfigsAsync Tests

    [Fact]
    public async Task ListConfigsAsync_WithNamespace_ShouldReturnPage()
    {
        // Arrange
        const string namespaceId = "test-ns";
        var expectedPage = new Page<ConfigBasicInfo>
        {
            Count = 2,
            PageNo = 1,
            PageSize = 10,
            List = new List<ConfigBasicInfo>
            {
                new() { DataId = "config1.yaml", Group = "DEFAULT_GROUP" },
                new() { DataId = "config2.yaml", Group = "DEFAULT_GROUP" }
            }
        };
        _mockMaintainer.Setup(x => x.ListConfigsAsync(namespaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _mockMaintainer.Object.ListConfigsAsync(namespaceId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.List.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListConfigsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        const string namespaceId = "test-ns";
        const int pageNo = 2;
        const int pageSize = 5;
        var expectedPage = new Page<ConfigBasicInfo>
        {
            Count = 12,
            PageNo = pageNo,
            PageSize = pageSize,
            List = new List<ConfigBasicInfo>
            {
                new() { DataId = "config6.yaml" },
                new() { DataId = "config7.yaml" },
                new() { DataId = "config8.yaml" },
                new() { DataId = "config9.yaml" },
                new() { DataId = "config10.yaml" }
            }
        };
        _mockMaintainer.Setup(x => x.ListConfigsAsync(
            null, null, namespaceId, null, null, null, pageNo, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _mockMaintainer.Object.ListConfigsAsync(
            null, null, namespaceId, null, null, null, pageNo, pageSize);

        // Assert
        result.PageNo.Should().Be(pageNo);
        result.PageSize.Should().Be(pageSize);
        result.List.Should().HaveCount(5);
    }

    #endregion

    #region SearchConfigsAsync Tests

    [Fact]
    public async Task SearchConfigsAsync_WithKeyword_ShouldReturnMatchingConfigs()
    {
        // Arrange
        const string search = "blur";
        const string namespaceId = "test-ns";
        var expectedPage = new Page<ConfigBasicInfo>
        {
            Count = 1,
            List = new List<ConfigBasicInfo>
            {
                new() { DataId = "test-config.yaml", Group = "DEFAULT_GROUP" }
            }
        };
        _mockMaintainer.Setup(x => x.SearchConfigsAsync(
            null, null, namespaceId, null, null, null, search, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _mockMaintainer.Object.SearchConfigsAsync(
            null, null, namespaceId, null, null, null, search, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetConfigAsync_NonExistent_ShouldReturnNull()
    {
        // Arrange
        const string dataId = "non-existent.yaml";
        _mockMaintainer.Setup(x => x.GetConfigAsync(dataId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigDetailInfo?)null);

        // Act
        var result = await _mockMaintainer.Object.GetConfigAsync(dataId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteConfigAsync_NonExistent_ShouldReturnFalse()
    {
        // Arrange
        const string dataId = "non-existent.yaml";
        _mockMaintainer.Setup(x => x.DeleteConfigAsync(dataId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _mockMaintainer.Object.DeleteConfigAsync(dataId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListConfigsAsync_EmptyNamespace_ShouldReturnEmptyPage()
    {
        // Arrange
        const string namespaceId = "empty-ns";
        var emptyPage = new Page<ConfigBasicInfo>
        {
            Count = 0,
            List = new List<ConfigBasicInfo>()
        };
        _mockMaintainer.Setup(x => x.ListConfigsAsync(namespaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPage);

        // Act
        var result = await _mockMaintainer.Object.ListConfigsAsync(namespaceId);

        // Assert
        result.Count.Should().Be(0);
        result.List.Should().BeEmpty();
    }

    #endregion
}
