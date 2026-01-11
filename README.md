<div align="center">

# RedNb.Nacos

**A Modern .NET SDK for Nacos**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-green.svg?style=flat-square)](LICENSE)
[![Nacos](https://img.shields.io/badge/Nacos-3.1.1-00C7B7.svg?style=flat-square)](https://nacos.io/)
[![GitHub stars](https://img.shields.io/github/stars/redNb/RedNb.Nacos?style=flat-square)](https://github.com/redNb/RedNb.Nacos/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/redNb/RedNb.Nacos?style=flat-square)](https://github.com/redNb/RedNb.Nacos/issues)
[![GitHub forks](https://img.shields.io/github/forks/redNb/RedNb.Nacos?style=flat-square)](https://github.com/redNb/RedNb.Nacos/network)

[English](README.en.md) | 简体中文

</div>

---

**RedNb.Nacos** 是一个现代化的 .NET Nacos 客户端 SDK，完全兼容 Nacos 3.x，支持配置中心、服务发现和 AI 服务（MCP/A2A）等核心功能。

> 🎯 **为什么选择 RedNb.Nacos？**
> - 基于 .NET 10 构建，采用最新语言特性
> - 完整支持 Nacos 3.x 新功能（Fuzzy Watch、AI Service）
> - 模块化设计，按需引用，减少依赖

## ✨ 特性

- 🚀 **高性能** - 支持 HTTP 和 gRPC 两种通信协议
- 📦 **模块化设计** - 按需引用，灵活组合
- 🔄 **Nacos 3.x 兼容** - 支持最新的 Fuzzy Watch、AI Service 等特性
- 💉 **依赖注入** - 原生支持 Microsoft.Extensions.DependencyInjection
- 🏗️ **ASP.NET Core 集成** - 配置提供程序、健康检查、服务自动注册
- ⚡ **异步优先** - 全异步 API 设计
- 📝 **强类型** - 完整的类型支持和 XML 文档

## 📦 NuGet 包

| 包名 | 描述 | NuGet |
|------|------|-------|
| `RedNb.Nacos` | 核心抽象层：接口、模型和常量定义 | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos) |
| `RedNb.Nacos.Http` | HTTP 客户端实现 | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.Http.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos.Http) |
| `RedNb.Nacos.Grpc` | gRPC 高性能客户端实现 | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.Grpc.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos.Grpc) |
| `RedNb.Nacos.DependencyInjection` | 依赖注入扩展 | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.DependencyInjection.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos.DependencyInjection) |
| `RedNb.Nacos.AspNetCore` | ASP.NET Core 集成 | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.AspNetCore.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos.AspNetCore) |
| `RedNb.Nacos.All` | 全功能包（包含以上所有） | [![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.All.svg?style=flat-square)](https://www.nuget.org/packages/RedNb.Nacos.All) |

## 🚀 快速开始

### 安装

```bash
# 基础包（推荐）
dotnet add package RedNb.Nacos.Http

# 或全功能包
dotnet add package RedNb.Nacos.All

# ASP.NET Core 集成
dotnet add package RedNb.Nacos.AspNetCore
```

### 基础用法

#### 1. 直接使用工厂创建服务

```csharp
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Naming;

// 配置选项
var options = new NacosClientOptions
{
    ServerAddresses = "localhost:8848",
    Username = "nacos",
    Password = "nacos",
    Namespace = "",
    DefaultTimeout = 5000
};

// 创建服务
var factory = new NacosFactory();
var configService = factory.CreateConfigService(options);
var namingService = factory.CreateNamingService(options);
```

#### 2. 配置中心

```csharp
// 获取配置
var content = await configService.GetConfigAsync("app-config", "DEFAULT_GROUP", 5000);

// 发布配置
await configService.PublishConfigAsync("app-config", "DEFAULT_GROUP", jsonContent, ConfigType.Json);

// 监听配置变更
await configService.AddListenerAsync("app-config", "DEFAULT_GROUP", new MyConfigListener());

// 模糊监听（Nacos 3.0）
await configService.FuzzyWatchAsync("app-*", "DEFAULT_GROUP", myWatcher);
```

#### 3. 服务发现

```csharp
// 注册实例
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

// 获取所有实例
var instances = await namingService.GetAllInstancesAsync("my-service");

// 选择一个健康实例（加权随机）
var selected = await namingService.SelectOneHealthyInstanceAsync("my-service");

// 订阅服务变更
await namingService.SubscribeAsync("my-service", evt =>
{
    Console.WriteLine($"Service changed: {evt.Instances?.Count} instances");
});

// 注销实例
await namingService.DeregisterInstanceAsync("my-service", instance);
```

### ASP.NET Core 集成

#### 1. 添加 Nacos 配置源

```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加 Nacos 作为配置源
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
    source.ConfigItems.Add(new NacosConfigurationItem 
    { 
        DataId = "db-config", 
        Group = "DEFAULT_GROUP",
        Optional = true  // 可选配置
    });
});
```

#### 2. 依赖注入

```csharp
// 注册 Nacos 服务
builder.Services.AddNacos(options =>
{
    options.ServerAddresses = "localhost:8848";
    options.Username = "nacos";
    options.Password = "nacos";
});

// 添加健康检查
builder.Services.AddHealthChecks()
    .AddNacos();
```

#### 3. 服务自动注册

```csharp
var app = builder.Build();

// 自动注册当前服务到 Nacos
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

#### 4. 注入使用

```csharp
[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly IConfigService _configService;
    private readonly INamingService _namingService;

    public DemoController(IConfigService configService, INamingService namingService)
    {
        _configService = configService;
        _namingService = namingService;
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
}
```

## 🧩 功能模块

### 配置中心 (Config Service)

| 功能 | 方法 | 描述 |
|------|------|------|
| 获取配置 | `GetConfigAsync()` | 获取配置内容 |
| 获取并监听 | `GetConfigAndSignListenerAsync()` | 获取配置并注册监听器 |
| 发布配置 | `PublishConfigAsync()` | 发布/更新配置 |
| CAS 发布 | `PublishConfigCasAsync()` | 基于 MD5 的乐观锁更新 |
| 删除配置 | `RemoveConfigAsync()` | 删除配置 |
| 添加监听 | `AddListenerAsync()` | 添加配置变更监听器 |
| 移除监听 | `RemoveListener()` | 移除配置变更监听器 |
| 模糊监听 | `FuzzyWatchAsync()` | 模式匹配批量监听 (Nacos 3.0) |
| 取消模糊监听 | `CancelFuzzyWatchAsync()` | 取消模糊监听 |
| 配置过滤器 | `AddConfigFilter()` | 添加配置拦截过滤器 |
| 服务状态 | `GetServerStatus()` | 获取服务器健康状态 |

### 服务发现 (Naming Service)

| 功能 | 方法 | 描述 |
|------|------|------|
| 注册实例 | `RegisterInstanceAsync()` | 注册服务实例 |
| 批量注册 | `BatchRegisterInstanceAsync()` | 批量注册实例 |
| 注销实例 | `DeregisterInstanceAsync()` | 注销服务实例 |
| 批量注销 | `BatchDeregisterInstanceAsync()` | 批量注销实例 |
| 获取实例 | `GetAllInstancesAsync()` | 获取所有实例 |
| 选择实例 | `SelectInstancesAsync()` | 按健康状态筛选实例 |
| 单实例选择 | `SelectOneHealthyInstanceAsync()` | 加权随机选择健康实例 |
| 订阅服务 | `SubscribeAsync()` | 订阅服务变更事件 |
| 取消订阅 | `UnsubscribeAsync()` | 取消服务订阅 |
| 模糊监听 | `FuzzyWatchAsync()` | 模式匹配批量监听 (Nacos 3.0) |
| 服务列表 | `GetServicesOfServerAsync()` | 分页获取服务列表 |
| 服务状态 | `GetServerStatus()` | 获取服务器健康状态 |

### AI 服务 (Nacos 3.0)

| 功能 | 方法 | 描述 |
|------|------|------|
| **MCP 服务** | | |
| 获取 MCP 服务器 | `GetMcpServerAsync()` | 获取 MCP 服务器详情 |
| 发布 MCP 服务器 | `ReleaseMcpServerAsync()` | 发布 MCP 服务器 |
| 注册端点 | `RegisterMcpServerEndpointAsync()` | 注册 MCP 端点 |
| 注销端点 | `DeregisterMcpServerEndpointAsync()` | 注销 MCP 端点 |
| 订阅 | `SubscribeMcpServerAsync()` | 订阅 MCP 服务器变更 |
| **A2A 服务** | | |
| 获取 Agent Card | `GetAgentCardAsync()` | 获取 Agent 详情 |
| 发布 Agent Card | `ReleaseAgentCardAsync()` | 发布 Agent |
| 注册端点 | `RegisterAgentEndpointAsync()` | 注册 Agent 端点 |
| 订阅 | `SubscribeAgentCardAsync()` | 订阅 Agent 变更 |

## 📁 项目结构

```
RedNb.Nacos/
├── src/
│   ├── RedNb.Nacos/                    # 核心抽象层
│   │   ├── Config/                     # 配置中心接口和模型
│   │   ├── Naming/                     # 服务发现接口和模型
│   │   ├── Ai/                         # AI 服务接口 (MCP/A2A)
│   │   ├── Constants/                  # 常量定义
│   │   ├── Exceptions/                 # 异常类型
│   │   └── Utils/                      # 工具类
│   ├── RedNb.Nacos.Http/               # HTTP 客户端实现
│   │   ├── Config/                     # 配置服务实现
│   │   ├── Naming/                     # 命名服务实现
│   │   ├── Ai/                         # AI 服务实现
│   │   └── Http/                       # HTTP 客户端基础设施
│   ├── RedNb.Nacos.Grpc/               # gRPC 客户端实现
│   │   ├── Config/                     # gRPC 配置服务
│   │   ├── Naming/                     # gRPC 命名服务
│   │   └── Protos/                     # Protocol Buffer 定义
│   ├── RedNb.Nacos.DependencyInjection/ # DI 扩展
│   ├── RedNb.Nacos.AspNetCore/         # ASP.NET Core 集成
│   │   ├── Configuration/              # 配置提供程序
│   │   ├── HealthChecks/               # 健康检查
│   │   └── ServiceRegistry/            # 服务自动注册
│   └── RedNb.Nacos.All/                # 全功能聚合包
├── samples/
│   ├── RedNb.Nacos.Sample.Console/     # 控制台示例
│   └── RedNb.Nacos.Sample.WebApi/      # WebAPI 示例
└── tests/
    ├── RedNb.Nacos.Tests/              # 单元测试
    ├── RedNb.Nacos.Http.Tests/         # HTTP 客户端测试
    └── RedNb.Nacos.IntegrationTests/   # 集成测试
```

## ⚙️ 配置选项

```csharp
public class NacosClientOptions
{
    /// <summary>服务器地址，多个用逗号分隔</summary>
    public string ServerAddresses { get; set; } = "localhost:8848";
    
    /// <summary>命名空间 ID</summary>
    public string? Namespace { get; set; }
    
    /// <summary>用户名</summary>
    public string? Username { get; set; }
    
    /// <summary>密码</summary>
    public string? Password { get; set; }
    
    /// <summary>默认超时时间（毫秒）</summary>
    public int DefaultTimeout { get; set; } = 5000;
    
    /// <summary>日志级别</summary>
    public string LogLevel { get; set; } = "Info";
    
    /// <summary>启用 gRPC（默认使用 HTTP）</summary>
    public bool UseGrpc { get; set; } = false;
}
```

## 🔧 高级特性

### 配置监听器

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

### 配置过滤器

```csharp
public class EncryptionFilter : IConfigFilter
{
    public string Name => "EncryptionFilter";
    public int Order => 1;

    public void DoFilter(IConfigRequest? request, IConfigResponse? response, IConfigFilterChain chain)
    {
        // 解密响应内容
        if (response?.Content != null)
        {
            response.Content = Decrypt(response.Content);
        }
        chain.DoFilter(request, response);
    }
}

// 注册过滤器
configService.AddConfigFilter(new EncryptionFilter());
```

### 模糊监听 (Fuzzy Watch)

```csharp
// 监听所有以 "app-" 开头的配置
await configService.FuzzyWatchAsync("app-*", "DEFAULT_GROUP", new MyFuzzyWatcher());

// 监听某个组下的所有服务
await namingService.FuzzyWatchAsync("*", "production-group", myServiceWatcher);
```

## 🔗 兼容性

| 组件 | 版本要求 |
|------|---------|
| .NET | 10.0+ |
| Nacos Server | 2.x / 3.x |
| C# | 13.0 (preview) |

## 🗺️ 路线图

- [x] 配置中心 (Config Service)
- [x] 服务发现 (Naming Service)
- [x] 模糊监听 (Fuzzy Watch)
- [x] AI 服务 - MCP/A2A (Nacos 3.0)
- [x] ASP.NET Core 集成
- [x] 依赖注入支持
- [x] 健康检查
- [x] 服务自动注册
- [ ] 分布式锁 (Lock Service)
- [ ] 安全认证 (Security Proxy)
- [ ] 故障转移 (Failover)
- [ ] 服务维护 (Naming Maintain)
- [ ] Prometheus 指标监控

## 📄 许可证

本项目采用 [Apache License 2.0](LICENSE) 许可证开源。

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

## 🤝 贡献指南

我们欢迎任何形式的贡献！请阅读以下指南：

### 如何贡献

1. **Fork** 本仓库
2. **创建** 功能分支 (`git checkout -b feature/AmazingFeature`)
3. **提交** 更改 (`git commit -m 'Add some AmazingFeature'`)
4. **推送** 到分支 (`git push origin feature/AmazingFeature`)
5. **创建** Pull Request

### 贡献类型

- 🐛 Bug 修复
- ✨ 新功能
- 📝 文档改进
- 🧪 测试用例
- 🎨 代码优化

### 代码规范

- 遵循 [C# 编码约定](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- 所有公共 API 必须有 XML 文档注释
- 提交信息遵循 [Conventional Commits](https://www.conventionalcommits.org/)

## 🙏 致谢

- [Nacos](https://nacos.io/) - 阿里巴巴开源的动态服务发现、配置管理和服务管理平台
- [nacos-sdk-csharp](https://github.com/nacos-group/nacos-sdk-csharp) - 官方 C# SDK 参考

## ⭐ Star History

如果这个项目对你有帮助，请给我们一个 ⭐ Star！

[![Star History Chart](https://api.star-history.com/svg?repos=redNb/RedNb.Nacos&type=Date)](https://star-history.com/#redNb/RedNb.Nacos&Date)

## 📞 联系方式

- 📧 Email: [your-email@example.com](mailto:your-email@example.com)
- 💬 Issues: [GitHub Issues](https://github.com/redNb/RedNb.Nacos/issues)
- 📖 Discussions: [GitHub Discussions](https://github.com/redNb/RedNb.Nacos/discussions)

---

<div align="center">

**如果觉得有用，请给个 ⭐ Star 支持一下！**

Made with ❤️ by [RedNb](https://github.com/redNb)

</div>
