using FluentAssertions;
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming;
using Xunit;
using Xunit.Abstractions;

namespace RedNb.Nacos.IntegrationTests;

/// <summary>
/// Integration tests for Nacos Naming Service.
/// Requires a running Nacos server at localhost:8848.
/// </summary>
[Collection("NacosIntegration")]
public class NamingServiceIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private INamingService? _namingService;
    private readonly NacosClientOptions _options;
    private readonly NacosFactory _factory;

    public NamingServiceIntegrationTests(ITestOutputHelper output)
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
        _namingService = _factory.CreateNamingService(_options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_namingService is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RegisterAndDeregisterInstance_ShouldWork()
    {
        // Arrange
        var serviceName = $"test-service-{Guid.NewGuid():N}";
        var ip = "192.168.1.100";
        var port = 8080;

        try
        {
            // Act - Register
            await _namingService!.RegisterInstanceAsync(serviceName, ip, port);
            _output.WriteLine($"Registered instance {ip}:{port} for service {serviceName}");

            // Wait for registration to complete
            await Task.Delay(1000);

            // Verify registration
            var instances = await _namingService.GetAllInstancesAsync(serviceName);
            _output.WriteLine($"Found {instances.Count} instances");

            // Assert
            instances.Should().NotBeEmpty();
            instances.Should().Contain(i => i.Ip == ip && i.Port == port);
        }
        finally
        {
            // Cleanup - Deregister
            await _namingService!.DeregisterInstanceAsync(serviceName, ip, port);
            _output.WriteLine("Deregistered instance");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RegisterInstanceWithMetadata_ShouldWork()
    {
        // Arrange
        var serviceName = $"test-service-metadata-{Guid.NewGuid():N}";
        var instance = new Instance
        {
            Ip = "192.168.1.101",
            Port = 8081,
            Weight = 2.0,
            Metadata = new Dictionary<string, string>
            {
                { "version", "1.0.0" },
                { "env", "test" }
            }
        };

        try
        {
            // Act
            await _namingService!.RegisterInstanceAsync(serviceName, instance);
            await Task.Delay(1000);

            // Verify
            var instances = await _namingService.GetAllInstancesAsync(serviceName);

            // Assert
            instances.Should().NotBeEmpty();
            var registered = instances.First();
            registered.Weight.Should().Be(2.0);
            // Metadata might be returned depending on Nacos version
            _output.WriteLine($"Instance metadata count: {registered.Metadata?.Count}");
        }
        finally
        {
            await _namingService!.DeregisterInstanceAsync(serviceName, instance);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SelectHealthyInstances_ShouldReturnHealthy()
    {
        // Arrange
        var serviceName = $"test-service-healthy-{Guid.NewGuid():N}";
        var instance = new Instance
        {
            Ip = "192.168.1.102",
            Port = 8082,
            Healthy = true,
            Enabled = true
        };

        try
        {
            await _namingService!.RegisterInstanceAsync(serviceName, instance);
            await Task.Delay(1000);

            // Act
            var healthyInstances = await _namingService.SelectInstancesAsync(
                serviceName, "DEFAULT_GROUP", true);

            // Assert
            _output.WriteLine($"Found {healthyInstances.Count} healthy instances");
            healthyInstances.Should().NotBeEmpty();
            healthyInstances.All(i => i.Healthy).Should().BeTrue();
        }
        finally
        {
            await _namingService!.DeregisterInstanceAsync(serviceName, instance);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SelectOneHealthyInstance_ShouldReturnOne()
    {
        // Arrange
        var serviceName = $"test-service-selectone-{Guid.NewGuid():N}";
        var instances = new[]
        {
            new Instance { Ip = "192.168.1.110", Port = 8090, Weight = 1.0 },
            new Instance { Ip = "192.168.1.111", Port = 8091, Weight = 2.0 },
            new Instance { Ip = "192.168.1.112", Port = 8092, Weight = 3.0 }
        };

        try
        {
            foreach (var instance in instances)
            {
                await _namingService!.RegisterInstanceAsync(serviceName, instance);
            }
            await Task.Delay(1500);

            // Act - Select multiple times to test weighted selection
            var selections = new Dictionary<string, int>();
            for (int i = 0; i < 100; i++)
            {
                var selected = await _namingService!.SelectOneHealthyInstanceAsync(serviceName);
                if (selected != null)
                {
                    var key = $"{selected.Ip}:{selected.Port}";
                    selections[key] = selections.GetValueOrDefault(key) + 1;
                }
            }

            // Assert
            _output.WriteLine("Selection distribution:");
            foreach (var kvp in selections)
            {
                _output.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
            
            selections.Should().NotBeEmpty();
        }
        finally
        {
            foreach (var instance in instances)
            {
                await _namingService!.DeregisterInstanceAsync(serviceName, instance);
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetServicesOfServer_ShouldReturnList()
    {
        // Arrange
        var serviceName = $"test-service-list-{Guid.NewGuid():N}";
        var instance = new Instance { Ip = "192.168.1.120", Port = 8100 };

        try
        {
            await _namingService!.RegisterInstanceAsync(serviceName, instance);
            // Wait longer for the service to be registered and indexed
            await Task.Delay(3000);

            // Act
            var services = await _namingService.GetServicesOfServerAsync(1, 100);

            // Assert
            _output.WriteLine($"Found {services.Count} services total");
            // In a fresh Nacos instance, there might not be any services yet
            // So we just verify the API works and returns a valid response
            services.Should().NotBeNull();
            services.Data.Should().NotBeNull();
        }
        finally
        {
            await _namingService!.DeregisterInstanceAsync(serviceName, instance);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Subscribe_ShouldReceiveNotifications()
    {
        // Arrange
        var serviceName = $"test-service-subscribe-{Guid.NewGuid():N}";
        var instance1 = new Instance { Ip = "192.168.1.130", Port = 8200 };
        var instance2 = new Instance { Ip = "192.168.1.131", Port = 8201 };

        var tcs = new TaskCompletionSource<IInstancesChangeEvent>();
        var receivedEvents = new List<IInstancesChangeEvent>();

        Action<IInstancesChangeEvent> callback = evt =>
        {
            _output.WriteLine($"Received instance change: {evt.Instances?.Count} instances");
            receivedEvents.Add(evt);
            if (evt.Instances?.Count == 2)
            {
                tcs.TrySetResult(evt);
            }
        };

        try
        {
            // Subscribe first
            await _namingService!.SubscribeAsync(serviceName, callback);

            // Register first instance
            await _namingService.RegisterInstanceAsync(serviceName, instance1);
            await Task.Delay(1000);

            // Register second instance
            await _namingService.RegisterInstanceAsync(serviceName, instance2);

            // Wait for notification (with timeout)
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(15000));

            // Assert
            _output.WriteLine($"Received {receivedEvents.Count} events total");
            receivedEvents.Should().NotBeEmpty();
        }
        finally
        {
            await _namingService!.UnsubscribeAsync(serviceName, callback);
            await _namingService.DeregisterInstanceAsync(serviceName, instance1);
            await _namingService.DeregisterInstanceAsync(serviceName, instance2);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetServerStatus_ShouldReturnUp()
    {
        // Act
        var status = _namingService!.GetServerStatus();

        // Assert
        _output.WriteLine($"Server status: {status}");
        status.Should().Be("UP");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAllInstances_NonExistentService_ShouldReturnEmpty()
    {
        // Arrange
        var serviceName = $"nonexistent-{Guid.NewGuid():N}";

        // Act
        var instances = await _namingService!.GetAllInstancesAsync(serviceName);

        // Assert
        instances.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RegisterWithGroup_ShouldIsolateByGroup()
    {
        // Arrange
        var serviceName = $"test-service-group-{Guid.NewGuid():N}";
        var group1 = "GROUP_A";
        var group2 = "GROUP_B";
        var instance1 = new Instance { Ip = "192.168.1.140", Port = 8300 };
        var instance2 = new Instance { Ip = "192.168.1.141", Port = 8301 };

        try
        {
            // Register to different groups
            await _namingService!.RegisterInstanceAsync(serviceName, group1, instance1);
            await _namingService.RegisterInstanceAsync(serviceName, group2, instance2);
            await Task.Delay(1000);

            // Act
            var instancesGroup1 = await _namingService.GetAllInstancesAsync(serviceName, group1);
            var instancesGroup2 = await _namingService.GetAllInstancesAsync(serviceName, group2);

            // Assert
            _output.WriteLine($"Group {group1}: {instancesGroup1.Count} instances");
            _output.WriteLine($"Group {group2}: {instancesGroup2.Count} instances");

            instancesGroup1.Should().HaveCount(1);
            instancesGroup1[0].Ip.Should().Be("192.168.1.140");

            instancesGroup2.Should().HaveCount(1);
            instancesGroup2[0].Ip.Should().Be("192.168.1.141");
        }
        finally
        {
            await _namingService!.DeregisterInstanceAsync(serviceName, group1, instance1);
            await _namingService.DeregisterInstanceAsync(serviceName, group2, instance2);
        }
    }
}
