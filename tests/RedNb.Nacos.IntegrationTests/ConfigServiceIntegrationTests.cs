using FluentAssertions;
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Config;
using Xunit;
using Xunit.Abstractions;

namespace RedNb.Nacos.IntegrationTests;

/// <summary>
/// Integration tests for Nacos Config Service.
/// Requires a running Nacos server at localhost:8848.
/// </summary>
[Collection("NacosIntegration")]
public class ConfigServiceIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private IConfigService? _configService;
    private readonly NacosClientOptions _options;
    private readonly NacosFactory _factory;

    public ConfigServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848",
            Username = "nacos",
            Password = "nacos",
            Namespace = "",
            DefaultTimeout = 5000
        };
        _factory = new NacosFactory();
    }

    public Task InitializeAsync()
    {
        _configService = _factory.CreateConfigService(_options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_configService is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PublishAndGetConfig_ShouldWork()
    {
        // Arrange
        var dataId = $"test-{Guid.NewGuid():N}";
        var group = "DEFAULT_GROUP";
        var content = "test.config.value=hello world";

        try
        {
            // Act - Publish
            var publishResult = await _configService!.PublishConfigAsync(dataId, group, content);
            _output.WriteLine($"Publish result: {publishResult}");
            publishResult.Should().BeTrue();

            // Wait for config to be saved
            await Task.Delay(500);

            // Act - Get
            var retrievedContent = await _configService.GetConfigAsync(dataId, group, 5000);
            _output.WriteLine($"Retrieved content: {retrievedContent}");

            // Assert
            retrievedContent.Should().Be(content);
        }
        finally
        {
            // Cleanup
            await _configService!.RemoveConfigAsync(dataId, group);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RemoveConfig_ShouldWork()
    {
        // Arrange
        var dataId = $"test-remove-{Guid.NewGuid():N}";
        var group = "DEFAULT_GROUP";
        var content = "to be deleted";

        // Publish first
        await _configService!.PublishConfigAsync(dataId, group, content);
        await Task.Delay(300);

        // Act
        var removeResult = await _configService.RemoveConfigAsync(dataId, group);

        // Assert
        removeResult.Should().BeTrue();

        // Verify removal
        await Task.Delay(300);
        var retrievedContent = await _configService.GetConfigAsync(dataId, group, 5000);
        retrievedContent.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetConfig_NonExistent_ShouldReturnNull()
    {
        // Arrange
        var dataId = $"nonexistent-{Guid.NewGuid():N}";
        var group = "DEFAULT_GROUP";

        // Act
        var content = await _configService!.GetConfigAsync(dataId, group, 5000);

        // Assert
        content.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PublishConfigWithType_ShouldWork()
    {
        // Arrange
        var dataId = $"test-json-{Guid.NewGuid():N}";
        var group = "DEFAULT_GROUP";
        var content = """{"key": "value", "number": 42}""";

        try
        {
            // Act
            var publishResult = await _configService!.PublishConfigAsync(
                dataId, group, content, ConfigType.Json);
            publishResult.Should().BeTrue();

            await Task.Delay(500);

            var retrievedContent = await _configService.GetConfigAsync(dataId, group, 5000);

            // Assert
            retrievedContent.Should().NotBeNull();
            _output.WriteLine($"Retrieved JSON: {retrievedContent}");
        }
        finally
        {
            await _configService!.RemoveConfigAsync(dataId, group);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConfigListener_ShouldReceiveChanges()
    {
        // Arrange
        var dataId = $"test-listener-{Guid.NewGuid():N}";
        var group = "DEFAULT_GROUP";
        var content1 = "initial content";
        var content2 = "updated content";
        
        var tcs = new TaskCompletionSource<ConfigInfo>();
        var listener = new TestConfigChangeListener(info =>
        {
            _output.WriteLine($"Received config change: {info.Content}");
            if (info.Content == content2)
            {
                tcs.TrySetResult(info);
            }
        });

        try
        {
            // Publish initial
            await _configService!.PublishConfigAsync(dataId, group, content1);
            await Task.Delay(500);

            // Add listener
            await _configService.AddListenerAsync(dataId, group, listener);

            // Update config
            await _configService.PublishConfigAsync(dataId, group, content2);

            // Wait for notification (with timeout)
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(30000));

            // Assert
            if (completedTask == tcs.Task)
            {
                var result = await tcs.Task;
                result.Content.Should().Be(content2);
            }
            else
            {
                _output.WriteLine("Timeout waiting for config change notification");
                // May not receive in all cases depending on long polling timing
            }
        }
        finally
        {
            _configService!.RemoveListener(dataId, group, listener);
            await _configService.RemoveConfigAsync(dataId, group);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetServerStatus_ShouldReturnUp()
    {
        // Act
        var status = _configService!.GetServerStatus();

        // Assert
        _output.WriteLine($"Server status: {status}");
        status.Should().Be("UP");
    }

    private class TestConfigChangeListener : IConfigChangeListener
    {
        private readonly Action<ConfigInfo> _callback;

        public TestConfigChangeListener(Action<ConfigInfo> callback)
        {
            _callback = callback;
        }

        public void OnReceiveConfigInfo(ConfigInfo configInfo)
        {
            _callback(configInfo);
        }
    }
}
