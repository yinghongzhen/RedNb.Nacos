using FluentAssertions;
using RedNb.Nacos.Core.Naming;
using Xunit;

namespace RedNb.Nacos.Tests;

public class InstanceTests
{
    [Fact]
    public void Instance_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var instance = new Instance();

        // Assert
        instance.Weight.Should().Be(1.0);
        instance.Healthy.Should().BeTrue();
        instance.Enabled.Should().BeTrue();
        instance.Ephemeral.Should().BeTrue();
        instance.Metadata.Should().NotBeNull();
    }

    [Fact]
    public void Instance_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var metadata = new Dictionary<string, string> { { "key", "value" } };

        // Act
        var instance = new Instance
        {
            InstanceId = "instance-1",
            Ip = "192.168.1.100",
            Port = 8080,
            Weight = 2.5,
            Healthy = false,
            Enabled = false,
            Ephemeral = false,
            ClusterName = "cluster-a",
            ServiceName = "service-a",
            Metadata = metadata
        };

        // Assert
        instance.InstanceId.Should().Be("instance-1");
        instance.Ip.Should().Be("192.168.1.100");
        instance.Port.Should().Be(8080);
        instance.Weight.Should().Be(2.5);
        instance.Healthy.Should().BeFalse();
        instance.Enabled.Should().BeFalse();
        instance.Ephemeral.Should().BeFalse();
        instance.ClusterName.Should().Be("cluster-a");
        instance.ServiceName.Should().Be("service-a");
        instance.Metadata.Should().ContainKey("key");
    }

    [Fact]
    public void Instance_ToString_ShouldContainIpAndPort()
    {
        // Arrange
        var instance = new Instance
        {
            Ip = "192.168.1.100",
            Port = 8080,
            ServiceName = "test-service"
        };

        // Act
        var result = instance.ToString();

        // Assert
        result.Should().Contain("192.168.1.100");
        result.Should().Contain("8080");
        result.Should().Contain("test-service");
    }

    [Fact]
    public void Instance_TwoInstancesWithSameIpPort_ShouldBeDifferentByDefault()
    {
        // Arrange
        var instance1 = new Instance { Ip = "192.168.1.100", Port = 8080 };
        var instance2 = new Instance { Ip = "192.168.1.100", Port = 8080 };

        // Act & Assert
        // By default reference equality
        instance1.Should().NotBeSameAs(instance2);
    }
}
