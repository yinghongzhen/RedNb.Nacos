using FluentAssertions;
using Moq;
using RedNb.Nacos.Core.Maintainer;
using RedNb.Nacos.Core.Naming;
using Xunit;

namespace RedNb.Nacos.Tests.Maintainer;

/// <summary>
/// Unit tests for IServiceMaintainer interface behavior.
/// </summary>
public class ServiceMaintainerTests
{
    private readonly Mock<IServiceMaintainer> _mockMaintainer;

    public ServiceMaintainerTests()
    {
        _mockMaintainer = new Mock<IServiceMaintainer>();
    }

    #region CreateServiceAsync Tests

    [Fact]
    public async Task CreateServiceAsync_WithServiceName_ShouldReturnOk()
    {
        // Arrange
        const string serviceName = "test-service";
        _mockMaintainer.Setup(x => x.CreateServiceAsync(serviceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.CreateServiceAsync(serviceName);

        // Assert
        result.Should().Be("ok");
        _mockMaintainer.Verify(x => x.CreateServiceAsync(serviceName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateServiceAsync_WithGroupAndServiceName_ShouldReturnOk()
    {
        // Arrange
        const string groupName = "test-group";
        const string serviceName = "test-service";
        _mockMaintainer.Setup(x => x.CreateServiceAsync(groupName, serviceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.CreateServiceAsync(groupName, serviceName);

        // Assert
        result.Should().Be("ok");
    }

    [Fact]
    public async Task CreateServiceAsync_WithFullConfiguration_ShouldReturnOk()
    {
        // Arrange
        const string namespaceId = "test-ns";
        const string groupName = "test-group";
        const string serviceName = "test-service";
        const bool ephemeral = true;
        const float protectThreshold = 0.7f;

        _mockMaintainer.Setup(x => x.CreateServiceAsync(
            namespaceId, groupName, serviceName, ephemeral, protectThreshold, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.CreateServiceAsync(
            namespaceId, groupName, serviceName, ephemeral, protectThreshold);

        // Assert
        result.Should().Be("ok");
    }

    [Fact]
    public async Task CreateServiceAsync_WithServiceDefinition_ShouldReturnOk()
    {
        // Arrange
        var service = new ServiceDefinition
        {
            Name = "test-service",
            GroupName = "test-group",
            NamespaceId = "test-ns",
            Ephemeral = true,
            ProtectThreshold = 0.5f,
            Metadata = new Dictionary<string, string> { { "key", "value" } }
        };
        _mockMaintainer.Setup(x => x.CreateServiceAsync(service, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.CreateServiceAsync(service);

        // Assert
        result.Should().Be("ok");
    }

    #endregion

    #region UpdateServiceAsync Tests

    [Fact]
    public async Task UpdateServiceAsync_WithMetadataUpdate_ShouldReturnOk()
    {
        // Arrange
        const string serviceName = "test-service";
        var newMetadata = new Dictionary<string, string> { { "version", "2.0" } };
        _mockMaintainer.Setup(x => x.UpdateServiceAsync(
            serviceName, newMetadata, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.UpdateServiceAsync(serviceName, newMetadata);

        // Assert
        result.Should().Be("ok");
    }

    [Fact]
    public async Task UpdateServiceAsync_WithProtectThreshold_ShouldReturnOk()
    {
        // Arrange
        const string serviceName = "test-service";
        const float newThreshold = 0.8f;
        _mockMaintainer.Setup(x => x.UpdateServiceAsync(
            serviceName, null, newThreshold, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.UpdateServiceAsync(serviceName, newProtectThreshold: newThreshold);

        // Assert
        result.Should().Be("ok");
    }

    #endregion

    #region RemoveServiceAsync Tests

    [Fact]
    public async Task RemoveServiceAsync_WithServiceName_ShouldReturnOk()
    {
        // Arrange
        const string serviceName = "test-service";
        _mockMaintainer.Setup(x => x.RemoveServiceAsync(serviceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.RemoveServiceAsync(serviceName);

        // Assert
        result.Should().Be("ok");
    }

    [Fact]
    public async Task RemoveServiceAsync_WithGroupAndServiceName_ShouldReturnOk()
    {
        // Arrange
        const string groupName = "test-group";
        const string serviceName = "test-service";
        _mockMaintainer.Setup(x => x.RemoveServiceAsync(groupName, serviceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.RemoveServiceAsync(groupName, serviceName);

        // Assert
        result.Should().Be("ok");
    }

    #endregion

    #region GetServiceDetailAsync Tests

    [Fact]
    public async Task GetServiceDetailAsync_ExistingService_ShouldReturnDetail()
    {
        // Arrange
        const string serviceName = "test-service";
        var expectedDetail = new ServiceDetailInfo
        {
            Name = serviceName,
            GroupName = "DEFAULT_GROUP",
            Ephemeral = true,
            ProtectThreshold = 0.5f,
            IpCount = 3,
            HealthyInstanceCount = 2
        };
        _mockMaintainer.Setup(x => x.GetServiceDetailAsync(serviceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDetail);

        // Act
        var result = await _mockMaintainer.Object.GetServiceDetailAsync(serviceName);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(serviceName);
        result.IpCount.Should().Be(3);
        result.HealthyInstanceCount.Should().Be(2);
    }

    [Fact]
    public async Task GetServiceDetailAsync_NonExistingService_ShouldReturnNull()
    {
        // Arrange
        const string serviceName = "non-existing-service";
        _mockMaintainer.Setup(x => x.GetServiceDetailAsync(serviceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceDetailInfo?)null);

        // Act
        var result = await _mockMaintainer.Object.GetServiceDetailAsync(serviceName);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ListServicesAsync Tests

    [Fact]
    public async Task ListServicesAsync_WithNamespace_ShouldReturnPagedList()
    {
        // Arrange
        const string namespaceId = "test-ns";
        var expectedPage = new Page<ServiceView>
        {
            Count = 2,
            PageNo = 1,
            PageSize = 10,
            List = new List<ServiceView>
            {
                new() { Name = "service1", GroupName = "DEFAULT_GROUP", IpCount = 2 },
                new() { Name = "service2", GroupName = "DEFAULT_GROUP", IpCount = 3 }
            }
        };
        _mockMaintainer.Setup(x => x.ListServicesAsync(namespaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _mockMaintainer.Object.ListServicesAsync(namespaceId, CancellationToken.None);

        // Assert
        result.Count.Should().Be(2);
        result.List.Should().HaveCount(2);
        result.List[0].Name.Should().Be("service1");
    }

    [Fact]
    public async Task ListServicesAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        const string namespaceId = "test-ns";
        const int pageNo = 2;
        const int pageSize = 5;
        var expectedPage = new Page<ServiceView>
        {
            Count = 15,
            PageNo = pageNo,
            PageSize = pageSize,
            PagesAvailable = 3,
            List = new List<ServiceView>
            {
                new() { Name = "service6" },
                new() { Name = "service7" },
                new() { Name = "service8" },
                new() { Name = "service9" },
                new() { Name = "service10" }
            }
        };
        _mockMaintainer.Setup(x => x.ListServicesAsync(
            namespaceId, null, null, false, pageNo, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _mockMaintainer.Object.ListServicesAsync(
            namespaceId, pageNo: pageNo, pageSize: pageSize);

        // Assert
        result.PageNo.Should().Be(pageNo);
        result.PageSize.Should().Be(pageSize);
        result.List.Should().HaveCount(5);
    }

    [Fact]
    public async Task ListServicesAsync_EmptyNamespace_ShouldReturnEmptyPage()
    {
        // Arrange
        const string namespaceId = "empty-ns";
        var expectedPage = Page<ServiceView>.Empty();
        _mockMaintainer.Setup(x => x.ListServicesAsync(namespaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _mockMaintainer.Object.ListServicesAsync(namespaceId, CancellationToken.None);

        // Assert
        result.Count.Should().Be(0);
        result.List.Should().BeEmpty();
    }

    #endregion

    #region ListServicesWithDetailAsync Tests

    [Fact]
    public async Task ListServicesWithDetailAsync_ShouldReturnDetailedList()
    {
        // Arrange
        const string namespaceId = "test-ns";
        var expectedPage = new Page<ServiceDetailInfo>
        {
            Count = 1,
            List = new List<ServiceDetailInfo>
            {
                new()
                {
                    Name = "service1",
                    GroupName = "DEFAULT_GROUP",
                    Ephemeral = true,
                    ProtectThreshold = 0.5f,
                    ClusterMap = new Dictionary<string, ClusterInfo>
                    {
                        { "DEFAULT", new ClusterInfo { Name = "DEFAULT" } }
                    }
                }
            }
        };
        _mockMaintainer.Setup(x => x.ListServicesWithDetailAsync(
            namespaceId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _mockMaintainer.Object.ListServicesWithDetailAsync(namespaceId);

        // Assert
        result.Count.Should().Be(1);
        result.List[0].ClusterMap.Should().ContainKey("DEFAULT");
    }

    #endregion

    #region GetSubscribersAsync Tests

    [Fact]
    public async Task GetSubscribersAsync_ExistingService_ShouldReturnSubscribers()
    {
        // Arrange
        const string serviceName = "test-service";
        var expectedPage = new Page<SubscriberInfo>
        {
            Count = 2,
            List = new List<SubscriberInfo>
            {
                new() { Ip = "192.168.1.1", Port = 8080, Agent = "Nacos-Java-Client" },
                new() { Ip = "192.168.1.2", Port = 8080, Agent = "Nacos-CSharp-Client" }
            }
        };
        _mockMaintainer.Setup(x => x.GetSubscribersAsync(serviceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _mockMaintainer.Object.GetSubscribersAsync(serviceName);

        // Assert
        result.Count.Should().Be(2);
        result.List.Should().Contain(s => s.Agent == "Nacos-CSharp-Client");
    }

    #endregion

    #region ListSelectorTypesAsync Tests

    [Fact]
    public async Task ListSelectorTypesAsync_ShouldReturnAvailableTypes()
    {
        // Arrange
        var expectedTypes = new List<string> { "none", "label", "expression" };
        _mockMaintainer.Setup(x => x.ListSelectorTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTypes);

        // Act
        var result = await _mockMaintainer.Object.ListSelectorTypesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("none");
        result.Should().Contain("label");
    }

    #endregion
}
