<div align="center">

# RedNb.Nacos

**A Modern .NET SDK for Nacos**

[![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos)
[![NuGet Downloads](https://img.shields.io/nuget/dt/RedNb.Nacos.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos)
[![.NET](https://img.shields.io/badge/.NET-8.0%20|%2010.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-green.svg?style=flat-square)](LICENSE)
[![Nacos](https://img.shields.io/badge/Nacos-2.x%20|%203.x-00C7B7.svg?style=flat-square)](https://nacos.io/)
[![GitHub stars](https://img.shields.io/github/stars/redNb/RedNb.Nacos?style=flat-square)](https://github.com/redNb/RedNb.Nacos/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/redNb/RedNb.Nacos?style=flat-square)](https://github.com/redNb/RedNb.Nacos/issues)

English | [简体中文](README.md)

</div>

---

**RedNb.Nacos** is a fully-featured modern .NET Nacos client SDK, fully compatible with Nacos 2.x/3.x, providing **200+ API methods** covering configuration management, service discovery, distributed locks, AI services (MCP/A2A), and operations management.

> 🎯 **Why Choose RedNb.Nacos?**
> - 🆕 Supports **.NET 8.0** and **.NET 10.0** with latest language features
> - ✅ **Most Complete API Coverage** - 200+ APIs covering all Nacos features
> - 🔄 Full support for **Nacos 3.x** new features (Fuzzy Watch, AI Service, Distributed Lock)
> - 🛠️ Exclusive **Maintainer Service API** - Namespace, cluster, client connection management
> - 📦 Modular design, import on demand, reduce dependencies

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🚀 **High Performance** | Supports both HTTP and gRPC protocols |
| 📦 **Modular Design** | Import on demand, flexible composition |
| 🔄 **Nacos 2.x/3.x Compatible** | Full support for Fuzzy Watch, AI Service, Distributed Lock |
| 🔒 **Distributed Lock** | Native Nacos 3.0 distributed lock support |
| 🤖 **AI Service** | Supports MCP (Model Context Protocol) and A2A (Agent-to-Agent) protocols |
| 🛠️ **Operations Management** | Complete Maintainer API for namespace, cluster, client management |
| 💉 **Dependency Injection** | Native support for Microsoft.Extensions.DependencyInjection |
| 🏗️ **ASP.NET Core Integration** | Configuration provider, health checks, automatic service registration |
| ⚡ **Async-First** | Fully async API design |
| 📝 **Strongly Typed** | Complete type support and XML documentation |

## 📦 NuGet Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| `RedNb.Nacos` | Core abstractions: interfaces, models, and constants | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos) |
| `RedNb.Nacos.Http` | HTTP client implementation | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.Http.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos.Http) |
| `RedNb.Nacos.Grpc` | gRPC high-performance client implementation | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.Grpc.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos.Grpc) |
| `RedNb.Nacos.DependencyInjection` | Dependency injection extensions | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.DependencyInjection.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos.DependencyInjection) |
| `RedNb.Nacos.AspNetCore` | ASP.NET Core integration | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.AspNetCore.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos.AspNetCore) |
| `RedNb.Nacos.All` | All-in-one package (includes all above) | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.All.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos.All) |

## 🚀 Quick Start

### Installation

```bash
# Basic package (recommended)
dotnet add package RedNb.Nacos.Http

# Or all-in-one package
dotnet add package RedNb.Nacos.All

# ASP.NET Core integration
dotnet add package RedNb.Nacos.AspNetCore
```

### Basic Usage

#### 1. Create Services Using Factory

```csharp
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;

// Configure options
var options = new NacosClientOptions
{
    ServerAddresses = "localhost:8848",
    Username = "nacos",
    Password = "nacos",
    Namespace = "",
    DefaultTimeout = 5000
};

// Create service factory
var factory = new NacosFactory();

// Get various services
var configService = factory.CreateConfigService(options);
var namingService = factory.CreateNamingService(options);
var lockService = factory.CreateLockService(options);
var aiService = factory.CreateAiService(options);
var maintainerService = factory.CreateMaintainerService(options);
```

#### 2. Configuration Center

```csharp
// Get configuration
var content = await configService.GetConfigAsync("app-config", "DEFAULT_GROUP", 5000);

// Publish configuration
await configService.PublishConfigAsync("app-config", "DEFAULT_GROUP", jsonContent, ConfigType.Json);

// CAS optimistic lock publish
await configService.PublishConfigCasAsync("app-config", "DEFAULT_GROUP", content, "oldMd5");

// Listen for config changes
await configService.AddListenerAsync("app-config", "DEFAULT_GROUP", new MyConfigListener());

// Fuzzy watch (Nacos 3.0) - watch all configs starting with "app-"
await configService.FuzzyWatchAsync("app-*", "DEFAULT_GROUP", myWatcher);
```

#### 3. Service Discovery

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
await namingService.RegisterInstanceAsync("my-service", instance);

// Batch register
await namingService.BatchRegisterInstanceAsync("my-service", "DEFAULT_GROUP", instances);

// Get all instances
var instances = await namingService.GetAllInstancesAsync("my-service");

// Select one healthy instance (weighted random)
var selected = await namingService.SelectOneHealthyInstanceAsync("my-service");

// Subscribe to service changes
await namingService.SubscribeAsync("my-service", evt =>
{
    Console.WriteLine($"Service changed: {evt.Instances?.Count} instances");
});

// Fuzzy watch (Nacos 3.0)
await namingService.FuzzyWatchAsync("*", "production-group", myServiceWatcher);

// Deregister instance
await namingService.DeregisterInstanceAsync("my-service", instance);
```

#### 4. Distributed Lock (Nacos 3.0)

```csharp
// Create lock instance
var lockInstance = LockInstance.Create("my-lock-key")
    .WithExpireTime(TimeSpan.FromSeconds(30))
    .WithOwner("client-1")
    .WithReentrant();

// Acquire lock
var acquired = await lockService.LockAsync(lockInstance);
if (acquired)
{
    try
    {
        // Execute critical section
        await DoSomethingCritical();
    }
    finally
    {
        // Release lock
        await lockService.UnlockAsync(lockInstance);
    }
}

// Or use TryLock with timeout
var success = await lockService.TryLockAsync(lockInstance, TimeSpan.FromSeconds(10));
```

#### 5. AI Service - MCP/A2A (Nacos 3.0)

```csharp
// === MCP Service ===
// Release MCP server
await aiService.ReleaseMcpServerAsync("my-mcp-server", mcpServerSpec);

// Register MCP endpoint
await aiService.RegisterMcpServerEndpointAsync("my-mcp-server", "1.0.0", endpoint);

// Get MCP server details
var mcpServer = await aiService.GetMcpServerAsync("my-mcp-server");

// Subscribe to MCP server changes
await aiService.SubscribeMcpServerAsync("my-mcp-server", myMcpListener);

// === A2A Service ===
// Release Agent Card
await aiService.ReleaseAgentCardAsync("my-agent", agentCard);

// Register Agent endpoint
await aiService.RegisterAgentEndpointAsync("my-agent", endpoint, TransportProtocol.Http);

// Get Agent Card details
var agentCard = await aiService.GetAgentCardAsync("my-agent");

// List all Agents
var agents = await aiService.ListAgentCardsAsync(1, 20);
```

#### 6. Maintainer Service

```csharp
// === Namespace Management ===
var namespaces = await maintainerService.GetNamespacesAsync();
await maintainerService.CreateNamespaceAsync("new-ns", "New Namespace");
await maintainerService.UpdateNamespaceAsync("ns-id", "Updated Name", "Description");
await maintainerService.DeleteNamespaceAsync("ns-id");

// === Service Management ===
await maintainerService.CreateServiceAsync("my-service", "DEFAULT_GROUP", 0.5f, true, "metadata");
var services = await maintainerService.ListServicesAsync(1, 10, "DEFAULT_GROUP");
var serviceDetail = await maintainerService.GetServiceAsync("my-service", "DEFAULT_GROUP");
await maintainerService.DeleteServiceAsync("my-service", "DEFAULT_GROUP");

// === Instance Management ===
await maintainerService.UpdateInstanceAsync("my-service", instance);
await maintainerService.UpdateInstanceHealthAsync("my-service", "192.168.1.100", 8080, true);
await maintainerService.BatchUpdateMetadataAsync("my-service", instances, metadata);

// === Config Management ===
await maintainerService.PublishConfigAsync("app-config", "DEFAULT_GROUP", content, ConfigType.Yaml);
var configs = await maintainerService.ListConfigsAsync(1, 10, "app", "DEFAULT_GROUP");
var history = await maintainerService.ListConfigHistoryAsync("app-config", "DEFAULT_GROUP", 1, 20);
await maintainerService.DeleteConfigsAsync(new[] { configId1, configId2 });

// === Beta/Gray Config ===
await maintainerService.PublishBetaConfigAsync("app-config", "DEFAULT_GROUP", content, "192.168.1.*");
await maintainerService.StopBetaConfigAsync("app-config", "DEFAULT_GROUP");

// === Config Import/Export ===
var exportData = await maintainerService.ExportConfigsAsync(new[] { configId1, configId2 });
await maintainerService.ImportConfigsAsync("DEFAULT_GROUP", policy, configData);
await maintainerService.CloneConfigsAsync(configIds, targetNamespaceId, policy);

// === Client Connection Management ===
var clients = await maintainerService.ListClientsAsync();
var clientDetail = await maintainerService.GetClientDetailAsync(clientId);
var subscriptions = await maintainerService.GetClientSubscribedServicesAsync(clientId);
var sdkStats = await maintainerService.GetSdkVersionStatisticsAsync();

// === Cluster Management ===
var members = await maintainerService.GetClusterMembersAsync();
var leader = await maintainerService.GetLeaderAsync();
var health = await maintainerService.HealthCheckAsync();
var metrics = await maintainerService.GetMetricsAsync();
```

### ASP.NET Core Integration

#### 1. Add Nacos Configuration Source

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Nacos as configuration source
builder.Configuration.AddNacosConfiguration(source =>
{
    source.Options.ServerAddresses = "localhost:8848";
    source.Options.Username = "nacos";
    source.Options.Password = "nacos";
    source.ConfigItems.Add(new NacosConfigurationItem 
    { 
        DataId = "app-config", 
        Group = "DEFAULT_GROUP",
        ConfigType = "json"
    });
    source.ConfigItems.Add(new NacosConfigurationItem 
    { 
        DataId = "db-config", 
        Group = "DEFAULT_GROUP",
        Optional = true  // Optional config
    });
});
```

#### 2. Dependency Injection

```csharp
// Register Nacos services
builder.Services.AddNacos(options =>
{
    options.ServerAddresses = "localhost:8848";
    options.Username = "nacos";
    options.Password = "nacos";
});

// Or register only config service
builder.Services.AddNacosConfig(options => { /* ... */ });

// Or register only naming service
builder.Services.AddNacosNaming(options => { /* ... */ });

// Add health checks
builder.Services.AddHealthChecks()
    .AddNacos();
```

#### 3. Automatic Service Registration

```csharp
var app = builder.Build();

// Automatically register current service to Nacos, deregister on shutdown
app.UseNacosServiceRegistry(
    serviceName: "my-webapi",
    port: 5000,
    metadata: new Dictionary<string, string>
    {
        { "version", "1.0.0" },
        { "env", app.Environment.EnvironmentName }
    });

app.MapHealthChecks("/health");
app.Run();
```

#### 4. Usage in Controllers

```csharp
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

    [HttpGet("config")]
    public async Task<string?> GetConfig()
    {
        return await _configService.GetConfigAsync("app-config", "DEFAULT_GROUP", 5000);
    }

    [HttpGet("instances")]
    public async Task<List<Instance>> GetInstances(string serviceName)
    {
        return await _namingService.GetAllInstancesAsync(serviceName);
    }

    [HttpPost("lock")]
    public async Task<bool> AcquireLock(string key)
    {
        var lockInstance = LockInstance.Create(key).WithExpireTime(TimeSpan.FromSeconds(30));
        return await _lockService.LockAsync(lockInstance);
    }
}
```

## 🧩 Feature Modules

### 📁 Configuration Center (IConfigService)

| Feature | Method | Description |
|---------|--------|-------------|
| Get Config | `GetConfigAsync()` | Get configuration content |
| Get and Listen | `GetConfigAndSignListenerAsync()` | Get config and register listener |
| Publish Config | `PublishConfigAsync()` | Publish/update configuration |
| CAS Publish | `PublishConfigCasAsync()` | MD5-based optimistic lock update |
| Delete Config | `RemoveConfigAsync()` | Delete configuration |
| Add Listener | `AddListenerAsync()` | Add config change listener |
| Remove Listener | `RemoveListener()` | Remove config change listener |
| Fuzzy Watch | `FuzzyWatchAsync()` | Pattern matching batch watch (Nacos 3.0) |
| Cancel Fuzzy Watch | `CancelFuzzyWatchAsync()` | Cancel fuzzy watch |
| Config Filter | `AddConfigFilter()` | Add config interception filter |
| Server Status | `GetServerStatus()` | Get server health status |

### 🌐 Service Discovery (INamingService)

| Feature | Method | Description |
|---------|--------|-------------|
| **Service Registration** | | |
| Register Instance | `RegisterInstanceAsync()` | Register service instance (multiple overloads) |
| Batch Register | `BatchRegisterInstanceAsync()` | Batch register instances |
| Deregister Instance | `DeregisterInstanceAsync()` | Deregister service instance (multiple overloads) |
| Batch Deregister | `BatchDeregisterInstanceAsync()` | Batch deregister instances |
| **Instance Query** | | |
| Get Instances | `GetAllInstancesAsync()` | Get all instances |
| Get by Cluster | `GetInstancesOfClusterAsync()` | Get instances of specified cluster |
| Select Instances | `SelectInstancesAsync()` | Filter instances by health status |
| Select One | `SelectOneHealthyInstanceAsync()` | Weighted random selection of healthy instance |
| **Service Subscription** | | |
| Subscribe | `SubscribeAsync()` | Subscribe to service change events |
| Unsubscribe | `UnsubscribeAsync()` | Cancel service subscription |
| Fuzzy Watch | `FuzzyWatchAsync()` | Pattern matching batch watch (Nacos 3.0) |
| **Service List** | | |
| Services List | `GetServicesOfServerAsync()` | Paginated service list |
| Subscribed List | `GetSubscribeServicesAsync()` | Get subscribed services list |
| Server Status | `GetServerStatus()` | Get server health status |

### 🔒 Distributed Lock (ILockService)

| Feature | Method | Description |
|---------|--------|-------------|
| Acquire Lock | `LockAsync()` | Acquire distributed lock |
| Release Lock | `UnlockAsync()` | Release distributed lock |
| Try Lock | `TryLockAsync()` | Lock acquisition with timeout |
| Remote Lock | `RemoteLockAsync()` | gRPC remote lock acquisition |
| Remote Unlock | `RemoteUnlockAsync()` | gRPC remote lock release |
| Server Status | `GetServerStatus()` | Get server status |

**LockInstance Fluent API:**

```csharp
var lock = LockInstance.Create("my-key")
    .WithExpireTime(TimeSpan.FromSeconds(30))  // Expiration time
    .WithLockType("distributed")               // Lock type
    .WithNamespace("prod-namespace")           // Namespace
    .WithOwner("service-A")                    // Owner
    .WithParam("priority", "high")             // Custom parameters
    .WithReentrant();                          // Reentrant
```

### 🤖 AI Service (IAiService) - Nacos 3.0

#### MCP Service (Model Context Protocol)

| Feature | Method | Description |
|---------|--------|-------------|
| Get Server | `GetMcpServerAsync()` | Get MCP server details |
| Release Server | `ReleaseMcpServerAsync()` | Release MCP server |
| Register Endpoint | `RegisterMcpServerEndpointAsync()` | Register MCP endpoint |
| Deregister Endpoint | `DeregisterMcpServerEndpointAsync()` | Deregister MCP endpoint |
| Subscribe | `SubscribeMcpServerAsync()` | Subscribe to MCP server changes |
| Unsubscribe | `UnsubscribeMcpServerAsync()` | Unsubscribe |
| Delete Server | `DeleteMcpServerAsync()` | Delete MCP server |
| List | `ListMcpServersAsync()` | Paginated list of MCP servers |
| Tool Management | `GetMcpToolSpecAsync()` | Get MCP tool specification |
| Refresh Tools | `RefreshMcpToolsAsync()` | Refresh MCP tools |

#### A2A Service (Agent-to-Agent)

| Feature | Method | Description |
|---------|--------|-------------|
| Get Agent | `GetAgentCardAsync()` | Get Agent Card details |
| Release Agent | `ReleaseAgentCardAsync()` | Release Agent Card |
| Register Endpoint | `RegisterAgentEndpointAsync()` | Register Agent endpoint |
| Batch Register | `BatchRegisterAgentEndpointsAsync()` | Batch register endpoints |
| Deregister Endpoint | `DeregisterAgentEndpointAsync()` | Deregister Agent endpoint |
| Subscribe | `SubscribeAgentCardAsync()` | Subscribe to Agent Card changes |
| Unsubscribe | `UnsubscribeAgentCardAsync()` | Unsubscribe |
| Delete Agent | `DeleteAgentAsync()` | Delete Agent |
| List | `ListAgentCardsAsync()` | Paginated list of Agent Cards |
| Version List | `ListAgentVersionsAsync()` | List Agent versions |

### 🛠️ Maintainer Service (IMaintainerService)

#### Namespace Management (ICoreMaintainer)

| Feature | Method | Description |
|---------|--------|-------------|
| List | `GetNamespacesAsync()` | Get all namespaces |
| Details | `GetNamespaceAsync()` | Get namespace details |
| Create | `CreateNamespaceAsync()` | Create namespace |
| Update | `UpdateNamespaceAsync()` | Update namespace |
| Delete | `DeleteNamespaceAsync()` | Delete namespace |

#### Service Management (IServiceMaintainer)

| Feature | Method | Description |
|---------|--------|-------------|
| Create | `CreateServiceAsync()` | Create service (multiple overloads) |
| Update | `UpdateServiceAsync()` | Update service |
| Delete | `DeleteServiceAsync()` | Delete service |
| Details | `GetServiceAsync()` | Get service details |
| List | `ListServicesAsync()` | Paginated service list |
| Details List | `ListServiceDetailsAsync()` | List services (with details) |
| Subscribers | `GetServiceSubscribersAsync()` | Get service subscribers |
| Selector Types | `ListSelectorTypesAsync()` | List selector types |

#### Instance Management (IInstanceMaintainer)

| Feature | Method | Description |
|---------|--------|-------------|
| Register | `RegisterInstanceAsync()` | Register instance |
| Deregister | `DeregisterInstanceAsync()` | Deregister instance |
| Update | `UpdateInstanceAsync()` | Update instance |
| Partial Update | `PatchInstanceAsync()` | Partial update instance |
| Metadata Update | `BatchUpdateMetadataAsync()` | Batch update metadata |
| Metadata Delete | `BatchDeleteMetadataAsync()` | Batch delete metadata |
| List | `ListInstancesAsync()` | List instances |
| Details | `GetInstanceAsync()` | Get instance details |

#### Config Management (IConfigMaintainer)

| Feature | Method | Description |
|---------|--------|-------------|
| Get | `GetConfigDetailAsync()` | Get config details |
| Publish | `PublishConfigAsync()` | Publish config (multiple overloads) |
| Update Metadata | `UpdateConfigMetadataAsync()` | Update config metadata |
| Delete | `DeleteConfigAsync()` | Delete config |
| Batch Delete | `DeleteConfigsAsync()` | Batch delete configs |
| List | `ListConfigsAsync()` | List configs |
| Search | `SearchConfigsAsync()` | Search configs |
| By Namespace | `GetConfigsByNamespaceAsync()` | Get configs by namespace |
| Listeners | `GetConfigListenersAsync()` | Get config listeners |
| Clone | `CloneConfigAsync()` | Clone config |

#### Config History (IConfigHistoryMaintainer)

| Feature | Method | Description |
|---------|--------|-------------|
| History List | `ListConfigHistoryAsync()` | List config history |
| History Details | `GetConfigHistoryAsync()` | Get history details |
| Previous Version | `GetPreviousConfigHistoryAsync()` | Get previous version history |

#### Beta/Gray Config (IBetaConfigMaintainer)

| Feature | Method | Description |
|---------|--------|-------------|
| Get Beta | `GetBetaConfigAsync()` | Get beta config |
| Publish Beta | `PublishBetaConfigAsync()` | Publish beta config |
| Stop Beta | `StopBetaConfigAsync()` | Stop beta config |
| Get Gray | `GetGrayConfigAsync()` | Get gray config (Nacos 3.0) |
| Publish Gray | `PublishGrayConfigAsync()` | Publish gray config (Nacos 3.0) |
| Delete Gray | `DeleteGrayConfigAsync()` | Delete gray config (Nacos 3.0) |

#### Config Import/Export (IConfigOpsMaintainer)

| Feature | Method | Description |
|---------|--------|-------------|
| Import | `ImportConfigsAsync()` | Import configs |
| Export | `ExportConfigsAsync()` | Export configs |
| Export by ID | `ExportConfigsByIdAsync()` | Export configs by ID |
| Export All | `ExportAllConfigsAsync()` | Export all configs |
| Clone | `CloneConfigsAsync()` | Clone configs to another namespace |
| Clone All | `CloneAllConfigsAsync()` | Clone all configs |

#### Client Management (IClientMaintainer)

| Feature | Method | Description |
|---------|--------|-------------|
| List | `ListClientsAsync()` | List all client connections |
| Naming Clients | `ListNamingClientsAsync()` | List naming service clients |
| Config Clients | `ListConfigClientsAsync()` | List config service clients |
| Details | `GetClientDetailAsync()` | Get client details |
| Subscribed Services | `GetClientSubscribedServicesAsync()` | Get client subscribed services |
| Published Services | `GetClientPublishedServicesAsync()` | Get client published services |
| Listened Configs | `GetClientListenedConfigsAsync()` | Get client listened configs |
| SDK Statistics | `GetSdkVersionStatisticsAsync()` | Get SDK version statistics |
| Node Statistics | `GetCurrentNodeStatisticsAsync()` | Get current node statistics |
| Reload Connections | `ReloadConnectionCountAsync()` | Reload connection count |
| Reset Limits | `ResetConnectionLimitAsync()` | Reset connection limits |

#### Cluster Management (ICoreMaintainer)

| Feature | Method | Description |
|---------|--------|-------------|
| Member List | `GetClusterMembersAsync()` | Get cluster members |
| Current Node | `GetSelfAddressAsync()` | Get current node address |
| Leader | `GetLeaderAsync()` | Get cluster leader |
| Update Lookup | `UpdateMemberLookupAsync()` | Update member lookup address |
| Leave Cluster | `LeaveClusterAsync()` | Leave cluster |
| Server State | `GetServerStateAsync()` | Get server state |
| Switches | `GetServerSwitchesAsync()` | Get server switch config |
| Update Switch | `UpdateServerSwitchAsync()` | Update server switch |
| Readiness | `GetReadinessAsync()` | Get readiness status |
| Liveness | `GetLivenessAsync()` | Get liveness status |
| Health Check | `HealthCheckAsync()` | Health check |
| Metrics | `GetMetricsAsync()` | Get metrics |
| Prometheus | `GetPrometheusMetricsAsync()` | Get Prometheus metrics |
| Raft Leader | `GetRaftLeaderAsync()` | Get Raft leader |
| Transfer Leader | `TransferRaftLeaderAsync()` | Transfer Raft leader |
| Reset Raft | `ResetRaftClusterAsync()` | Reset Raft cluster |

## 📁 Project Structure

```
RedNb.Nacos/
├── src/
│   ├── RedNb.Nacos/                     # Core abstractions
│   │   ├── Config/                      # Config center interfaces and models
│   │   ├── Naming/                      # Service discovery interfaces and models
│   │   ├── Lock/                        # Distributed lock interfaces and models
│   │   ├── Ai/                          # AI service interfaces (MCP/A2A)
│   │   ├── Maintainer/                  # Maintainer service interfaces
│   │   ├── Ability/                     # Ability interfaces
│   │   ├── Failover/                    # Failover
│   │   ├── Constants/                   # Constants
│   │   ├── Exceptions/                  # Exception types
│   │   └── Utils/                       # Utilities
│   ├── RedNb.Nacos.Http/                # HTTP client implementation
│   │   ├── Config/                      # Config service implementation
│   │   ├── Naming/                      # Naming service implementation
│   │   ├── Lock/                        # Lock service implementation
│   │   ├── Ai/                          # AI service implementation
│   │   ├── Maintainer/                  # Maintainer service implementation
│   │   └── Http/                        # HTTP client infrastructure
│   ├── RedNb.Nacos.Grpc/                # gRPC client implementation
│   │   ├── Config/                      # gRPC config service
│   │   ├── Naming/                      # gRPC naming service
│   │   ├── Lock/                        # gRPC lock service
│   │   └── Protos/                      # Protocol Buffer definitions
│   ├── RedNb.Nacos.DependencyInjection/ # DI extensions
│   ├── RedNb.Nacos.AspNetCore/          # ASP.NET Core integration
│   │   ├── Configuration/               # Configuration provider
│   │   ├── HealthChecks/                # Health checks
│   │   └── ServiceRegistry/             # Automatic service registration
│   └── RedNb.Nacos.All/                 # All-in-one package
├── samples/
│   ├── RedNb.Nacos.Sample.Console/      # Console sample
│   └── RedNb.Nacos.Sample.WebApi/       # WebAPI sample
├── tests/
│   ├── RedNb.Nacos.Tests/               # Unit tests
│   ├── RedNb.Nacos.Http.Tests/          # HTTP client tests
│   └── RedNb.Nacos.IntegrationTests/    # Integration tests
└── deploy/
    └── docker-compose/                  # Docker deployment config
```

## ⚙️ Configuration Options

```csharp
public class NacosClientOptions
{
    // ====== Server Connection ======
    /// <summary>Server addresses, comma separated</summary>
    public string ServerAddresses { get; set; } = "localhost:8848";
    
    /// <summary>Namespace ID</summary>
    public string? Namespace { get; set; }
    
    /// <summary>Context path</summary>
    public string ContextPath { get; set; } = "nacos";
    
    /// <summary>Cluster name</summary>
    public string ClusterName { get; set; } = "DEFAULT";
    
    /// <summary>Address server endpoint</summary>
    public string? Endpoint { get; set; }
    
    // ====== Authentication ======
    /// <summary>Username</summary>
    public string? Username { get; set; }
    
    /// <summary>Password</summary>
    public string? Password { get; set; }
    
    /// <summary>Access key</summary>
    public string? AccessKey { get; set; }
    
    /// <summary>Secret key</summary>
    public string? SecretKey { get; set; }
    
    // ====== Timeout Settings ======
    /// <summary>Default timeout in milliseconds, default 5000</summary>
    public int DefaultTimeout { get; set; } = 5000;
    
    /// <summary>Long polling timeout in milliseconds, default 30000</summary>
    public int LongPollTimeout { get; set; } = 30000;
    
    /// <summary>Retry count, default 3</summary>
    public int RetryCount { get; set; } = 3;
    
    // ====== Feature Switches ======
    /// <summary>Enable gRPC, default true</summary>
    public bool EnableGrpc { get; set; } = true;
    
    /// <summary>gRPC port offset, default 1000</summary>
    public int GrpcPortOffset { get; set; } = 1000;
    
    /// <summary>Enable logging, default true</summary>
    public bool EnableLogging { get; set; } = true;
    
    /// <summary>Load cache at start</summary>
    public bool NamingLoadCacheAtStart { get; set; }
    
    /// <summary>Empty push protection</summary>
    public bool NamingPushEmptyProtection { get; set; }
    
    // ====== TLS Settings ======
    /// <summary>Enable TLS</summary>
    public bool EnableTls { get; set; }
    
    /// <summary>TLS certificate path</summary>
    public string? TlsCertPath { get; set; }
    
    /// <summary>TLS key path</summary>
    public string? TlsKeyPath { get; set; }
    
    /// <summary>TLS CA certificate path</summary>
    public string? TlsCaPath { get; set; }
    
    // ====== Other ======
    /// <summary>Application name</summary>
    public string? AppName { get; set; }
}
```

## 🔧 Advanced Features

### Config Listener

```csharp
public class MyConfigListener : IConfigChangeListener
{
    public void OnReceiveConfigInfo(ConfigInfo configInfo)
    {
        Console.WriteLine($"Config changed: {configInfo.DataId}");
        Console.WriteLine($"New content: {configInfo.Content}");
        Console.WriteLine($"MD5: {configInfo.Md5}");
    }
}
```

### Config Filter

```csharp
public class EncryptionFilter : IConfigFilter
{
    public string Name => "EncryptionFilter";
    public int Order => 1;

    public void DoFilter(IConfigRequest? request, IConfigResponse? response, IConfigFilterChain chain)
    {
        // Decrypt response content
        if (response?.Content != null)
        {
            response.Content = Decrypt(response.Content);
        }
        chain.DoFilter(request, response);
    }
}

// Register filter
configService.AddConfigFilter(new EncryptionFilter());
```

### Fuzzy Watch - Nacos 3.0

```csharp
// Watch all configs starting with "app-"
await configService.FuzzyWatchAsync("app-*", "DEFAULT_GROUP", new MyFuzzyWatcher());

// Watch all services in a group
await namingService.FuzzyWatchAsync("*", "production-group", myServiceWatcher);

// Cancel fuzzy watch
await configService.CancelFuzzyWatchAsync("app-*", "DEFAULT_GROUP", myWatcher);
```

## 📊 Feature Statistics

| Module | Method Count | Description |
|--------|--------------|-------------|
| IConfigService | 17 | Configuration Center |
| INamingService | 50+ | Service Discovery (with overloads) |
| ILockService | 7 | Distributed Lock |
| IAiService (MCP) | 18 | MCP Service |
| IA2aService (A2A) | 16 | A2A Service |
| IServiceMaintainer | 12 | Service Management |
| IInstanceMaintainer | 11 | Instance Management |
| INamingMaintainer | 5 | Naming Service Operations |
| IConfigMaintainer | 18 | Config Management |
| IConfigHistoryMaintainer | 3 | Config History |
| IBetaConfigMaintainer | 7 | Beta/Gray Config |
| IConfigOpsMaintainer | 6 | Config Import/Export |
| IClientMaintainer | 11 | Client Management |
| ICoreMaintainer | 20 | Core Operations |
| **Total** | **200+** | **API Methods** |

## 🔗 Compatibility

| Component | Version Requirement |
|-----------|---------------------|
| .NET | 8.0+ / 10.0+ |
| Nacos Server | 2.x / 3.x |
| C# | 12.0+ |

## 🗺️ Roadmap

- [x] Configuration Center (Config Service)
- [x] Service Discovery (Naming Service)
- [x] Fuzzy Watch - Nacos 3.0
- [x] AI Service - MCP/A2A (Nacos 3.0)
- [x] Distributed Lock (Lock Service) - Nacos 3.0
- [x] Maintainer Service
- [x] ASP.NET Core Integration
- [x] Dependency Injection Support
- [x] Health Checks
- [x] Automatic Service Registration
- [x] HTTP Client Implementation
- [x] gRPC Client Implementation
- [ ] Security Authentication (Security Proxy)
- [ ] Prometheus Metrics Monitoring

## 📄 License

This project is licensed under [Apache License 2.0](LICENSE).

```
Copyright 2024-2026 RedNb

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```

## 🤝 Contributing

We welcome all forms of contributions! Please read the following guidelines:

### How to Contribute

1. **Fork** this repository
2. **Create** a feature branch (`git checkout -b feature/AmazingFeature`)
3. **Commit** your changes (`git commit -m 'Add some AmazingFeature'`)
4. **Push** to the branch (`git push origin feature/AmazingFeature`)
5. **Create** a Pull Request

### Contribution Types

- 🐛 Bug fixes
- ✨ New features
- 📝 Documentation improvements
- 🧪 Test cases
- 🎨 Code optimization

### Code Standards

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- All public APIs must have XML documentation comments
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/)

## 🙏 Acknowledgements

- [Nacos](https://nacos.io/) - Alibaba's open-source dynamic service discovery, configuration management, and service management platform
- [nacos-sdk-csharp](https://github.com/nacos-group/nacos-sdk-csharp) - Official C# SDK reference

## ⭐ Star History

If this project is helpful to you, please give us a ⭐ Star!

[![Star History Chart](https://api.star-history.com/svg?repos=redNb/RedNb.Nacos&type=Date)](https://star-history.com/#redNb/RedNb.Nacos&Date)

## 📞 Contact

- 📧 Email: [442962355@qq.com](mailto:442962355@qq.com)
- 💬 Issues: [GitHub Issues](https://github.com/redNb/RedNb.Nacos/issues)
- 📖 Discussions: [GitHub Discussions](https://github.com/redNb/RedNb.Nacos/discussions)

---

<div align="center">

**If you find this useful, please give us a ⭐ Star!**

Made with ❤️ by [RedNb](https://github.com/redNb)

</div>
