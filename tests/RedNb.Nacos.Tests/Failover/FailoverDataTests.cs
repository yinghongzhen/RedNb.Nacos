using FluentAssertions;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Failover;
using Xunit;

namespace RedNb.Nacos.Tests.Failover;

/// <summary>
/// Unit tests for FailoverData class.
/// </summary>
public class FailoverDataTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        const string key = "test-key";
        var data = new ServiceInfo { Name = "test-service" };

        // Act
        var failoverData = new FailoverData<ServiceInfo>(FailoverDataType.Naming, key, data);

        // Assert
        failoverData.DataType.Should().Be(FailoverDataType.Naming);
        failoverData.Key.Should().Be(key);
        failoverData.Data.Should().BeSameAs(data);
    }

    [Fact]
    public void CreateForNaming_ShouldSetNamingType()
    {
        // Arrange
        const string key = "service-key";
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
        var failoverData = FailoverData<ServiceInfo>.CreateForNaming(key, serviceInfo);

        // Assert
        failoverData.DataType.Should().Be(FailoverDataType.Naming);
        failoverData.Key.Should().Be(key);
        failoverData.Data.Name.Should().Be("test-service");
        failoverData.Data.Hosts.Should().HaveCount(1);
    }

    [Fact]
    public void CreateForConfig_ShouldSetConfigType()
    {
        // Arrange
        const string key = "config-key";
        var configData = new TestConfigData { Content = "key: value", DataId = "test.yaml" };

        // Act
        var failoverData = FailoverData<TestConfigData>.CreateForConfig(key, configData);

        // Assert
        failoverData.DataType.Should().Be(FailoverDataType.Config);
        failoverData.Key.Should().Be(key);
        failoverData.Data.Content.Should().Be("key: value");
    }

    [Fact]
    public void CreateForNaming_WithComplexServiceInfo_ShouldPreserveData()
    {
        // Arrange
        var serviceInfo = new ServiceInfo
        {
            Name = "complex-service",
            GroupName = "prod-group",
            Clusters = "cluster-a,cluster-b",
            CacheMillis = 10000,
            Hosts = new List<Instance>
            {
                new() { Ip = "10.0.0.1", Port = 8080, Weight = 1.0, Healthy = true },
                new() { Ip = "10.0.0.2", Port = 8080, Weight = 2.0, Healthy = true },
                new() { Ip = "10.0.0.3", Port = 8080, Weight = 1.0, Healthy = false }
            }
        };

        // Act
        var failoverData = FailoverData<ServiceInfo>.CreateForNaming("complex-key", serviceInfo);

        // Assert
        failoverData.Data.Hosts.Should().HaveCount(3);
        failoverData.Data.Clusters.Should().Be("cluster-a,cluster-b");
    }

    [Fact]
    public void FailoverDataType_Naming_ShouldHaveCorrectValue()
    {
        FailoverDataType.Naming.Should().Be(FailoverDataType.Naming);
        ((int)FailoverDataType.Naming).Should().Be(0);
    }

    [Fact]
    public void FailoverDataType_Config_ShouldHaveCorrectValue()
    {
        FailoverDataType.Config.Should().Be(FailoverDataType.Config);
        ((int)FailoverDataType.Config).Should().Be(1);
    }

    /// <summary>
    /// Test helper class for config data.
    /// </summary>
    private class TestConfigData
    {
        public string? DataId { get; set; }
        public string? Group { get; set; }
        public string? Content { get; set; }
    }
}
