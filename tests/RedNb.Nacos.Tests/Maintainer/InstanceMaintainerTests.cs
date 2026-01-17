using FluentAssertions;
using Moq;
using RedNb.Nacos.Core.Maintainer;
using RedNb.Nacos.Core.Naming;
using Xunit;

namespace RedNb.Nacos.Tests.Maintainer;

/// <summary>
/// Unit tests for IInstanceMaintainer interface behavior.
/// </summary>
public class InstanceMaintainerTests
{
    private readonly Mock<IInstanceMaintainer> _mockMaintainer;

    public InstanceMaintainerTests()
    {
        _mockMaintainer = new Mock<IInstanceMaintainer>();
    }

    #region RegisterInstanceAsync Tests

    [Fact]
    public async Task RegisterInstanceAsync_SimpleInstance_ShouldReturnOk()
    {
        // Arrange
        const string serviceName = "test-service";
        const string ip = "192.168.1.100";
        const int port = 8080;
        _mockMaintainer.Setup(x => x.RegisterInstanceAsync(
            serviceName, ip, port, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.RegisterInstanceAsync(serviceName, ip, port);

        // Assert
        result.Should().Be("ok");
        _mockMaintainer.Verify(x => x.RegisterInstanceAsync(
            serviceName, ip, port, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterInstanceAsync_WithGroupAndInstance_ShouldReturnOk()
    {
        // Arrange
        const string groupName = "test-group";
        const string serviceName = "test-service";
        var instance = new Instance
        {
            Ip = "192.168.1.100",
            Port = 8080,
            Weight = 1.0,
            Healthy = true,
            Enabled = true,
            Metadata = new Dictionary<string, string> { { "version", "1.0" } }
        };
        _mockMaintainer.Setup(x => x.RegisterInstanceAsync(
            groupName, serviceName, instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.RegisterInstanceAsync(groupName, serviceName, instance);

        // Assert
        result.Should().Be("ok");
    }

    #endregion

    #region DeregisterInstanceAsync Tests

    [Fact]
    public async Task DeregisterInstanceAsync_SimpleInstance_ShouldReturnOk()
    {
        // Arrange
        const string serviceName = "test-service";
        const string ip = "192.168.1.100";
        const int port = 8080;
        _mockMaintainer.Setup(x => x.DeregisterInstanceAsync(
            serviceName, ip, port, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.DeregisterInstanceAsync(serviceName, ip, port);

        // Assert
        result.Should().Be("ok");
    }

    [Fact]
    public async Task DeregisterInstanceAsync_WithGroupAndInstance_ShouldReturnOk()
    {
        // Arrange
        const string groupName = "test-group";
        const string serviceName = "test-service";
        var instance = new Instance { Ip = "192.168.1.100", Port = 8080 };
        _mockMaintainer.Setup(x => x.DeregisterInstanceAsync(
            groupName, serviceName, instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.DeregisterInstanceAsync(groupName, serviceName, instance);

        // Assert
        result.Should().Be("ok");
    }

    #endregion

    #region UpdateInstanceAsync Tests

    [Fact]
    public async Task UpdateInstanceAsync_WithInstance_ShouldReturnOk()
    {
        // Arrange
        const string serviceName = "test-service";
        var instance = new Instance
        {
            Ip = "192.168.1.100",
            Port = 8080,
            Weight = 2.0,
            Enabled = false
        };
        _mockMaintainer.Setup(x => x.UpdateInstanceAsync(
            serviceName, instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.UpdateInstanceAsync(serviceName, instance);

        // Assert
        result.Should().Be("ok");
    }

    [Fact]
    public async Task UpdateInstanceAsync_WithGroupAndInstance_ShouldReturnOk()
    {
        // Arrange
        const string groupName = "test-group";
        const string serviceName = "test-service";
        var instance = new Instance { Ip = "192.168.1.100", Port = 8080, Weight = 3.0 };
        _mockMaintainer.Setup(x => x.UpdateInstanceAsync(
            groupName, serviceName, instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.UpdateInstanceAsync(groupName, serviceName, instance);

        // Assert
        result.Should().Be("ok");
    }

    #endregion

    #region PartialUpdateInstanceAsync Tests

    [Fact]
    public async Task PartialUpdateInstanceAsync_ShouldReturnOk()
    {
        // Arrange
        const string groupName = "test-group";
        const string serviceName = "test-service";
        var instance = new Instance
        {
            Ip = "192.168.1.100",
            Port = 8080,
            Metadata = new Dictionary<string, string> { { "newKey", "newValue" } }
        };
        _mockMaintainer.Setup(x => x.PartialUpdateInstanceAsync(
            groupName, serviceName, instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        // Act
        var result = await _mockMaintainer.Object.PartialUpdateInstanceAsync(groupName, serviceName, instance);

        // Assert
        result.Should().Be("ok");
    }

    #endregion

    #region BatchUpdateInstanceMetadataAsync Tests

    [Fact]
    public async Task BatchUpdateInstanceMetadataAsync_ShouldReturnResult()
    {
        // Arrange
        const string groupName = "test-group";
        const string serviceName = "test-service";
        var instances = new List<Instance>
        {
            new() { Ip = "192.168.1.100", Port = 8080 },
            new() { Ip = "192.168.1.101", Port = 8080 }
        };
        var newMetadata = new Dictionary<string, string>
        {
            { "version", "2.0" },
            { "environment", "production" }
        };
        var expectedResult = new InstanceMetadataBatchResult
        {
            Updated = true,
            UpdatedInstances = new List<string> { "192.168.1.100:8080", "192.168.1.101:8080" }
        };
        _mockMaintainer.Setup(x => x.BatchUpdateInstanceMetadataAsync(
            groupName, serviceName, instances, newMetadata, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockMaintainer.Object.BatchUpdateInstanceMetadataAsync(
            groupName, serviceName, instances, newMetadata);

        // Assert
        result.Updated.Should().BeTrue();
        result.UpdatedInstances.Should().HaveCount(2);
    }

    [Fact]
    public async Task BatchUpdateInstanceMetadataAsync_PartialSuccess_ShouldReturnPartialResult()
    {
        // Arrange
        const string groupName = "test-group";
        const string serviceName = "test-service";
        var instances = new List<Instance>
        {
            new() { Ip = "192.168.1.100", Port = 8080 },
            new() { Ip = "192.168.1.200", Port = 8080 } // Non-existing
        };
        var newMetadata = new Dictionary<string, string> { { "key", "value" } };
        var expectedResult = new InstanceMetadataBatchResult
        {
            Updated = true,
            UpdatedInstances = new List<string> { "192.168.1.100:8080" },
            Message = "Instance 192.168.1.200:8080 not found"
        };
        _mockMaintainer.Setup(x => x.BatchUpdateInstanceMetadataAsync(
            groupName, serviceName, instances, newMetadata, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockMaintainer.Object.BatchUpdateInstanceMetadataAsync(
            groupName, serviceName, instances, newMetadata);

        // Assert
        result.Updated.Should().BeTrue();
        result.UpdatedInstances.Should().HaveCount(1);
        result.Message.Should().Contain("not found");
    }

    #endregion

    #region BatchDeleteInstanceMetadataAsync Tests

    [Fact]
    public async Task BatchDeleteInstanceMetadataAsync_ShouldReturnResult()
    {
        // Arrange
        const string groupName = "test-group";
        const string serviceName = "test-service";
        var instances = new List<Instance>
        {
            new() { Ip = "192.168.1.100", Port = 8080 }
        };
        var metadataKeys = new List<string> { "oldKey1", "oldKey2" };
        var expectedResult = new InstanceMetadataBatchResult { Updated = true };
        _mockMaintainer.Setup(x => x.BatchDeleteInstanceMetadataAsync(
            groupName, serviceName, instances, metadataKeys, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockMaintainer.Object.BatchDeleteInstanceMetadataAsync(
            groupName, serviceName, instances, metadataKeys);

        // Assert
        result.Updated.Should().BeTrue();
    }

    #endregion

    #region ListInstancesAsync Tests

    [Fact]
    public async Task ListInstancesAsync_WithServiceName_ShouldReturnInstances()
    {
        // Arrange
        const string serviceName = "test-service";
        var expectedInstances = new List<Instance>
        {
            new() { Ip = "192.168.1.100", Port = 8080, Healthy = true, Weight = 1.0 },
            new() { Ip = "192.168.1.101", Port = 8080, Healthy = true, Weight = 1.0 },
            new() { Ip = "192.168.1.102", Port = 8080, Healthy = false, Weight = 1.0 }
        };
        _mockMaintainer.Setup(x => x.ListInstancesAsync(
            serviceName, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedInstances);

        // Act
        var result = await _mockMaintainer.Object.ListInstancesAsync(serviceName);

        // Assert
        result.Should().HaveCount(3);
        result.Count(i => i.Healthy).Should().Be(2);
    }

    [Fact]
    public async Task ListInstancesAsync_HealthyOnly_ShouldReturnOnlyHealthyInstances()
    {
        // Arrange
        const string serviceName = "test-service";
        var healthyInstances = new List<Instance>
        {
            new() { Ip = "192.168.1.100", Port = 8080, Healthy = true },
            new() { Ip = "192.168.1.101", Port = 8080, Healthy = true }
        };
        _mockMaintainer.Setup(x => x.ListInstancesAsync(
            serviceName, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthyInstances);

        // Act
        var result = await _mockMaintainer.Object.ListInstancesAsync(serviceName, healthyOnly: true);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.Healthy);
    }

    [Fact]
    public async Task ListInstancesAsync_WithCluster_ShouldFilterByCluster()
    {
        // Arrange
        const string serviceName = "test-service";
        const string clusterName = "cluster-a";
        var clusterInstances = new List<Instance>
        {
            new() { Ip = "192.168.1.100", Port = 8080, ClusterName = clusterName }
        };
        _mockMaintainer.Setup(x => x.ListInstancesAsync(
            serviceName, clusterName, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clusterInstances);

        // Act
        var result = await _mockMaintainer.Object.ListInstancesAsync(
            serviceName, clusterName, false);

        // Assert
        result.Should().HaveCount(1);
        result[0].ClusterName.Should().Be(clusterName);
    }

    #endregion

    #region GetInstanceDetailAsync Tests

    [Fact]
    public async Task GetInstanceDetailAsync_ExistingInstance_ShouldReturnInstance()
    {
        // Arrange
        const string serviceName = "test-service";
        const string ip = "192.168.1.100";
        const int port = 8080;
        var expectedInstance = new Instance
        {
            Ip = ip,
            Port = port,
            ServiceName = serviceName,
            Healthy = true,
            Weight = 1.0,
            Enabled = true,
            Metadata = new Dictionary<string, string> { { "version", "1.0" } }
        };
        _mockMaintainer.Setup(x => x.GetInstanceDetailAsync(
            serviceName, ip, port, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedInstance);

        // Act
        var result = await _mockMaintainer.Object.GetInstanceDetailAsync(serviceName, ip, port);

        // Assert
        result.Should().NotBeNull();
        result!.Ip.Should().Be(ip);
        result.Port.Should().Be(port);
        result.Metadata.Should().ContainKey("version");
    }

    [Fact]
    public async Task GetInstanceDetailAsync_NonExistingInstance_ShouldReturnNull()
    {
        // Arrange
        const string serviceName = "test-service";
        const string ip = "192.168.1.200";
        const int port = 8080;
        _mockMaintainer.Setup(x => x.GetInstanceDetailAsync(
            serviceName, ip, port, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instance?)null);

        // Act
        var result = await _mockMaintainer.Object.GetInstanceDetailAsync(serviceName, ip, port);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInstanceDetailAsync_WithFullParameters_ShouldReturnInstance()
    {
        // Arrange
        const string groupName = "test-group";
        const string serviceName = "test-service";
        const string ip = "192.168.1.100";
        const int port = 8080;
        const string clusterName = "cluster-a";
        var expectedInstance = new Instance
        {
            Ip = ip,
            Port = port,
            ClusterName = clusterName
        };
        _mockMaintainer.Setup(x => x.GetInstanceDetailAsync(
            groupName, serviceName, ip, port, clusterName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedInstance);

        // Act
        var result = await _mockMaintainer.Object.GetInstanceDetailAsync(
            groupName, serviceName, ip, port, clusterName);

        // Assert
        result.Should().NotBeNull();
        result!.ClusterName.Should().Be(clusterName);
    }

    #endregion
}
