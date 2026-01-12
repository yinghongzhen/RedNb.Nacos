using FluentAssertions;
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Core.Naming.Selector;
using Xunit;
using Xunit.Abstractions;

namespace RedNb.Nacos.IntegrationTests;

/// <summary>
/// Integration tests for NamingSelector features.
/// Requires a running Nacos server at localhost:8848.
/// </summary>
[Collection("NacosIntegration")]
public class NamingSelectorIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private INamingService? _namingService;
    private readonly NacosClientOptions _options;
    private readonly NacosFactory _factory;

    public NamingSelectorIntegrationTests(ITestOutputHelper output)
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
    public async Task SubscribeWithLabelSelector_ShouldFilterByLabels()
    {
        // Arrange
        var serviceName = $"test-service-selector-{Guid.NewGuid():N}";
        var instance1 = new Instance
        {
            Ip = "192.168.1.200",
            Port = 8400,
            Metadata = new Dictionary<string, string>
            {
                { "env", "production" },
                { "version", "v1" }
            }
        };
        var instance2 = new Instance
        {
            Ip = "192.168.1.201",
            Port = 8401,
            Metadata = new Dictionary<string, string>
            {
                { "env", "staging" },
                { "version", "v1" }
            }
        };

        var receivedEvents = new List<IInstancesChangeEvent>();
        var selector = new LabelSelector(new Dictionary<string, string> { { "env", "production" } });

        Action<IInstancesChangeEvent> callback = evt =>
        {
            _output.WriteLine($"Received {evt.Instances?.Count ?? 0} instances");
            receivedEvents.Add(evt);
        };

        try
        {
            // Subscribe with selector
            await _namingService!.SubscribeAsync(serviceName, selector, callback);

            // Register instances
            await _namingService.RegisterInstanceAsync(serviceName, instance1);
            await _namingService.RegisterInstanceAsync(serviceName, instance2);
            await Task.Delay(2000);

            // Get all instances and apply selector
            var allInstances = await _namingService.GetAllInstancesAsync(serviceName);
            _output.WriteLine($"Total instances: {allInstances.Count}");

            var context = new NamingContext
            {
                ServiceName = serviceName,
                Instances = allInstances
            };
            var filteredResult = selector.Select(context);

            // Assert
            filteredResult.Count.Should().Be(1);
            filteredResult.Instances[0].Ip.Should().Be("192.168.1.200");
        }
        finally
        {
            await _namingService!.UnsubscribeAsync(serviceName, selector, callback);
            await _namingService.DeregisterInstanceAsync(serviceName, instance1);
            await _namingService.DeregisterInstanceAsync(serviceName, instance2);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ClusterSelector_ShouldFilterByCluster()
    {
        // Arrange
        var serviceName = $"test-service-cluster-{Guid.NewGuid():N}";
        var instance1 = new Instance
        {
            Ip = "192.168.1.210",
            Port = 8500,
            ClusterName = "cluster-a"
        };
        var instance2 = new Instance
        {
            Ip = "192.168.1.211",
            Port = 8501,
            ClusterName = "cluster-b"
        };

        try
        {
            // Register instances to different clusters
            await _namingService!.RegisterInstanceAsync(serviceName, instance1);
            await _namingService.RegisterInstanceAsync(serviceName, instance2);
            await Task.Delay(2000);

            // Get instances from specific cluster
            var clusterAInstances = await _namingService.GetAllInstancesAsync(
                serviceName, new List<string> { "cluster-a" });
            
            var clusterBInstances = await _namingService.GetAllInstancesAsync(
                serviceName, new List<string> { "cluster-b" });

            // Assert
            _output.WriteLine($"Cluster-a instances: {clusterAInstances.Count}");
            _output.WriteLine($"Cluster-b instances: {clusterBInstances.Count}");

            clusterAInstances.Should().HaveCount(1);
            clusterAInstances[0].Ip.Should().Be("192.168.1.210");

            clusterBInstances.Should().HaveCount(1);
            clusterBInstances[0].Ip.Should().Be("192.168.1.211");
        }
        finally
        {
            await _namingService!.DeregisterInstanceAsync(serviceName, instance1);
            await _namingService!.DeregisterInstanceAsync(serviceName, instance2);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompositeSelector_ShouldCombineFilters()
    {
        // Arrange
        var serviceName = $"test-service-composite-{Guid.NewGuid():N}";
        var instances = new[]
        {
            new Instance
            {
                Ip = "192.168.1.220",
                Port = 8600,
                ClusterName = "cluster-a",
                Metadata = new Dictionary<string, string> { { "env", "production" } }
            },
            new Instance
            {
                Ip = "192.168.1.221",
                Port = 8601,
                ClusterName = "cluster-a",
                Metadata = new Dictionary<string, string> { { "env", "staging" } }
            },
            new Instance
            {
                Ip = "192.168.1.222",
                Port = 8602,
                ClusterName = "cluster-b",
                Metadata = new Dictionary<string, string> { { "env", "production" } }
            }
        };

        try
        {
            // Register all instances
            foreach (var instance in instances)
            {
                await _namingService!.RegisterInstanceAsync(serviceName, instance);
            }
            await Task.Delay(2000);

            // Get all instances
            var allInstances = await _namingService!.GetAllInstancesAsync(serviceName);
            _output.WriteLine($"Total instances: {allInstances.Count}");

            // Create composite selector: cluster-a AND env=production
            var compositeSelector = CompositeSelector.Builder()
                .WithClusters("cluster-a")
                .WithLabels(new Dictionary<string, string> { { "env", "production" } })
                .Build();

            var context = new NamingContext
            {
                ServiceName = serviceName,
                Instances = allInstances
            };
            var result = compositeSelector.Select(context);

            // Assert
            _output.WriteLine($"Filtered instances: {result.Count}");
            result.Count.Should().Be(1);
            result.Instances[0].Ip.Should().Be("192.168.1.220");
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
    public async Task GetServicesOfServer_WithSelector_ShouldWork()
    {
        // Arrange
        var serviceName = $"test-service-list-selector-{Guid.NewGuid():N}";
        var instance = new Instance { Ip = "192.168.1.230", Port = 8700 };

        try
        {
            await _namingService!.RegisterInstanceAsync(serviceName, instance);
            await Task.Delay(3000);

            // Act - Get services with label selector
            var labelSelector = new LabelSelector(new Dictionary<string, string>());
            var services = await _namingService.GetServicesOfServerAsync(1, 100, labelSelector);

            // Assert
            _output.WriteLine($"Found {services.Count} services with selector");
            services.Should().NotBeNull();
        }
        finally
        {
            await _namingService!.DeregisterInstanceAsync(serviceName, instance);
        }
    }
}
