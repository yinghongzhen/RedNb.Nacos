using FluentAssertions;
using RedNb.Nacos;
using RedNb.Nacos.Config;
using RedNb.Nacos.Grpc;
using Xunit;

namespace RedNb.Nacos.IntegrationTests;

/// <summary>
/// Integration tests that validate the gRPC bi-stream config-push path against
/// a real Nacos 3.2.4 server. Exercises the production wire-up:
/// AddListenerAsync -&gt; server listen registration -&gt; PublishConfigAsync -&gt;
/// server push (ConfigChangeNotifyRequest) -&gt; HandleConfigChangeNotify -&gt;
/// NotifyListeners -&gt; IConfigChangeListener.OnReceiveConfigInfo.
/// Requires Nacos 3.2.4 running at localhost:8848 (gRPC port 9848 auto-derived
/// via NacosClientOptions.GrpcPortOffset).
/// </summary>
[Collection("NacosIntegration")]
public class ConfigListenerPushTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private NacosClientOptions _options = null!;
    private IConfigService? _service;

    public ConfigListenerPushTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async ValueTask InitializeAsync()
    {
        _options = new NacosClientOptions
        {
            ServerAddresses = NacosServerFixture.ServerAddress,
            Username = NacosServerFixture.Username,
            Password = NacosServerFixture.Password,
            Namespace = NacosServerFixture.Namespace,
            DefaultTimeout = 5000
        };
        _service = await NacosGrpcFactory.CreateConfigServiceAsync(_options);
    }

    public async ValueTask DisposeAsync()
    {
        if (_service is not null)
        {
            await _service.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddListener_ThenPublish_ListenerReceivesPushContent()
    {
        // Arrange
        var dataId = $"grpc-push-{Guid.NewGuid():N}";
        var group = "DEFAULT_GROUP";
        var initialContent = "v1";
        var updatedContent = "v2";

        var received = new TaskCompletionSource<ConfigInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = new TestConfigChangeListener(info =>
        {
            _output.WriteLine($"Push received: {info.Content}");
            if (info.Content == updatedContent)
            {
                received.TrySetResult(info);
            }
        });

        try
        {
            // Seed initial config so AddListenerAsync can populate MD5 without 404.
            (await _service!.PublishConfigAsync(dataId, group, initialContent)).Should().BeTrue();
            await Task.Delay(500); // let server persist

            await _service.AddListenerAsync(dataId, group, listener);

            // Give the gRPC bi-stream a moment to register the listen.
            await Task.Delay(500);

            // Update config — server should push to subscribed clients.
            (await _service.PublishConfigAsync(dataId, group, updatedContent)).Should().BeTrue();

            var winner = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            winner.Should().BeSameAs(received.Task, "gRPC push should deliver within 30s");

            var info = await received.Task;
            info.Content.Should().Be(updatedContent);
            info.DataId.Should().Be(dataId);
            info.Group.Should().Be(group);
        }
        finally
        {
            if (_service is not null)
            {
                _service.RemoveListener(dataId, group, listener);
                await _service.RemoveConfigAsync(dataId, group);
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RemoveListener_StopsPushNotifications()
    {
        // Arrange
        var dataId = $"grpc-unlisten-{Guid.NewGuid():N}";
        var group = "DEFAULT_GROUP";

        var pushCount = 0;
        var pushTcs = new TaskCompletionSource<ConfigInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = new TestConfigChangeListener(info =>
        {
            Interlocked.Increment(ref pushCount);
            _output.WriteLine($"Push received (count={pushCount}): {info.Content}");
            pushTcs.TrySetResult(info); // first push wins
        });

        try
        {
            await _service!.PublishConfigAsync(dataId, group, "seed");
            await Task.Delay(500);

            await _service.AddListenerAsync(dataId, group, listener);
            await Task.Delay(500);

            await _service.PublishConfigAsync(dataId, group, "first-update");
            var winner = await Task.WhenAny(pushTcs.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            winner.Should().BeSameAs(pushTcs.Task);
            var firstPushCount = pushCount;
            firstPushCount.Should().BeGreaterThanOrEqualTo(1);

            // Now unlisten.
            _service.RemoveListener(dataId, group, listener);
            await Task.Delay(1000); // give unlisten time to land

            // Reset and trigger another publish.
            pushCount = 0;
            await _service.PublishConfigAsync(dataId, group, "second-update");
            await Task.Delay(5000); // wait 5s — should be plenty if unlisten worked

            pushCount.Should().Be(0, "listener was removed; server should not push to it");
        }
        finally
        {
            await _service!.RemoveConfigAsync(dataId, group);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task MultipleListeners_OnSameKey_AllReceivePush()
    {
        // Arrange
        var dataId = $"grpc-fanout-{Guid.NewGuid():N}";
        var group = "DEFAULT_GROUP";

        var tcs1 = new TaskCompletionSource<ConfigInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcs2 = new TaskCompletionSource<ConfigInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener1 = new TestConfigChangeListener(i => tcs1.TrySetResult(i));
        var listener2 = new TestConfigChangeListener(i => tcs2.TrySetResult(i));

        try
        {
            await _service!.PublishConfigAsync(dataId, group, "seed");
            await Task.Delay(500);

            await _service.AddListenerAsync(dataId, group, listener1);
            await _service.AddListenerAsync(dataId, group, listener2);
            await Task.Delay(500);

            await _service.PublishConfigAsync(dataId, group, "fanout-update");

            var all = Task.WhenAll(tcs1.Task, tcs2.Task);
            var winner = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(30)));
            winner.Should().BeSameAs(all, "both listeners should receive the push within 30s");

            (await tcs1.Task).Content.Should().Be("fanout-update");
            (await tcs2.Task).Content.Should().Be("fanout-update");
        }
        finally
        {
            _service!.RemoveListener(dataId, group, listener1);
            _service.RemoveListener(dataId, group, listener2);
            await _service.RemoveConfigAsync(dataId, group);
        }
    }

    private sealed class TestConfigChangeListener : IConfigChangeListener
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
