# RedNb.Nacos

[![NuGet](https://img.shields.io/nuget/v/RedNb.Nacos.All.svg)](https://www.nuget.org/packages/RedNb.Nacos.All/)
[![GitHub Release](https://img.shields.io/github/v/release/yinghongzhen/RedNb.Nacos)](https://github.com/yinghongzhen/RedNb.Nacos/releases/latest)
[![CI](https://github.com/yinghongzhen/RedNb.Nacos/actions/workflows/ci.yml/badge.svg)](https://github.com/yinghongzhen/RedNb.Nacos/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%2010-512BD4)](docs/CAPABILITIES.md)
[![Tested Nacos](https://img.shields.io/badge/tested%20Nacos-3.2.4-blue)](docs/TEST_REPORT.md)

English | [简体中文](README.md)

A .NET 8 / .NET 10 SDK targeting Nacos 3.2.4. Stable version **2.1.1** is available on [NuGet.org](https://www.nuget.org/packages/RedNb.Nacos.All/2.1.1) and [GitHub Releases](https://github.com/yinghongzhen/RedNb.Nacos/releases/tag/v2.1.1). SDK and server versions are numbered independently.

**2.1.1 maintenance update:** migrates the test infrastructure to xUnit.net v3. Public SDK APIs and runtime behavior are unchanged. All 1,500 tests and 11 CI checks passed; see the [migration report](docs/XUNIT_V3_MIGRATION.md).

Version 2.0.0 changes public namespaces and defaults Config/Naming to gRPC. Read the [migration guide](docs/MIGRATION.md) before upgrading from 1.x. Nacos 2.x is not supported; other 3.x releases require explicit validation.

**2.1.0 has passed acceptance:** 1,500 tests and all 11 CI checks passed. It adds Windows/Linux NativeAOT support and per-client JSON context injection, with fixes for numeric/container serialization, YAML merging and AI management contracts. See the [2.1.0 acceptance report](docs/NATIVEAOT_TEST_REPORT.md).

## Installation

For typical .NET applications:

```bash
dotnet add package RedNb.Nacos.DependencyInjection --version 2.1.1
```

For ASP.NET Core configuration reload, health checks and service registration:

```bash
dotnet add package RedNb.Nacos.AspNetCore --version 2.1.1
```

For all components:

```bash
dotnet add package RedNb.Nacos.All --version 2.1.1
```

Required dependencies are installed automatically; you do not need to add all six packages manually.

## NativeAOT (2.1.0)

Validated targets are .NET 8 / .NET 10 on `win-x64` / `linux-x64`, for both the SDK client and a separate ASP.NET Core Minimal API. Validation includes project references, actual NuGet consumers, configuration, naming, AI lifecycles, ACK exchange and local server restart recovery.

SDK protocol models use source-generated JSON metadata. Register application types through `NacosClientOptions.JsonTypeInfoResolver`; custom generic disk caches can receive `JsonTypeInfo<T>`. Regular JIT retains reflection fallback for application types. NativeAOT and JSON-reflection-disabled applications report unregistered types explicitly.

See the [NativeAOT guide](docs/NATIVEAOT.md), [native SDK sample](samples/RedNb.Nacos.Sample.Aot) and [Minimal API sample](samples/RedNb.Nacos.Sample.AotWeb). MVC/Swagger, ARM64, macOS, Linux musl, TLS/proxies, multi-node clusters and external LLMs are outside this acceptance scope.

## Usage

Use `RedNb.Nacos.DependencyInjection` and `AddNacos` for lazy Config/Naming services. Add AI with `AddNacosAi`, namespace administration with `AddNacosAdministration`, or a named/keyed client with `AddNacos(name, configure)`. Use `await using` for service-provider and client disposal.

Read server addresses, username and password from environment variables or your secret store. API/gRPC/Console default ports are 8848/9848/8080. The default authentication plugin uses username/password or tokens, not AK/SK login.

The six packages remain Core (`RedNb.Nacos`), Http, Grpc, DependencyInjection, AspNetCore and the dependency-only All package. AI runtime frameworks are sample dependencies, not SDK dependencies.

See the [Chinese quick start](README.md), [capability matrix](docs/CAPABILITIES.md), [migration guide](docs/MIGRATION.md), [test setup](docs/ENVIRONMENT_BOOTSTRAP.md), [test report](docs/TEST_REPORT.md) and [samples](samples/README.md).

HTTP-only config listeners and fuzzy watch fail explicitly; use gRPC. MCP/A2A Console resources are public-namespace scoped in 3.2.4. Legacy maintenance contracts are migration-only. Native locks provide the server mutex primitive, not fencing or reentrancy. TLS, proxy and multi-node failure behavior require separate validation.

Apache-2.0; see [LICENSE](LICENSE).
