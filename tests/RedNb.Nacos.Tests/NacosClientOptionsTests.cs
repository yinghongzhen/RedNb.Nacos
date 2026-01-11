using FluentAssertions;
using RedNb.Nacos.Core;
using Xunit;

namespace RedNb.Nacos.Tests;

public class NacosClientOptionsTests
{
    [Fact]
    public void NacosClientOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var options = new NacosClientOptions();

        // Assert
        options.EnableTls.Should().BeFalse();
        options.DefaultTimeout.Should().Be(3000); // NacosConstants.DefaultTimeout
        options.LongPollTimeout.Should().Be(30000);
        options.RetryCount.Should().Be(3);
        options.ServerAddresses.Should().Be("localhost:8848"); // Default value
    }

    [Fact]
    public void GetServerAddressList_SingleServer_ShouldReturnList()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848"
        };

        // Act
        var servers = options.GetServerAddressList();

        // Assert
        servers.Should().HaveCount(1);
        servers[0].Should().Be("localhost:8848");
    }

    [Fact]
    public void GetServerAddressList_MultipleServers_ShouldReturnAllServers()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "server1:8848,server2:8848,server3:8848"
        };

        // Act
        var servers = options.GetServerAddressList();

        // Assert
        servers.Should().HaveCount(3);
        servers.Should().Contain("server1:8848");
        servers.Should().Contain("server2:8848");
        servers.Should().Contain("server3:8848");
    }

    [Fact]
    public void GetServerAddressList_WithSpaces_ShouldTrimServers()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = " server1:8848 , server2:8848 "
        };

        // Act
        var servers = options.GetServerAddressList();

        // Assert
        servers.Should().HaveCount(2);
        servers[0].Should().Be("server1:8848");
        servers[1].Should().Be("server2:8848");
    }

    [Fact]
    public void GetServerAddressList_EmptyAddress_ShouldReturnEmptyList()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = ""
        };

        // Act
        var servers = options.GetServerAddressList();

        // Assert
        servers.Should().BeEmpty();
    }

    [Fact]
    public void GetServerAddressList_DefaultAddress_ShouldReturnLocalhost()
    {
        // Arrange
        var options = new NacosClientOptions();

        // Act
        var servers = options.GetServerAddressList();

        // Assert - ServerAddresses defaults to "localhost:8848"
        servers.Should().HaveCount(1);
        servers[0].Should().Be("localhost:8848");
    }

    [Fact]
    public void GetGrpcAddress_DefaultPort_ShouldAddOffset()
    {
        // Arrange
        var options = new NacosClientOptions();

        // Act
        var grpcAddress = options.GetGrpcAddress("localhost:8848");

        // Assert
        grpcAddress.Should().Be("localhost:9848");
    }

    [Fact]
    public void GetGrpcAddress_CustomPort_ShouldAddOffset()
    {
        // Arrange
        var options = new NacosClientOptions();

        // Act
        var grpcAddress = options.GetGrpcAddress("192.168.1.100:9000");

        // Assert
        grpcAddress.Should().Be("192.168.1.100:10000");
    }

    [Fact]
    public void GetGrpcAddress_NoPort_ShouldReturnOriginalAddress()
    {
        // Arrange
        var options = new NacosClientOptions();

        // Act
        var grpcAddress = options.GetGrpcAddress("localhost");

        // Assert - Without port, the implementation returns original address
        grpcAddress.Should().Be("localhost");
    }

    [Fact]
    public void NacosClientOptions_SetCredentials_ShouldRetainValues()
    {
        // Arrange & Act
        var options = new NacosClientOptions
        {
            Username = "nacos",
            Password = "nacos"
        };

        // Assert
        options.Username.Should().Be("nacos");
        options.Password.Should().Be("nacos");
    }

    [Fact]
    public void NacosClientOptions_SetNamespace_ShouldRetainValue()
    {
        // Arrange & Act
        var options = new NacosClientOptions
        {
            Namespace = "test-namespace"
        };

        // Assert
        options.Namespace.Should().Be("test-namespace");
    }
}
