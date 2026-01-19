# RedNb.Nacos WebAPI Sample

An ASP.NET Core WebAPI application demonstrating integration with the RedNb.Nacos SDK.

## Features Demonstrated

- ✅ Adding Nacos as a configuration source
- ✅ Dependency injection with `AddNacos()`
- ✅ Health checks with `AddNacos()`
- ✅ Automatic service registration with `UseNacosServiceRegistry()`
- ✅ Using services in controllers

## Prerequisites

- .NET 8.0 or later
- Nacos Server running on `localhost:8848`

## Running the Sample

```bash
dotnet run
```

Access the application:
- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health

## Sample Code

### Program.cs Setup

```csharp
using RedNb.Nacos.DependencyInjection;
using RedNb.Nacos.AspNetCore.Configuration;
using RedNb.Nacos.AspNetCore.HealthChecks;
using RedNb.Nacos.AspNetCore.ServiceRegistry;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Nacos as configuration source
builder.Configuration.AddNacosConfiguration(source =>
{
    source.Options.ServerAddresses = "localhost:8848";
    source.Options.Username = "nacos";
    source.Options.Password = "nacos";
    source.ConfigItems.Add(new NacosConfigurationItem 
    { 
        DataId = "app-config", 
        Group = "DEFAULT_GROUP" 
    });
});

// 2. Register Nacos services for DI
builder.Services.AddNacos(options =>
{
    options.ServerAddresses = "localhost:8848";
    options.Username = "nacos";
    options.Password = "nacos";
});

// 3. Add health checks
builder.Services.AddHealthChecks()
    .AddNacos();

var app = builder.Build();

// 4. Map health check endpoint
app.MapHealthChecks("/health");

// 5. Automatic service registration
app.UseNacosServiceRegistry(
    serviceName: "sample-webapi",
    port: 5000,
    metadata: new Dictionary<string, string>
    {
        { "version", "1.0.0" },
        { "env", app.Environment.EnvironmentName }
    });

app.Run();
```

### Using Services in Controllers

```csharp
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Core.Lock;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly IConfigService _configService;
    private readonly INamingService _namingService;
    private readonly ILockService _lockService;

    public DemoController(
        IConfigService configService,
        INamingService namingService,
        ILockService lockService)
    {
        _configService = configService;
        _namingService = namingService;
        _lockService = lockService;
    }

    [HttpGet("config/{dataId}")]
    public async Task<ActionResult<string>> GetConfig(string dataId)
    {
        var content = await _configService.GetConfigAsync(dataId, "DEFAULT_GROUP", 5000);
        return content ?? NotFound();
    }

    [HttpGet("instances/{serviceName}")]
    public async Task<ActionResult<List<Instance>>> GetInstances(string serviceName)
    {
        var instances = await _namingService.GetAllInstancesAsync(serviceName);
        return instances;
    }

    [HttpPost("lock/{key}")]
    public async Task<ActionResult<bool>> AcquireLock(string key)
    {
        var lockInstance = LockInstance.Create(key)
            .WithExpireTime(TimeSpan.FromSeconds(30));
        var acquired = await _lockService.LockAsync(lockInstance);
        return acquired;
    }
}
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/swagger` | Swagger UI documentation |
| GET | `/health` | Health check endpoint |
| GET | `/Demo/config/{dataId}` | Get configuration by dataId |
| GET | `/Demo/instances/{serviceName}` | Get service instances |
| POST | `/Demo/lock/{key}` | Acquire distributed lock |

## Configuration

### appsettings.json

```json
{
  "Nacos": {
    "ServerAddresses": "localhost:8848",
    "Username": "nacos",
    "Password": "nacos",
    "Namespace": ""
  }
}
```

You can also load configuration from Nacos dynamically using `AddNacosConfiguration()`.

## Health Check Response

```json
{
  "status": "Healthy",
  "results": {
    "nacos": {
      "status": "Healthy",
      "description": "Nacos server is healthy"
    }
  }
}
```

## Service Registration

When the application starts, it automatically:
1. Registers itself to Nacos with the specified service name
2. Reports health status periodically
3. Deregisters on graceful shutdown

Check the Nacos console to see the registered instance:
- Service: `sample-webapi`
- Group: `DEFAULT_GROUP`
- Instance: Your local IP:5000

## Troubleshooting

### Service Not Registering

1. Ensure `AddNacos()` is called in `ConfigureServices`
2. Check if `UseNacosServiceRegistry()` is called after `Build()`
3. Verify Nacos server connectivity

### Configuration Not Loading

1. Create the configuration in Nacos console first
2. Verify dataId and group match exactly
3. Use `Optional = true` for optional configurations

### Health Check Failing

1. Check Nacos server status
2. Verify credentials are correct
3. Check network connectivity to Nacos server
