using FluentAssertions;
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace RedNb.Nacos.Http.Tests;

/// <summary>
/// Tests for HTTP-based Config Service using WireMock.
/// </summary>
public class ConfigServiceHttpTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly NacosClientOptions _options;
    private readonly NacosFactory _factory;

    public ConfigServiceHttpTests()
    {
        _server = WireMockServer.Start();
        _options = new NacosClientOptions
        {
            ServerAddresses = $"localhost:{_server.Port}",
            Username = "nacos",
            Password = "nacos"
        };
        _factory = new NacosFactory();

        // Setup login endpoint
        SetupLoginEndpoint();
    }

    private void SetupLoginEndpoint()
    {
        _server
            .Given(Request.Create()
                .WithPath("/nacos/v1/auth/login")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"accessToken\":\"test-token\",\"tokenTtl\":18000}"));
    }

    [Fact]
    public async Task GetConfigAsync_Success_ShouldReturnContent()
    {
        // Arrange
        var expectedContent = "key=value\nname=test";

        _server
            .Given(Request.Create()
                .WithPath("/nacos/v1/cs/configs")
                .WithParam("dataId", "test-config")
                .WithParam("group", "DEFAULT_GROUP")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(expectedContent));

        var configService = _factory.CreateConfigService(_options);

        // Act
        var result = await configService.GetConfigAsync("test-config", "DEFAULT_GROUP", 5000);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task GetConfigAsync_NotFound_ShouldReturnNull()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/nacos/v1/cs/configs")
                .WithParam("dataId", "non-existent")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404));

        var configService = _factory.CreateConfigService(_options);

        // Act
        var result = await configService.GetConfigAsync("non-existent", "DEFAULT_GROUP", 5000);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PublishConfigAsync_Success_ShouldReturnTrue()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/nacos/v1/cs/configs")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("true"));

        var configService = _factory.CreateConfigService(_options);

        // Act
        var result = await configService.PublishConfigAsync("test-config", "DEFAULT_GROUP", "new content", "text");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveConfigAsync_Success_ShouldReturnTrue()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/nacos/v1/cs/configs")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("true"));

        var configService = _factory.CreateConfigService(_options);

        // Act
        var result = await configService.RemoveConfigAsync("test-config", "DEFAULT_GROUP");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetServerStatus_WhenHealthy_ShouldReturnUp()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/nacos/v1/console/health/readiness")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("UP"));

        var configService = _factory.CreateConfigService(_options);

        // Act
        var status = configService.GetServerStatus();

        // Assert
        status.Should().Be("UP");
    }

    public void Dispose()
    {
        _server?.Stop();
        _server?.Dispose();
    }
}
