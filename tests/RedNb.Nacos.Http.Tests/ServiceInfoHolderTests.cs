using FluentAssertions;
using Moq;
using RedNb.Nacos.Client.Naming;
using RedNb.Nacos.Core.Naming;
using Xunit;

namespace RedNb.Nacos.Http.Tests;

public class ServiceInfoHolderTests
{
    [Fact]
    public void GetServiceInfo_NotCached_ShouldReturnNull()
    {
        // Arrange
        var holder = new ServiceInfoHolder();

        // Act
        var result = holder.GetServiceInfo("testService", "DEFAULT_GROUP", "");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UpdateServiceInfo_ShouldCache()
    {
        // Arrange
        var holder = new ServiceInfoHolder();
        var serviceInfo = new ServiceInfo
        {
            Name = "testService",
            GroupName = "DEFAULT_GROUP",
            Hosts = new List<Instance>
            {
                new Instance { Ip = "192.168.1.1", Port = 8080 }
            }
        };

        // Act
        holder.UpdateServiceInfo(serviceInfo);
        var result = holder.GetServiceInfo("testService", "DEFAULT_GROUP", "");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("testService");
        result.Hosts.Should().HaveCount(1);
    }

    [Fact]
    public void UpdateServiceInfo_ShouldUpdateExisting()
    {
        // Arrange
        var holder = new ServiceInfoHolder();
        var serviceInfo1 = new ServiceInfo
        {
            Name = "testService",
            GroupName = "DEFAULT_GROUP",
            Hosts = new List<Instance>
            {
                new Instance { Ip = "192.168.1.1", Port = 8080 }
            }
        };
        var serviceInfo2 = new ServiceInfo
        {
            Name = "testService",
            GroupName = "DEFAULT_GROUP",
            Hosts = new List<Instance>
            {
                new Instance { Ip = "192.168.1.1", Port = 8080 },
                new Instance { Ip = "192.168.1.2", Port = 8080 }
            }
        };

        // Act
        holder.UpdateServiceInfo(serviceInfo1);
        holder.UpdateServiceInfo(serviceInfo2);
        var result = holder.GetServiceInfo("testService", "DEFAULT_GROUP", "");

        // Assert
        result.Should().NotBeNull();
        result!.Hosts.Should().HaveCount(2);
    }

    [Fact]
    public void GetAllServiceInfos_Empty_ShouldReturnEmpty()
    {
        // Arrange
        var holder = new ServiceInfoHolder();

        // Act
        var result = holder.GetAllServiceInfos();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllServiceInfos_ShouldReturnAll()
    {
        // Arrange
        var holder = new ServiceInfoHolder();
        var serviceInfo1 = new ServiceInfo { Name = "service1", GroupName = "DEFAULT_GROUP" };
        var serviceInfo2 = new ServiceInfo { Name = "service2", GroupName = "DEFAULT_GROUP" };

        holder.UpdateServiceInfo(serviceInfo1);
        holder.UpdateServiceInfo(serviceInfo2);

        // Act
        var result = holder.GetAllServiceInfos();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveServiceInfo_ShouldRemoveFromCache()
    {
        // Arrange
        var holder = new ServiceInfoHolder();
        var serviceInfo = new ServiceInfo { Name = "testService", GroupName = "DEFAULT_GROUP" };
        holder.UpdateServiceInfo(serviceInfo);

        // Act
        holder.RemoveServiceInfo("testService", "DEFAULT_GROUP", "");
        var result = holder.GetServiceInfo("testService", "DEFAULT_GROUP", "");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DifferentClusters_ShouldBeSeparate()
    {
        // Arrange
        var holder = new ServiceInfoHolder();
        var serviceInfo1 = new ServiceInfo 
        { 
            Name = "testService", 
            GroupName = "DEFAULT_GROUP", 
            Clusters = "cluster-a" 
        };
        var serviceInfo2 = new ServiceInfo 
        { 
            Name = "testService", 
            GroupName = "DEFAULT_GROUP", 
            Clusters = "cluster-b" 
        };

        holder.UpdateServiceInfo(serviceInfo1);
        holder.UpdateServiceInfo(serviceInfo2);

        // Act
        var result1 = holder.GetServiceInfo("testService", "DEFAULT_GROUP", "cluster-a");
        var result2 = holder.GetServiceInfo("testService", "DEFAULT_GROUP", "cluster-b");

        // Assert
        result1.Should().NotBeNull();
        result1!.Clusters.Should().Be("cluster-a");
        result2.Should().NotBeNull();
        result2!.Clusters.Should().Be("cluster-b");
    }
}
