# RedNb.Nacos

基于 .NET 10 的 Nacos 3.x 客户端 SDK，提供配置中心和服务发现功能。

## 📋 功能特性

- ✅ **配置中心**：配置读取、发布、删除，支持配置变更监听
- ✅ **服务注册与发现**：服务注册、注销、实例查询、健康检查
- ✅ **负载均衡**：内置 Random、RoundRobin、WeightedRandom、WeightedRoundRobin 策略
- ✅ **认证支持**：Token 自动获取与刷新
- ✅ **热更新**：配置变更自动推送到 IConfiguration
- ✅ **ASP.NET Core 集成**：WebApplicationBuilder 扩展、后台服务自动注册

## 📦 NuGet 包

| 包名 | 描述 |
|------|------|
| `RedNb.Nacos` | 核心 SDK（配置中心 + 服务发现） |
| `RedNb.Nacos.AspNetCore` | ASP.NET Core 集成 |
| `RedNb.Nacos.Configuration` | IConfiguration 配置提供程序 |

## 🚀 快速开始

### 安装

```bash
dotnet add package RedNb.Nacos
dotnet add package RedNb.Nacos.AspNetCore
dotnet add package RedNb.Nacos.Configuration
```

### 基础配置

在 `appsettings.json` 中添加配置：

```json
{
  "RedNb": {
    "Nacos": {
      "ServerAddresses": ["http://localhost:8848"],
      "Namespace": "public",
      "Username": "nacos",
      "Password": "nacos",
      "Naming": {
        "ServiceName": "my-service",
        "GroupName": "DEFAULT_GROUP",
        "ClusterName": "DEFAULT",
        "RegisterEnabled": true,
        "Weight": 1.0,
        "Metadata": {
          "version": "1.0.0"
        }
      },
      "Config": {
        "Listeners": [
          {
            "DataId": "my-service.json",
            "Group": "DEFAULT_GROUP"
          }
        ]
      }
    }
  }
}
```

### ASP.NET Core 集成

```csharp
var builder = WebApplication.CreateBuilder(args);

// 方式一：从配置文件读取
builder.AddRedNbNacos();

// 方式二：代码配置
builder.Services.AddRedNbNacosAspNetCore(options =>
{
    options.ServerAddresses = ["http://localhost:8848"];
    options.Namespace = "public";
    options.Username = "nacos";
    options.Password = "nacos";
    
    options.Naming.ServiceName = "my-service";
    options.Naming.RegisterEnabled = true;
});

var app = builder.Build();
app.Run();
```

### 配置中心

```csharp
// 从 Nacos 读取配置到 IConfiguration
builder.Configuration.AddRedNbNacosConfiguration(
    serverAddresses: "http://localhost:8848",
    dataId: "my-service.json",
    group: "DEFAULT_GROUP",
    namespaceId: "public",
    username: "nacos",
    password: "nacos"
);
```

### 注入服务使用

```csharp
public class MyService
{
    private readonly INacosConfigService _configService;
    private readonly INacosNamingService _namingService;

    public MyService(
        INacosConfigService configService,
        INacosNamingService namingService)
    {
        _configService = configService;
        _namingService = namingService;
    }

    public async Task<string> GetConfigAsync()
    {
        return await _configService.GetConfigAsync("my-config.json", "DEFAULT_GROUP");
    }

    public async Task<Instance?> GetServiceInstanceAsync()
    {
        return await _namingService.SelectOneInstanceAsync("target-service", "DEFAULT_GROUP");
    }
}
```

## 🔌 API 参考

### INacosConfigService

```csharp
public interface INacosConfigService
{
    Task<string?> GetConfigAsync(string dataId, string group, CancellationToken cancellationToken = default);
    Task<bool> PublishConfigAsync(string dataId, string group, string content, CancellationToken cancellationToken = default);
    Task<bool> RemoveConfigAsync(string dataId, string group, CancellationToken cancellationToken = default);
    Task AddListenerAsync(string dataId, string group, IConfigListener listener, CancellationToken cancellationToken = default);
    Task RemoveListenerAsync(string dataId, string group, IConfigListener listener, CancellationToken cancellationToken = default);
}
```

### INacosNamingService

```csharp
public interface INacosNamingService
{
    Task RegisterInstanceAsync(string serviceName, string groupName, Instance instance, CancellationToken cancellationToken = default);
    Task DeregisterInstanceAsync(string serviceName, string groupName, Instance instance, CancellationToken cancellationToken = default);
    Task<List<Instance>> GetInstancesAsync(string serviceName, string groupName, CancellationToken cancellationToken = default);
    Task<List<Instance>> GetHealthyInstancesAsync(string serviceName, string groupName, CancellationToken cancellationToken = default);
    Task<Instance?> SelectOneInstanceAsync(string serviceName, string groupName, CancellationToken cancellationToken = default);
    Task SubscribeAsync(string serviceName, string groupName, IEventListener listener, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string serviceName, string groupName, IEventListener listener, CancellationToken cancellationToken = default);
}
```

## ⚖️ 负载均衡策略

| 策略 | 说明 |
|------|------|
| `random` | 随机选择（默认） |
| `roundrobin` | 轮询 |
| `weightedrandom` | 加权随机 |
| `weightedroundrobin` | 加权轮询 |

```csharp
// 设置负载均衡策略
options.Naming.LoadBalancerStrategy = "weightedroundrobin";
```

## 🏗️ 项目结构

```
RedNb.Nacos/
├── src/
│   ├── RedNb.Nacos/                    # 核心 SDK
│   │   ├── Auth/                       # 认证模块
│   │   ├── Common/                     # 公共组件
│   │   │   ├── Constants/              # 常量定义
│   │   │   ├── Exceptions/             # 异常定义
│   │   │   ├── Options/                # 配置选项
│   │   │   └── Utils/                  # 工具类
│   │   ├── Config/                     # 配置中心
│   │   ├── Http/                       # HTTP 客户端
│   │   └── Naming/                     # 服务发现
│   ├── RedNb.Nacos.AspNetCore/         # ASP.NET Core 集成
│   └── RedNb.Nacos.Configuration/      # 配置提供程序
├── tests/
│   └── RedNb.Nacos.Tests/              # 单元测试
├── samples/
│   └── RedNb.Nacos.Sample.WebApi/      # 示例项目
└── RedNb.Nacos.sln
```

## 📝 Nacos 3.x 兼容性

本 SDK 专为 Nacos 3.x 设计，使用 v3 API 端点：

| 功能 | API 端点 |
|------|----------|
| 认证 | `/nacos/v3/auth/login` |
| 配置 | `/nacos/v3/cs/config` |
| 服务注册 | `/nacos/v3/ns/instance` |
| 服务列表 | `/nacos/v3/ns/instance/list` |

## 🧪 测试

```bash
cd tests/RedNb.Nacos.Tests
dotnet test
```

## 📄 许可证

MIT License

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

---

**RedNb** - 中创科技出品
