# RedNb.Nacos SDK

A complete .NET 10 SDK for [Nacos](https://nacos.io) - a dynamic service discovery, configuration management and service management platform.

## Features

- **Configuration Management**: Get, publish, update, and delete configurations with listener support for real-time updates
- **Service Discovery**: Register, deregister, and discover service instances with health checks
- **Multiple Protocols**: Support for both HTTP and gRPC communication
- **High Availability**: Built-in failover, retry mechanisms, and server health tracking
- **Dependency Injection**: First-class support for ASP.NET Core DI
- **Local Caching**: Automatic local caching of configurations and service info

## Quick Start

### Installation

```bash
# Add the packages (after publishing to NuGet)
dotnet add package RedNb.Nacos.Client
# For gRPC support:
dotnet add package RedNb.Nacos.GrpcClient
```

### Basic Usage

#### Configuration Service

```csharp
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;

// Create options
var options = new NacosClientOptions
{
    ServerAddresses = "localhost:8848",
    Username = "nacos",
    Password = "nacos",
    Namespace = ""
};

// Create config service
var configService = NacosFactory.CreateConfigService(options);

// Get config
var content = await configService.GetConfigAsync("dataId", "DEFAULT_GROUP", 5000);

// Publish config
await configService.PublishConfigAsync("dataId", "DEFAULT_GROUP", "content");

// Listen for changes
await configService.AddListenerAsync("dataId", "DEFAULT_GROUP", new MyConfigListener());

// Remove config
await configService.RemoveConfigAsync("dataId", "DEFAULT_GROUP");
```

#### Naming Service

```csharp
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming.Models;

// Create naming service
var namingService = NacosFactory.CreateNamingService(options);

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
await namingService.RegisterInstanceAsync("my-service", instance);

// Get all instances
var instances = await namingService.GetAllInstancesAsync("my-service");

// Select one healthy instance (weighted random)
var selected = await namingService.SelectOneHealthyInstanceAsync("my-service");

// Subscribe to changes
namingService.Subscribe("my-service", evt =>
{
    Console.WriteLine($"Service changed: {evt.Hosts?.Count} instances");
});

// Deregister
await namingService.DeregisterInstanceAsync("my-service", instance);
```

### ASP.NET Core Integration

#### Using Dependency Injection

```csharp
// In Program.cs
builder.Services.AddNacos(options =>
{
    options.ServerAddresses = "localhost:8848";
    options.Username = "nacos";
    options.Password = "nacos";
});

// In a controller or service
public class MyController : ControllerBase
{
    private readonly IConfigService _configService;
    private readonly INamingService _namingService;

    public MyController(IConfigService configService, INamingService namingService)
    {
        _configService = configService;
        _namingService = namingService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var config = await _configService.GetConfigAsync("dataId", "group", 5000);
        return Ok(config);
    }
}
```

### Using gRPC Client

```csharp
using RedNb.Nacos.GrpcClient;
using RedNb.Nacos.Core;

// Create gRPC services
var configService = await NacosGrpcFactory.CreateConfigServiceAsync(options);
var namingService = await NacosGrpcFactory.CreateNamingServiceAsync(options);

// Or use DI
builder.Services.AddNacosGrpc(options =>
{
    options.ServerAddresses = "localhost:8848";
    options.Username = "nacos";
    options.Password = "nacos";
});
```

## Configuration Options

| Property | Description | Default |
|----------|-------------|---------|
| `ServerAddresses` | Comma-separated Nacos server addresses | Required |
| `Username` | Username for authentication | null |
| `Password` | Password for authentication | null |
| `Namespace` | Namespace/Tenant ID | "" (public) |
| `EnableTls` | Enable HTTPS/TLS | false |
| `DefaultTimeout` | Default request timeout (ms) | 5000 |
| `LongPollTimeout` | Long polling timeout (ms) | 30000 |
| `RetryCount` | Number of retry attempts | 3 |

## Project Structure

```
RedNb.Nacos/
├── src/
│   ├── RedNb.Nacos.Core/           # Core interfaces and models
│   ├── RedNb.Nacos.Client/         # HTTP client implementation
│   └── RedNb.Nacos.GrpcClient/     # gRPC client implementation
├── tests/
│   ├── RedNb.Nacos.Core.Tests/     # Unit tests for Core
│   ├── RedNb.Nacos.Client.Tests/   # Unit tests for Client
│   └── RedNb.Nacos.IntegrationTests/  # Integration tests
└── samples/
    ├── RedNb.Nacos.Demo.Console/   # Console demo application
    └── RedNb.Nacos.Demo.WebApi/    # ASP.NET Core Web API demo
```

## Building

```bash
cd RedNb.Nacos
dotnet restore
dotnet build
```

## Running Tests

```bash
# Unit tests
dotnet test tests/RedNb.Nacos.Core.Tests
dotnet test tests/RedNb.Nacos.Client.Tests

# Integration tests (requires running Nacos server)
dotnet test tests/RedNb.Nacos.IntegrationTests
```

## Running Demos

### Console Demo

```bash
cd samples/RedNb.Nacos.Demo.Console
dotnet run
```

### Web API Demo

```bash
cd samples/RedNb.Nacos.Demo.WebApi
dotnet run
```

Then access Swagger UI at: `https://localhost:5001/swagger`

## Requirements

- .NET 10.0 or later
- Nacos Server 2.x or 3.x

## Nacos Server

To run Nacos locally using Docker:

```bash
docker run --name nacos-standalone -e MODE=standalone \
  -e NACOS_AUTH_ENABLE=true \
  -e NACOS_AUTH_TOKEN=SecretKey012345678901234567890123456789012345678901234567890123456789 \
  -e NACOS_AUTH_IDENTITY_KEY=serverIdentity \
  -e NACOS_AUTH_IDENTITY_VALUE=security \
  -p 8848:8848 -p 9848:9848 -d nacos/nacos-server:v3.1.1
```

Default credentials: `nacos` / `nacos`

## License

MIT

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
