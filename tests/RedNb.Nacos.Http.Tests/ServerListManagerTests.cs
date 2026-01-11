using FluentAssertions;
using RedNb.Nacos.Client.Http;
using RedNb.Nacos.Core;
using Xunit;

namespace RedNb.Nacos.Http.Tests;

public class ServerListManagerTests
{
    [Fact]
    public void Constructor_WithSingleServer_ShouldInitialize()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848"
        };

        // Act
        var manager = new ServerListManager(options);

        // Assert
        var servers = manager.GetServerList();
        servers.Should().HaveCount(1);
        servers[0].Should().Be("localhost:8848");
    }

    [Fact]
    public void Constructor_WithMultipleServers_ShouldInitialize()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "server1:8848,server2:8848,server3:8848"
        };

        // Act
        var manager = new ServerListManager(options);

        // Assert
        var servers = manager.GetServerList();
        servers.Should().HaveCount(3);
    }

    [Fact]
    public void GetNextServer_ShouldReturnServer()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "server1:8848,server2:8848"
        };
        var manager = new ServerListManager(options);

        // Act
        var server = manager.GetNextServer();

        // Assert
        server.Should().NotBeNullOrEmpty();
        server.Should().BeOneOf("server1:8848", "server2:8848");
    }

    [Fact]
    public void GetNextServer_ShouldRoundRobin()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "server1:8848,server2:8848"
        };
        var manager = new ServerListManager(options);

        // Act
        var server1 = manager.GetNextServer();
        var server2 = manager.GetNextServer();

        // Assert
        server1.Should().NotBe(server2);
    }

    [Fact]
    public void MarkServerHealthy_ShouldMarkServer()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "server1:8848,server2:8848"
        };
        var manager = new ServerListManager(options);
        manager.MarkServerUnhealthy("server1:8848");

        // Act
        manager.MarkServerHealthy("server1:8848");

        // Assert
        manager.HasHealthyServer().Should().BeTrue();
    }

    [Fact]
    public void MarkServerUnhealthy_ShouldMarkServer()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "server1:8848"
        };
        var manager = new ServerListManager(options);

        // Act
        manager.MarkServerUnhealthy("server1:8848");

        // Assert
        manager.GetHealthyServerList().Should().BeEmpty();
    }

    [Fact]
    public void HasHealthyServer_WithAllHealthy_ShouldReturnTrue()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "server1:8848,server2:8848"
        };
        var manager = new ServerListManager(options);

        // Act & Assert
        manager.HasHealthyServer().Should().BeTrue();
    }

    [Fact]
    public void HasHealthyServer_WithAllUnhealthy_ShouldReturnFalse()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "server1:8848"
        };
        var manager = new ServerListManager(options);
        manager.MarkServerUnhealthy("server1:8848");

        // Act & Assert
        manager.HasHealthyServer().Should().BeFalse();
    }

    [Fact]
    public void GetHealthyServerList_ShouldReturnOnlyHealthy()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "server1:8848,server2:8848"
        };
        var manager = new ServerListManager(options);
        manager.MarkServerUnhealthy("server1:8848");

        // Act
        var healthyServers = manager.GetHealthyServerList();

        // Assert
        healthyServers.Should().HaveCount(1);
        healthyServers[0].Should().Be("server2:8848");
    }
}
