# RedNb.Nacos

[![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.All.svg)](https://www.nuget.org/packages/RedNb.Nacos.All/)
[![GitHub Release](https://img.shields.io/github/v/release/yinghongzhen/RedNb.Nacos)](https://github.com/yinghongzhen/RedNb.Nacos/releases/latest)
[![CI](https://github.com/yinghongzhen/RedNb.Nacos/actions/workflows/ci.yml/badge.svg)](https://github.com/yinghongzhen/RedNb.Nacos/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%2010-512BD4)](docs/CAPABILITIES.md)
[![Tested Nacos](https://img.shields.io/badge/tested%20Nacos-3.2.4-blue)](docs/TEST_REPORT.md)

[English](README.en.md) | 简体中文

面向 Nacos 3.2.4 的 .NET 8 / .NET 10 SDK。配置和服务发现默认使用 gRPC；AI 与管理操作按服务器实际能力选择 HTTP Client、Admin 或 Console 通道。

当前稳定版 **2.1.1** 已发布到 [NuGet.org](https://www.nuget.org/packages/RedNb.Nacos.All/2.1.1) 和 [GitHub Release](https://github.com/yinghongzhen/RedNb.Nacos/releases/tag/v2.1.1)。SDK 版本与 Nacos 服务端版本分别编号。

**2.1.1 维护更新**：升级 xUnit.net v3 测试基础设施，SDK 公共 API 和运行时功能保持一致。1500 次测试及 CI 11 项检查全部通过，见 [xUnit v3 升级报告](docs/XUNIT_V3_MIGRATION.md)。

**2.1.0 已完成验收**：1500 项测试通过，CI 11 项检查全部成功。新增 Windows/Linux NativeAOT 支持与客户端级 JSON Context 注入，并修复 JSON 数值/容器、YAML 合并及 AI 管理接口契约。详细证据见 [2.1.0 验收报告](docs/NATIVEAOT_TEST_REPORT.md)。

从 1.x 升级包含命名空间和默认实现的破坏性调整，请先阅读[迁移说明](docs/MIGRATION.md)。**不支持 Nacos 2.x**；其他 3.x 小版本的支持范围见[能力矩阵](docs/CAPABILITIES.md)。

## 安装

普通 .NET 应用推荐安装依赖注入包：

```bash
dotnet add package RedNb.Nacos.DependencyInjection --version 2.1.1
```

ASP.NET Core 应用需要配置热更新、健康检查和服务注册时，安装：

```bash
dotnet add package RedNb.Nacos.AspNetCore --version 2.1.1
```

需要全部组件时，可安装聚合包：

```bash
dotnet add package RedNb.Nacos.All --version 2.1.1
```

上述包会自动引入所需依赖，无需把六个包全部手动添加。

## 选择类库

| NuGet 包 | 用途 |
| --- | --- |
| [RedNb.Nacos](https://www.nuget.org/packages/RedNb.Nacos) | 接口、模型、选项和公共功能 |
| [RedNb.Nacos.Http](https://www.nuget.org/packages/RedNb.Nacos.Http) | 显式 HTTP 客户端和 v3 管理接口 |
| [RedNb.Nacos.Grpc](https://www.nuget.org/packages/RedNb.Nacos.Grpc) | 配置、服务发现、原生锁和 AI 组合客户端 |
| [RedNb.Nacos.DependencyInjection](https://www.nuget.org/packages/RedNb.Nacos.DependencyInjection) | 推荐的 DI 注册入口、命名客户端 |
| [RedNb.Nacos.AspNetCore](https://www.nuget.org/packages/RedNb.Nacos.AspNetCore) | IConfiguration 热更新、健康检查、服务注册 |
| [RedNb.Nacos.All](https://www.nuget.org/packages/RedNb.Nacos.All) | 纯依赖聚合包，方便体验全部组件 |

## 推荐用法

```csharp
using Microsoft.Extensions.DependencyInjection;
using RedNb.Nacos.DependencyInjection;
using RedNb.Nacos.Config;

var services = new ServiceCollection();
services.AddNacos(options =>
{
    options.ServerAddresses = Environment.GetEnvironmentVariable("NACOS_SERVER") ?? "localhost:8848";
    options.ConsoleAddresses = Environment.GetEnvironmentVariable("NACOS_CONSOLE") ?? "localhost:8080";
    options.Username = Environment.GetEnvironmentVariable("NACOS_USERNAME");
    options.Password = Environment.GetEnvironmentVariable("NACOS_PASSWORD");
});
await using var provider = services.BuildServiceProvider();
var config = provider.GetRequiredService<IConfigService>();
var content = await config.GetConfigAsync("app-config", "DEFAULT_GROUP", 5000);
```

只需配置或服务发现时，可使用 `AddNacosConfig` / `AddNacosNaming`。AI 通过 `AddNacosAi` 单独启用，命名空间管理通过 `AddNacosAdministration` 单独启用。各模块延迟创建，不会因为注册基础服务就连接 AI 或锁模块。

多个 Nacos 环境使用 `services.AddNacos("environment-name", configure)`，再通过 `GetRequiredKeyedService<IConfigService>("environment-name")` 获取。不同名称的客户端和缓存相互隔离。

不使用 DI 时可用 `NacosGrpcFactory.CreateConfigServiceAsync(options)`、`CreateNamingServiceAsync(options)`；AI 推荐 `new NacosAiClient(options)`，不需要调用者自行组合两种通道。具体使用见 [samples](samples/README.md)。

## 环境和验证

- API 端口默认 8848，客户端 gRPC 默认 9848，控制台默认 8080；显式映射不同端口时请配置地址和 GrpcPortOffset。
- 默认鉴权使用用户名密码/token。默认 Nacos 插件不提供本 SDK 旧版本假设的 AK/SK 登录接口。
- [单机 Docker 部署](deploy/docker-compose/README.md)
- [构建和集成测试](docs/ENVIRONMENT_BOOTSTRAP.md)
- [完整改进计划](docs/IMPROVEMENT_PLAN.md)
- [2.1.0 NativeAOT 验收报告](docs/NATIVEAOT_TEST_REPORT.md)
- [2.0.0 测试报告](docs/TEST_REPORT.md)
- [文档索引](docs/README.md)

## NativeAOT（2.1.0）

已验证 .NET 8 / .NET 10 × `win-x64` / `linux-x64` 的原生 SDK 客户端和独立 ASP.NET Core Minimal API，包括源码引用与实际 NuGet 包消费者。验证覆盖配置与服务发现、AI 生命周期、ACK 收发，以及本地服务器重启后的配置监听、Naming 和 MCP 后端恢复。

SDK 协议模型使用源生成 JSON 元数据。自定义请求、响应或扩展对象通过 `NacosClientOptions.JsonTypeInfoResolver` 注册应用的 `JsonSerializerContext`；自定义泛型磁盘缓存可传入 `JsonTypeInfo<T>`。普通 JIT 保留用户类型的反射回退；NativeAOT 或禁用 JSON 反射时，未注册类型会明确报错。

使用方法见 [NativeAOT 指南](docs/NATIVEAOT.md)，示例见 [原生 SDK](samples/RedNb.Nacos.Sample.Aot) 与 [原生 Minimal API](samples/RedNb.Nacos.Sample.AotWeb)。现有 MVC/Swagger 示例不属于 NativeAOT 验收范围；ARM64、macOS、Linux musl、TLS/代理、多节点集群及外部 LLM 等范围尚未认证。

## 能力边界

纯 HTTP 配置监听和 Fuzzy Watch 会明确报不支持；使用推荐 gRPC 入口。MCP/Agent Console 操作在 3.2.4 上限定 public 空间。旧 Maintainer 接口保留迁移错误提示，不再发送 v1/v2 请求；已实现的新管理功能以能力矩阵为准。原生锁是服务器互斥原语，不提供 fencing 或可重入语义。

源码按 Apache-2.0 发布，见 [LICENSE](LICENSE)。感谢 [Nacos](https://github.com/alibaba/nacos) 社区。
