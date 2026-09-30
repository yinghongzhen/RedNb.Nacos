using FluentAssertions;
using RedNb.Nacos;
using RedNb.Nacos.Naming;
using RedNb.Nacos.Grpc;
using Xunit;

namespace RedNb.Nacos.IntegrationTests;

/// <summary>
/// Integration tests that validate the gRPC bi-stream naming-push path against
/// a real Nacos 3.2.4 server. Exercises the production wire-up:
/// SubscribeAsync -> server subscribe registration -> RegisterInstanceAsync ->
/// server push (NotifySubscriberRequest) -> HandleServiceChangeNotify ->
/// NamingServiceInfoHolder.ProcessServiceInfo -> OnServiceInfoChanged ->
/// HandleServiceInfoChanged -> NotifySubscribers -> IInstancesChangeEvent.
/// Requires Nacos 3.2.4 running at localhost:8848 (gRPC port 9848 auto-derived
/// via NacosClientOptions.GrpcPortOffset).
/// </summary>
[Collection("NacosIntegration")]
public class NamingSubscribePushTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private NacosClientOptions _options = null!;
    private INamingService? _service;

    public NamingSubscribePushTests(ITestOutputHelper output)
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
        _service = await NacosGrpcFactory.CreateNamingServiceAsync(_options);
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
    public async Task Subscribe_ThenRegisterInstance_ListenerReceivesInstances()
    {
        var serviceName = $"grpc-push-{Guid.NewGuid():N}";
        var group = "DEFAULT_GROUP";

        var received = new TaskCompletionSource<IInstancesChangeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        Action<IInstancesChangeEvent> listener = evt =>
        {
            _output.WriteLine($"Push received: {evt.ServiceName}@{evt.GroupName}, instances={evt.Instances.Count}");
            if (evt.Instances.Count > 0)
            {
                received.TrySetResult(evt);
            }
        };

        try
        {
            // Subscribe BEFORE registering — listener should receive push when first instance arrives.
            await _service!.SubscribeAsync(serviceName, group, new List<string>(), listener);

            // Give the gRPC bi-stream a moment to register the subscribe.
            await Task.Delay(1000);

            // Register an instance — server should push to subscribed clients.
            await _service.RegisterInstanceAsync(serviceName, group, new Instance
            {
                Ip = "127.0.0.1",
                Port = 8080,
                Healthy = true,
                Enabled = true,
                Ephemeral = true,
                ClusterName = "DEFAULT"
            });

            var winner = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            winner.Should().BeSameAs(received.Task, "gRPC naming push should deliver within 30s");

            var evt = await received.Task;
            evt.ServiceName.Should().Be(serviceName);
            evt.GroupName.Should().Be(group);
            evt.Instances.Should().HaveCount(1);
            evt.Instances[0].Ip.Should().Be("127.0.0.1");
            evt.Instances[0].Port.Should().Be(8080);
        }
        finally
        {
            if (_service is not null)
            {
                await _service.UnsubscribeAsync(serviceName, group, new List<string>(), listener);
                await _service.DeregisterInstanceAsync(serviceName, group, new Instance
                {
                    Ip = "127.0.0.1",
                    Port = 8080,
                    ClusterName = "DEFAULT"
                });
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Unsubscribe_StopsPushNotifications()
    {
        var serviceName = $"grpc-unsub-{Guid.NewGuid():N}";
        var group = "DEFAULT_GROUP";

        var pushCount = 0;
        var firstPushTcs = new TaskCompletionSource<IInstancesChangeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        Action<IInstancesChangeEvent> listener = evt =>
        {
            Interlocked.Increment(ref pushCount);
            _output.WriteLine($"Push received (count={pushCount}): instances={evt.Instances.Count}");
            firstPushTcs.TrySetResult(evt);
        };

        try
        {
            await _service!.RegisterInstanceAsync(serviceName, group, new Instance
            {
                Ip = "127.0.0.1",
                Port = 8080,
                ClusterName = "DEFAULT"
            });
            await Task.Delay(500);

            await _service.SubscribeAsync(serviceName, group, new List<string>(), listener);
            await Task.Delay(1000);

            // Trigger a change by re-registering (server pushes update).
            await _service.RegisterInstanceAsync(serviceName, group, new Instance
            {
                Ip = "127.0.0.1",
                Port = 8080,
                ClusterName = "DEFAULT"
            });

            var winner = await Task.WhenAny(firstPushTcs.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            winner.Should().BeSameAs(firstPushTcs.Task);
            var firstPushCount = pushCount;
            firstPushCount.Should().BeGreaterThanOrEqualTo(1);

            // Now unsubscribe.
            await _service.UnsubscribeAsync(serviceName, group, new List<string>(), listener);
            await Task.Delay(1000);

            // Reset counter and trigger another change.
            pushCount = 0;
            await _service.RegisterInstanceAsync(serviceName, group, new Instance
            {
                Ip = "127.0.0.2",
                Port = 8080,
                ClusterName = "DEFAULT"
            });
            await Task.Delay(5000);

            pushCount.Should().Be(0, "listener was removed; server should not push to it");
        }
        finally
        {
            await _service!.DeregisterInstanceAsync(serviceName, group, new Instance
            {
                Ip = "127.0.0.1",
                Port = 8080,
                ClusterName = "DEFAULT"
            });
            await _service.DeregisterInstanceAsync(serviceName, group, new Instance
            {
                Ip = "127.0.0.2",
                Port = 8080,
                ClusterName = "DEFAULT"
            });
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task MultipleSubscribers_OnSameService_AllReceivePush()
    {
        var serviceName = $"grpc-fanout-{Guid.NewGuid():N}";
        var group = "DEFAULT_GROUP";

        var tcs1 = new TaskCompletionSource<IInstancesChangeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcs2 = new TaskCompletionSource<IInstancesChangeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        Action<IInstancesChangeEvent> listener1 = evt =>
        {
            if (evt.Instances.Count > 0) tcs1.TrySetResult(evt);
        };
        Action<IInstancesChangeEvent> listener2 = evt =>
        {
            if (evt.Instances.Count > 0) tcs2.TrySetResult(evt);
        };

        try
        {
            await _service!.SubscribeAsync(serviceName, group, new List<string>(), listener1);
            await _service.SubscribeAsync(serviceName, group, new List<string>(), listener2);
            await Task.Delay(1000);

            await _service.RegisterInstanceAsync(serviceName, group, new Instance
            {
                Ip = "127.0.0.1",
                Port = 8080,
                ClusterName = "DEFAULT"
            });

            var all = Task.WhenAll(tcs1.Task, tcs2.Task);
            var winner = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(30)));
            winner.Should().BeSameAs(all, "both subscribers should receive the push within 30s");

            (await tcs1.Task).Instances.Should().HaveCount(1);
            (await tcs2.Task).Instances.Should().HaveCount(1);
        }
        finally
        {
            if (_service is not null)
            {
                await _service.UnsubscribeAsync(serviceName, group, new List<string>(), listener1);
                await _service.UnsubscribeAsync(serviceName, group, new List<string>(), listener2);
                await _service.DeregisterInstanceAsync(serviceName, group, new Instance
                {
                    Ip = "127.0.0.1",
                    Port = 8080,
                    ClusterName = "DEFAULT"
                });
            }
        }
    }
}
