# RedNb.Nacos Console Sample

A console application demonstrating basic usage of the RedNb.Nacos SDK.

## Features Demonstrated

- ✅ Creating services using `NacosFactory`
- ✅ Publishing and retrieving configurations
- ✅ Adding config change listeners
- ✅ Registering service instances
- ✅ Discovering service instances
- ✅ Weighted random instance selection
- ✅ Subscribing to service changes

## Prerequisites

- .NET 8.0 or later
- Nacos Server running on `localhost:8848`

## Running the Sample

```bash
dotnet run
```

## Sample Code

### Creating Services

```csharp
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;

var options = new NacosClientOptions
{
    ServerAddresses = "localhost:8848",
    Username = "nacos",
    Password = "nacos",
    Namespace = "",
    DefaultTimeout = 5000
};

// Create factory with optional logging
var factory = new NacosFactory(loggerFactory);

// Create services
var configService = factory.CreateConfigService(options);
var namingService = factory.CreateNamingService(options);
```

### Configuration Management

```csharp
// Publish config
await configService.PublishConfigAsync("demo-config", "DEFAULT_GROUP", content, ConfigType.Json);

// Get config
var content = await configService.GetConfigAsync("demo-config", "DEFAULT_GROUP", 5000);

// Listen for changes
await configService.AddListenerAsync("demo-config", "DEFAULT_GROUP", new MyConfigListener());

// Remove config
await configService.RemoveConfigAsync("demo-config", "DEFAULT_GROUP");
```

### Service Discovery

```csharp
// Register instance
var instance = new Instance
{
    Ip = "192.168.1.100",
    Port = 8080,
    Weight = 1.0,
    Healthy = true,
    Ephemeral = true,
    Metadata = new Dictionary<string, string>
    {
        { "version", "1.0.0" }
    }
};
await namingService.RegisterInstanceAsync("demo-service", instance);

// Get all instances
var instances = await namingService.GetAllInstancesAsync("demo-service");

// Select one healthy instance (weighted random)
var selected = await namingService.SelectOneHealthyInstanceAsync("demo-service");

// Subscribe to service changes
await namingService.SubscribeAsync("demo-service", evt =>
{
    Console.WriteLine($"Service changed: {evt.Instances?.Count} instances");
});

// Deregister instance
await namingService.DeregisterInstanceAsync("demo-service", instance);
```

### Config Change Listener

```csharp
class MyConfigListener : IConfigChangeListener
{
    public void OnReceiveConfigInfo(ConfigInfo configInfo)
    {
        Console.WriteLine($"Config changed: {configInfo.DataId}");
        Console.WriteLine($"New content: {configInfo.Content}");
        Console.WriteLine($"MD5: {configInfo.Md5}");
    }
}
```

## Interactive Mode

After initial setup, the sample enters interactive mode:

- Press **Enter** to update the configuration
- Press **Q** then Enter to quit

Each config update triggers the listener, demonstrating real-time config change notifications.

## Output Example

```
===========================================
  RedNb.Nacos SDK Console Sample
===========================================

Connecting to Nacos server at: localhost:8848

--- Config Service Demo ---

Publishing config: demo-config@DEFAULT_GROUP
Publish result: SUCCESS

Getting config: demo-config@DEFAULT_GROUP
Retrieved content:
{
    "app": {
        "name": "RedNb.Nacos.Sample",
        "version": "1.0.0"
    }
}

--- Naming Service Demo ---

Registering instance: 192.168.1.100:8080 for service: demo-service
Instance registered successfully!

Getting all instances for service: demo-service
Found 1 instance(s):
  - 192.168.1.100:8080 (Weight: 1, Healthy: True)

--- Interactive Mode ---
Press Enter to update config, 'q' to quit...
```

## Related Files

- [Program.cs](Program.cs) - Main application entry point
- [ConfigHotReloadTest.cs](ConfigHotReloadTest.cs) - Additional hot reload testing
