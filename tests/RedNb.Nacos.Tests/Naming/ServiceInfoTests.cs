using FluentAssertions;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming;
using Xunit;

namespace RedNb.Nacos.Tests.Naming;

public class ServiceInfoTests
{
    [Fact]
    public void ServiceInfo_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var info = new ServiceInfo();

        // Assert
        info.Name.Should().Be(string.Empty);
        info.GroupName.Should().Be(NacosConstants.DefaultGroup);
        info.Clusters.Should().BeNull();
        info.CacheMillis.Should().Be(1000L);
        info.Hosts.Should().NotBeNull();
        info.Hosts.Should().BeEmpty();
        info.Checksum.Should().Be(string.Empty);
        info.AllIps.Should().BeFalse();
        info.ReachProtectionThreshold.Should().BeFalse();
    }

    [Fact]
    public void ServiceInfo_ConstructorWithName_ShouldSetNameAndClusters()
    {
        // Act
        var info = new ServiceInfo("my-service", "cluster1,cluster2");

        // Assert
        info.Name.Should().Be("my-service");
        info.Clusters.Should().Be("cluster1,cluster2");
    }

    [Fact]
    public void ServiceInfo_FromKey_ThreePartKey_ShouldParseCorrectly()
    {
        // Arrange
        var key = $"my-group{NacosConstants.ServiceInfoSplitter}my-service{NacosConstants.ServiceInfoSplitter}cluster1";

        // Act
        var info = ServiceInfo.FromKey(key);

        // Assert
        info.GroupName.Should().Be("my-group");
        info.Name.Should().Be("my-service");
        info.Clusters.Should().Be("cluster1");
    }

    [Fact]
    public void ServiceInfo_FromKey_TwoPartKey_ShouldParseCorrectly()
    {
        // Arrange
        var key = $"my-group{NacosConstants.ServiceInfoSplitter}my-service";

        // Act
        var info = ServiceInfo.FromKey(key);

        // Assert
        info.GroupName.Should().Be("my-group");
        info.Name.Should().Be("my-service");
        info.Clusters.Should().BeNull();
    }

    [Fact]
    public void ServiceInfo_FromKey_InvalidKey_ShouldThrow()
    {
        // Arrange
        var key = "invalid-key-without-splitter";

        // Act & Assert
        var action = () => ServiceInfo.FromKey(key);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ServiceInfo_IpCount_ShouldReturnHostsCount()
    {
        // Arrange
        var info = new ServiceInfo();
        info.Hosts.Add(new Instance { Ip = "1.1.1.1", Port = 8080 });
        info.Hosts.Add(new Instance { Ip = "2.2.2.2", Port = 8080 });

        // Act & Assert
        info.IpCount().Should().Be(2);
    }

    [Fact]
    public void ServiceInfo_IsValid_WithHosts_ShouldReturnTrue()
    {
        // Arrange
        var info = new ServiceInfo();
        info.Hosts.Add(new Instance { Ip = "1.1.1.1", Port = 8080 });

        // Act & Assert
        info.IsValid().Should().BeTrue();
    }

    [Fact]
    public void ServiceInfo_IsValid_WithoutHosts_ShouldReturnFalse()
    {
        // Arrange
        var info = new ServiceInfo();

        // Act & Assert
        info.IsValid().Should().BeFalse();
    }

    [Fact]
    public void ServiceInfo_Validate_AllIpsTrue_ShouldReturnTrue()
    {
        // Arrange
        var info = new ServiceInfo { AllIps = true };

        // Act & Assert
        info.Validate().Should().BeTrue();
    }

    [Fact]
    public void ServiceInfo_Validate_WithHealthyInstances_ShouldReturnTrue()
    {
        // Arrange
        var info = new ServiceInfo();
        info.Hosts.Add(new Instance { Ip = "1.1.1.1", Port = 8080, Healthy = true, Weight = 1.0 });

        // Act & Assert
        info.Validate().Should().BeTrue();
    }

    [Fact]
    public void ServiceInfo_Validate_WithUnhealthyInstances_ShouldReturnFalse()
    {
        // Arrange
        var info = new ServiceInfo();
        info.Hosts.Add(new Instance { Ip = "1.1.1.1", Port = 8080, Healthy = false, Weight = 1.0 });

        // Act & Assert
        info.Validate().Should().BeFalse();
    }

    [Fact]
    public void ServiceInfo_Validate_WithZeroWeightInstances_ShouldReturnFalse()
    {
        // Arrange
        var info = new ServiceInfo();
        info.Hosts.Add(new Instance { Ip = "1.1.1.1", Port = 8080, Healthy = true, Weight = 0.0 });

        // Act & Assert
        info.Validate().Should().BeFalse();
    }

    [Fact]
    public void ServiceInfo_Expired_RecentRefresh_ShouldReturnFalse()
    {
        // Arrange
        var info = new ServiceInfo
        {
            LastRefTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            CacheMillis = 10000
        };

        // Act & Assert
        info.Expired().Should().BeFalse();
    }

    [Fact]
    public void ServiceInfo_Expired_OldRefresh_ShouldReturnTrue()
    {
        // Arrange
        var info = new ServiceInfo
        {
            LastRefTime = DateTimeOffset.UtcNow.AddSeconds(-10).ToUnixTimeMilliseconds(),
            CacheMillis = 1000
        };

        // Act & Assert
        info.Expired().Should().BeTrue();
    }

    [Fact]
    public void ServiceInfo_Key_ShouldReturnCorrectFormat()
    {
        // Arrange
        var info = new ServiceInfo
        {
            Name = "my-service",
            GroupName = "my-group",
            Clusters = "cluster1"
        };

        // Act
        var key = info.Key;

        // Assert
        key.Should().Contain("my-group");
        key.Should().Contain("my-service");
        key.Should().Contain("cluster1");
    }

    [Fact]
    public void ServiceInfo_GetGroupedServiceName_WithGroup_ShouldIncludeGroup()
    {
        // Arrange
        var info = new ServiceInfo
        {
            Name = "my-service",
            GroupName = "my-group"
        };

        // Act
        var name = info.GetGroupedServiceName();

        // Assert
        name.Should().Contain("my-group");
        name.Should().Contain("my-service");
    }

    [Fact]
    public void ServiceInfo_AddHost_ShouldAddToHosts()
    {
        // Arrange
        var info = new ServiceInfo();
        var instance = new Instance { Ip = "1.1.1.1", Port = 8080 };

        // Act
        info.AddHost(instance);

        // Assert
        info.Hosts.Should().HaveCount(1);
        info.Hosts[0].Ip.Should().Be("1.1.1.1");
    }

    [Fact]
    public void ServiceInfo_AddAllHosts_ShouldAddMultiple()
    {
        // Arrange
        var info = new ServiceInfo();
        var instances = new List<Instance>
        {
            new() { Ip = "1.1.1.1", Port = 8080 },
            new() { Ip = "2.2.2.2", Port = 8080 }
        };

        // Act
        info.AddAllHosts(instances);

        // Assert
        info.Hosts.Should().HaveCount(2);
    }

    [Fact]
    public void ServiceInfo_ToString_ShouldReturnKey()
    {
        // Arrange
        var info = new ServiceInfo { Name = "service", GroupName = "group" };

        // Act
        var str = info.ToString();

        // Assert
        str.Should().Be(info.Key);
    }

    [Fact]
    public void ServiceInfo_GetKey_Static_WithClusters_ShouldIncludeClusters()
    {
        // Act
        var key = ServiceInfo.GetKey("service", "cluster1");

        // Assert
        key.Should().Contain("cluster1");
    }

    [Fact]
    public void ServiceInfo_GetKey_Static_WithoutClusters_ShouldReturnNameOnly()
    {
        // Act
        var key = ServiceInfo.GetKey("service", null);

        // Assert
        key.Should().Be("service");
    }
}
