using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RedNb.Nacos.Auth;
using RedNb.Nacos.Common.Failover;
using RedNb.Nacos.Common.Options;
using RedNb.Nacos.Config;
using RedNb.Nacos.Naming;
using RedNb.Nacos.Remote.Grpc;
using RedNb.Nacos.Remote.Http;

namespace RedNb.Nacos.Tests;

/// <summary>
/// 依赖注入扩展测试
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRedNbNacos_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nacos:ServerAddresses:0"] = "http://localhost:8848",
                ["Nacos:Namespace"] = "test"
            })
            .Build();

        // Act
        services.AddRedNbNacos(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IOptions<NacosOptions>>());
        Assert.NotNull(provider.GetService<IAuthService>());
        Assert.NotNull(provider.GetService<INacosHttpClient>());
        Assert.NotNull(provider.GetService<INacosConfigService>());
        Assert.NotNull(provider.GetService<INacosNamingService>());
        Assert.NotNull(provider.GetService<IConfigSnapshot>());
        Assert.NotNull(provider.GetService<IServiceSnapshot>());
        Assert.NotNull(provider.GetService<INacosGrpcClient>());
    }

    [Fact]
    public void AddRedNbNacos_WithAction_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRedNbNacos(options =>
        {
            options.ServerAddresses = ["http://localhost:8848"];
            options.Namespace = "test-namespace";
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NacosOptions>>().Value;

        // Assert
        Assert.Contains("http://localhost:8848", options.ServerAddresses);
        Assert.Equal("test-namespace", options.Namespace);
    }

    [Fact]
    public void AddRedNbNacosConfig_ShouldRegisterOnlyConfigService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nacos:ServerAddresses:0"] = "http://localhost:8848"
            })
            .Build();

        // Act
        services.AddRedNbNacosConfig(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<INacosConfigService>());
        Assert.Null(provider.GetService<INacosNamingService>());
    }

    [Fact]
    public void AddRedNbNacosNaming_ShouldRegisterOnlyNamingService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nacos:ServerAddresses:0"] = "http://localhost:8848"
            })
            .Build();

        // Act
        services.AddRedNbNacosNaming(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<INacosNamingService>());
        Assert.Null(provider.GetService<INacosConfigService>());
    }

    [Fact]
    public void AddRedNbNacos_ShouldRegisterSnapshotServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nacos:ServerAddresses:0"] = "http://localhost:8848",
                ["Nacos:Failover:Enabled"] = "true"
            })
            .Build();

        // Act
        services.AddRedNbNacos(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var configSnapshot = provider.GetService<IConfigSnapshot>();
        var serviceSnapshot = provider.GetService<IServiceSnapshot>();
        
        Assert.NotNull(configSnapshot);
        Assert.NotNull(serviceSnapshot);
        Assert.IsType<LocalFileConfigSnapshot>(configSnapshot);
        Assert.IsType<LocalFileServiceSnapshot>(serviceSnapshot);
    }

    [Fact]
    public void AddRedNbNacos_ShouldRegisterGrpcClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nacos:ServerAddresses:0"] = "http://localhost:8848",
                ["Nacos:UseGrpc"] = "true"
            })
            .Build();

        // Act
        services.AddRedNbNacos(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var grpcClient = provider.GetService<INacosGrpcClient>();
        Assert.NotNull(grpcClient);
        Assert.IsType<NacosGrpcClient>(grpcClient);
    }
}
