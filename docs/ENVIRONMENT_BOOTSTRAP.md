# 构建与测试环境

需要 .NET 10 SDK（global.json 允许同主版本较新 feature band）、.NET 8 和 ASP.NET Core 8 runtime、Docker，以及 Nacos 3.2.4。

## Aspire 一键启动示例（2.2.0）

先启动 Docker Desktop / Docker Engine。在支持 .NET 10 与 Aspire AppHost 的 Visual Studio 中，将 `samples/RedNb.Nacos.Sample.AppHost` 设为启动项目，按 F5。命令行等价入口：

```powershell
dotnet run --project samples/RedNb.Nacos.Sample.AppHost
```

首次需要还原 Aspire 13.6.1 的 SDK、CLI bundle 和 Nacos 镜像。HTTPS dashboard 使用开发证书，首次使用可运行 `dotnet dev-certs https --trust`。SDK 的 NuGet 消费者无需安装 Aspire；SDK 仍支持 .NET 8/10，AppHost 使用 .NET 10。

启动顺序：

1. `nacos`：临时 Nacos 3.2.4 容器，启用鉴权，等待 Console readiness。
2. `bootstrap`：初始化管理员，使用 SDK 发布必需的 JSON `app-config`；成功退出后才启动示例。
3. `webapi`、`console`、`ai`：加载配置、监听变更、注册服务，以及运行本地 echo AI 演示。`ai` 完成后退出为正常现象；失败会返回非零退出码。

API、Console、gRPC 和 Web API 使用 Aspire 分配的端口；AppHost 计算 gRPC 端口偏移并注入示例。不要手工假定宿主机 gRPC 端口为 API + 1000。Dashboard 中可打开 Console、Web API `/swagger`、`/demo` 和 `/health`，并查看各资源日志。

本地管理员密码、鉴权 token 和服务身份默认随机生成，作为 Aspire secret 参数传递；管理员用户名为 `nacos`。需要查看或固定本地密码时，可通过 AppHost user-secrets 配置已知值：

```powershell
dotnet user-secrets set "Parameters:nacos-password" "your-local-development-password" --project samples/RedNb.Nacos.Sample.AppHost
```

本入口创建临时演示环境，不连接云端服务器、不保留数据卷。停止 AppHost 会停止其管理的示例和容器；已有 Docker Compose 的独立、MySQL 和集群部署方案继续位于 `deploy/docker-compose/`。

`RedNb.Nacos.Sample.AppHost.Tests` 自动启动独立环境，验证初始化、真实 HTTP 健康检查、Web API、Console、服务注册端点、配置热更新，以及 echo AI 正常退出。该测试需要 Docker。

```powershell
dotnet test tests/RedNb.Nacos.Sample.AppHost.Tests -c Release
```

## SDK 构建与测试

健康检查的 `ConsoleAddresses` 接受 `host:port` 或带 HTTP/HTTPS 协议的地址。已有协议优先；只有裸地址才由 `EnableTls` 选择默认协议。控制台 readiness 路径为 `/v3/console/health/readiness`，不加 `/nacos`。`GetBaseUrl` 同样保留显式协议，但会附加配置的 API ContextPath。

```powershell
dotnet restore RedNb.Nacos.sln
dotnet build RedNb.Nacos.sln -c Release
dotnet test RedNb.Nacos.sln --no-build -c Release -m:1 -p:TestTfmsInParallel=false --logger trx --results-directory artifacts/test-results
python scripts/verify-test-results.py artifacts/test-results --minimum-runs 10
dotnet pack RedNb.Nacos.sln --no-build -c Release -o artifacts/packages
python scripts/verify-packages.py artifacts/packages --version 2.2.0
```

测试前使用新的结果目录，避免旧 TRX 混入统计。按项目和目标框架顺序执行，避免多个 WireMock 测试宿主争用本机资源。

## xUnit.net v3

六个测试项目使用 xUnit.net v3 产品线的 `xunit.v3.mtp-off` 4.0.1 和 Visual Studio adapter 4.0.0。包版本的主版本号为 4，产品名称仍为 v3。选择 `mtp-off` 是为了保留现有 VSTest、TRX 校验器和 `coverlet.collector`；无需改变上面的 `dotnet test` 命令，也不启用 Microsoft.Testing.Platform。

测试项目为可执行项目，`IAsyncLifetime` 的初始化和清理方法返回 `ValueTask`，`ITestOutputHelper` 位于 `Xunit` 命名空间。新增异步测试时应向支持取消的方法传递 `TestContext.Current.CancellationToken`；现有测试的 xUnit1051 提示未被全局压制。

共享的 `tests/xunit.runner.json` 保留集合级并行和 Theory 数据预展开；同一 `NacosIntegration` 集合内顺序执行。HTTP 测试通过 `Parallelization(MaxThreads = 2)` 保留并发上限。v3 会调用测试类的 `IAsyncDisposable`，因此异步清理应完整释放测试资源。

覆盖率可按需采集，例如：

```powershell
dotnet test tests/RedNb.Nacos.Tests/RedNb.Nacos.Tests.csproj --no-build -c Release -m:1 -p:TestTfmsInParallel=false --collect:"XPlat Code Coverage" --results-directory artifacts/coverage
```

迁移验证和范围见 [xUnit v3 升级报告](XUNIT_V3_MIGRATION.md)。

## 集成测试变量

| 环境变量 | 含义 | 本地默认 |
| --- | --- | --- |
| NACOS_TEST_SERVER | SDK API 地址（host:port） | localhost:8848 |
| NACOS_TEST_CONSOLE | 控制台地址 | localhost:8080 |
| NACOS_TEST_USERNAME | 用户名 | nacos |
| NACOS_TEST_PASSWORD | 密码 | nacos，仅本地测试 |
| NACOS_TEST_NAMESPACE | Config/Naming 测试空间 | public/空 tenant |
| NACOS_TEST_FAULT_CONTAINER | 启用重启测试的本地容器名 | 不设置则跳过故障注入 |

远程联调先创建独立 namespace，再把连接信息放进当前进程环境变量。不要将真实凭据写入仓库或控制台命令记录文件。测试资源使用随机名称，namespace 生命周期测试仅删除自身创建的空间。MCP/A2A Console 限定 public 时使用 audit/it 前缀随机资源并逐项清理。

故障注入只允许 localhost/127.0.0.1，容器名必须匹配 rednb-nacos-*。CI 使用临时容器；云端不进行重启、清库或网络中断。

普通 CI 不依赖云端凭据。GitHub workflow 已覆盖 master 的 push/PR，并校验零测试发现、失败测试和实际 NuGet 包内容。
