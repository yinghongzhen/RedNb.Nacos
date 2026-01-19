# RedNb.Nacos Samples

This directory contains sample projects demonstrating how to use the RedNb.Nacos SDK.

## Sample Projects

| Project | Description |
|---------|-------------|
| [RedNb.Nacos.Sample.Console](RedNb.Nacos.Sample.Console/) | Console application demonstrating basic SDK usage |
| [RedNb.Nacos.Sample.WebApi](RedNb.Nacos.Sample.WebApi/) | ASP.NET Core WebAPI demonstrating DI integration |

## Prerequisites

- .NET 8.0 or later
- Nacos Server 2.x or 3.x running on `localhost:8848`

### Quick Start with Nacos

You can use Docker Compose to quickly start a Nacos server:

```bash
cd ../deploy/docker-compose
./start.sh   # Linux/macOS
start.bat    # Windows
```

Or run Nacos standalone:

```bash
docker run -d --name nacos -p 8848:8848 -p 9848:9848 \
  -e MODE=standalone \
  -e NACOS_AUTH_ENABLE=true \
  nacos/nacos-server:v2.3.0
```

## Running the Samples

### Console Sample

```bash
cd RedNb.Nacos.Sample.Console
dotnet run
```

This sample demonstrates:
- Creating services using `NacosFactory`
- Publishing and retrieving configurations
- Adding config change listeners
- Registering and discovering service instances
- Weighted random instance selection

### WebAPI Sample

```bash
cd RedNb.Nacos.Sample.WebApi
dotnet run
```

This sample demonstrates:
- Adding Nacos as a configuration source
- Dependency injection with `AddNacos()`
- Health checks with `AddNacos()`
- Automatic service registration with `UseNacosServiceRegistry()`
- Using services in controllers

Access the API:
- Swagger UI: http://localhost:5000/swagger
- Health Check: http://localhost:5000/health

## Key Concepts

### Factory Pattern (Console)

```csharp
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;

var options = new NacosClientOptions
{
    ServerAddresses = "localhost:8848",
    Username = "nacos",
    Password = "nacos"
};

var factory = new NacosFactory();
var configService = factory.CreateConfigService(options);
var namingService = factory.CreateNamingService(options);
```

### Dependency Injection (WebAPI)

```csharp
// Register all Nacos services
builder.Services.AddNacos(options =>
{
    options.ServerAddresses = "localhost:8848";
    options.Username = "nacos";
    options.Password = "nacos";
});

// Or register specific services
builder.Services.AddNacosConfig(options => { /* ... */ });
builder.Services.AddNacosNaming(options => { /* ... */ });
```

### Configuration Source

```csharp
builder.Configuration.AddNacosConfiguration(source =>
{
    source.Options.ServerAddresses = "localhost:8848";
    source.ConfigItems.Add(new NacosConfigurationItem 
    { 
        DataId = "app-config", 
        Group = "DEFAULT_GROUP" 
    });
});
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddNacos();

app.MapHealthChecks("/health");
```

### Automatic Service Registration

```csharp
app.UseNacosServiceRegistry(
    serviceName: "my-service",
    port: 5000,
    metadata: new Dictionary<string, string>
    {
        { "version", "1.0.0" }
    });
```

## Troubleshooting

### Connection Failed

1. Ensure Nacos server is running on `localhost:8848`
2. Check credentials (default: `nacos`/`nacos`)
3. Verify network connectivity

### Config Not Found

1. Create the configuration in Nacos console first, or
2. Use `Optional = true` for optional configurations

### Service Not Registered

1. Ensure `AddNacos()` is called before `UseNacosServiceRegistry()`
2. Check Nacos console for registered instances
