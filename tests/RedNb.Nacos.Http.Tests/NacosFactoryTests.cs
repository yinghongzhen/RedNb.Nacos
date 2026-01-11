using FluentAssertions;
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Naming;
using Xunit;

namespace RedNb.Nacos.Http.Tests;

public class NacosFactoryTests
{
    [Fact]
    public void CreateConfigService_ShouldReturnInstance()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848"
        };
        var factory = new NacosFactory();

        // Act
        var service = factory.CreateConfigService(options);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IConfigService>();
    }

    [Fact]
    public void CreateNamingService_ShouldReturnInstance()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848"
        };
        var factory = new NacosFactory();

        // Act
        var service = factory.CreateNamingService(options);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<INamingService>();
    }

    [Fact]
    public void CreateConfigService_WithServerAddress_ShouldReturnInstance()
    {
        // Arrange
        var factory = new NacosFactory();

        // Act
        var service = factory.CreateConfigService("localhost:8848");

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IConfigService>();
    }

    [Fact]
    public void CreateNamingService_WithServerAddress_ShouldReturnInstance()
    {
        // Arrange
        var factory = new NacosFactory();

        // Act
        var service = factory.CreateNamingService("localhost:8848");

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<INamingService>();
    }

    [Fact]
    public void Factory_ImplementsINacosFactory_ShouldBeTrue()
    {
        // Act
        var factory = new NacosFactory();

        // Assert
        factory.Should().BeAssignableTo<INacosFactory>();
    }

    [Fact]
    public void CreateConfigService_WithCredentials_ShouldReturnInstance()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848",
            Username = "nacos",
            Password = "nacos"
        };
        var factory = new NacosFactory();

        // Act
        var service = factory.CreateConfigService(options);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void CreateNamingService_WithNamespace_ShouldReturnInstance()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848",
            Namespace = "test-namespace"
        };
        var factory = new NacosFactory();

        // Act
        var service = factory.CreateNamingService(options);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void StaticCreateConfigService_ShouldReturnInstance()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848"
        };

        // Act
        var service = NacosFactory.CreateConfigServiceStatic(options);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IConfigService>();
    }

    [Fact]
    public void StaticCreateNamingService_ShouldReturnInstance()
    {
        // Arrange
        var options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848"
        };

        // Act
        var service = NacosFactory.CreateNamingServiceStatic(options);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<INamingService>();
    }
}
