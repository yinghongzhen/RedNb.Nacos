using Microsoft.Extensions.Logging;
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Naming;

Console.WriteLine("===========================================");
Console.WriteLine("  RedNb.Nacos SDK Console Sample");
Console.WriteLine("===========================================");
Console.WriteLine();

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Configuration
var options = new NacosClientOptions
{
    ServerAddresses = "localhost:8848",
    Username = "nacos",
    Password = "nacos",
    Namespace = "",
    EnableGrpc = false,
    DefaultTimeout = 5000
};

Console.WriteLine($"Connecting to Nacos server at: {options.ServerAddresses}");
Console.WriteLine();

// Create services using factory with logger
var factory = new NacosFactory(loggerFactory);
var configService = factory.CreateConfigService(options);
var namingService = factory.CreateNamingService(options);

try
{
    // ===== Config Service Demo =====
    Console.WriteLine("--- Config Service Demo ---");
    Console.WriteLine();

    // 1. Publish a config
    var dataId = "demo-config";
    var group = "DEFAULT_GROUP";
    var content = """
    {
        "app": {
            "name": "RedNb.Nacos.Sample",
            "version": "1.0.0",
            "settings": {
                "timeout": 30000,
                "retryCount": 3,
                "enableCache": true
            }
        },
        "database": {
            "connectionString": "Server=localhost;Database=demo;",
            "maxPoolSize": 100
        }
    }
    """;

    Console.WriteLine($"Publishing config: {dataId}@{group}");
    var publishResult = await configService.PublishConfigAsync(dataId, group, content, ConfigType.Json);
    Console.WriteLine($"Publish result: {(publishResult ? "SUCCESS" : "FAILED")}");
    Console.WriteLine();

    // Wait for config to be saved
    await Task.Delay(500);

    // 2. Get the config
    Console.WriteLine($"Getting config: {dataId}@{group}");
    var retrievedContent = await configService.GetConfigAsync(dataId, group, 5000);
    Console.WriteLine($"Retrieved content:");
    Console.WriteLine(retrievedContent);
    Console.WriteLine();

    // 3. Add a listener for config changes
    Console.WriteLine("Adding config change listener...");
    var listener = new DemoConfigListener();
    await configService.AddListenerAsync(dataId, group, listener);
    Console.WriteLine("Listener added. Config changes will be logged.");
    Console.WriteLine();

    // ===== Naming Service Demo =====
    Console.WriteLine("--- Naming Service Demo ---");
    Console.WriteLine();

    // 1. Register a service instance
    var serviceName = "demo-service";
    var instance = new Instance
    {
        Ip = "192.168.1.100",
        Port = 8080,
        Weight = 1.0,
        Healthy = true,
        Enabled = true,
        Ephemeral = true,
        Metadata = new Dictionary<string, string>
        {
            { "version", "1.0.0" },
            { "env", "demo" }
        }
    };

    Console.WriteLine($"Registering instance: {instance.Ip}:{instance.Port} for service: {serviceName}");
    await namingService.RegisterInstanceAsync(serviceName, instance);
    Console.WriteLine("Instance registered successfully!");
    Console.WriteLine();

    // Wait for registration
    await Task.Delay(1000);

    // 2. Register another instance
    var instance2 = new Instance
    {
        Ip = "192.168.1.101",
        Port = 8081,
        Weight = 2.0,
        Healthy = true,
        Enabled = true,
        Ephemeral = true,
        Metadata = new Dictionary<string, string>
        {
            { "version", "1.0.1" },
            { "env", "demo" }
        }
    };

    Console.WriteLine($"Registering instance: {instance2.Ip}:{instance2.Port} for service: {serviceName}");
    await namingService.RegisterInstanceAsync(serviceName, instance2);
    Console.WriteLine("Instance registered successfully!");
    Console.WriteLine();

    await Task.Delay(1000);

    // 3. Get all instances
    Console.WriteLine($"Getting all instances for service: {serviceName}");
    var instances = await namingService.GetAllInstancesAsync(serviceName);
    Console.WriteLine($"Found {instances.Count} instance(s):");
    foreach (var inst in instances)
    {
        Console.WriteLine($"  - {inst.Ip}:{inst.Port} (Weight: {inst.Weight}, Healthy: {inst.Healthy})");
    }
    Console.WriteLine();

    // 4. Select healthy instances
    Console.WriteLine("Selecting healthy instances...");
    var healthyInstances = await namingService.SelectInstancesAsync(serviceName, "DEFAULT_GROUP", true);
    Console.WriteLine($"Healthy instances: {healthyInstances.Count}");
    Console.WriteLine();

    // 5. Select one healthy instance (weighted random)
    Console.WriteLine("Testing weighted random selection (10 times):");
    var selectionCounts = new Dictionary<string, int>();
    for (int i = 0; i < 10; i++)
    {
        var selected = await namingService.SelectOneHealthyInstanceAsync(serviceName);
        if (selected != null)
        {
            var key = $"{selected.Ip}:{selected.Port}";
            selectionCounts[key] = selectionCounts.GetValueOrDefault(key) + 1;
        }
    }
    foreach (var kvp in selectionCounts)
    {
        Console.WriteLine($"  {kvp.Key}: {kvp.Value} times");
    }
    Console.WriteLine();

    // 6. Subscribe to service changes
    Console.WriteLine("Subscribing to service changes...");
    await namingService.SubscribeAsync(serviceName, evt =>
    {
        Console.WriteLine($"[SERVICE CHANGE] {evt.ServiceName}@{evt.GroupName}: {evt.Instances?.Count} instances");
    });
    Console.WriteLine("Subscribed. Service changes will be logged.");
    Console.WriteLine();

    // 7. Get services list
    Console.WriteLine("Getting services from server...");
    var services = await namingService.GetServicesOfServerAsync(1, 10);
    Console.WriteLine($"Services count: {services.Count}");
    if (services.Data != null)
    {
        foreach (var svc in services.Data.Take(5))
        {
            Console.WriteLine($"  - {svc}");
        }
        if (services.Count > 5)
        {
            Console.WriteLine($"  ... and {services.Count - 5} more");
        }
    }
    Console.WriteLine();

    // ===== Server Status =====
    Console.WriteLine("--- Server Status ---");
    Console.WriteLine($"Config Service Status: {configService.GetServerStatus()}");
    Console.WriteLine($"Naming Service Status: {namingService.GetServerStatus()}");
    Console.WriteLine();

    // ===== Interactive Mode =====
    Console.WriteLine("--- Interactive Mode ---");
    Console.WriteLine("Press Enter to update config, 'q' to quit...");
    
    var updateCount = 0;
    while (true)
    {
        var key = Console.ReadLine();
        if (key?.ToLower() == "q")
        {
            break;
        }

        updateCount++;
        var newContent = $$"""
        {
            "app": {
                "name": "RedNb.Nacos.Sample",
                "version": "1.0.{{updateCount}}",
                "updateTime": "{{DateTime.Now:yyyy-MM-dd HH:mm:ss}}"
            }
        }
        """;

        Console.WriteLine($"Updating config (version 1.0.{updateCount})...");
        await configService.PublishConfigAsync(dataId, group, newContent, ConfigType.Json);
        Console.WriteLine("Config updated. Waiting for listener notification...");
        Console.WriteLine();
    }

    // Cleanup
    Console.WriteLine();
    Console.WriteLine("--- Cleanup ---");
    
    Console.WriteLine("Deregistering instances...");
    await namingService.DeregisterInstanceAsync(serviceName, instance);
    await namingService.DeregisterInstanceAsync(serviceName, instance2);
    Console.WriteLine("Instances deregistered.");

    Console.WriteLine("Removing config...");
    await configService.RemoveConfigAsync(dataId, group);
    Console.WriteLine("Config removed.");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
finally
{
    // Dispose services
    if (configService is IAsyncDisposable configDisposable)
    {
        await configDisposable.DisposeAsync();
    }
    if (namingService is IAsyncDisposable namingDisposable)
    {
        await namingDisposable.DisposeAsync();
    }
}

Console.WriteLine();
Console.WriteLine("Sample completed. Press any key to exit...");
Console.ReadKey();

// Config change listener implementation
class DemoConfigListener : IConfigChangeListener
{
    public void OnReceiveConfigInfo(ConfigInfo configInfo)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("[CONFIG CHANGED]");
        Console.WriteLine($"  DataId: {configInfo.DataId}");
        Console.WriteLine($"  Group: {configInfo.Group}");
        Console.WriteLine($"  MD5: {configInfo.Md5}");
        Console.WriteLine($"  Content:");
        Console.WriteLine(configInfo.Content);
        Console.WriteLine("========================================");
        Console.WriteLine();
    }
}
