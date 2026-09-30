# xUnit.net v3 升级报告

验证日期：2026-09-29。升级基线：`master / 12ed38f`。本次只调整测试基础设施，SDK 公共 API 和包版本 `2.1.0` 保持不变。

## 迁移内容

| 项目 | 升级前 | 升级后 |
| --- | --- | --- |
| 测试框架 | `xunit` 2.9.3 | `xunit.v3.mtp-off` 4.0.1 |
| VSTest adapter | `xunit.runner.visualstudio` 3.1.5 | 4.0.0 |
| 执行方式 | VSTest | VSTest，继续生成 TRX |
| 测试项目 | 默认类库输出 | 五个项目均为 `OutputType=Exe` |
| `IAsyncLifetime` | `Task` | `ValueTask` |
| 输出接口 | `Xunit.Abstractions.ITestOutputHelper` | `Xunit.ITestOutputHelper` |
| HTTP 并发上限 | `CollectionBehavior.MaxParallelThreads` | `Parallelization.MaxThreads = 2` |

xUnit.net v3 是产品名称，其 NuGet 包现已使用 4.x 版本号。采用 `mtp-off` 保留仓库现有测试命令、TRX 校验和 Coverlet 采集方式，没有连带迁移 Microsoft.Testing.Platform。`Microsoft.NET.Test.Sdk` 和 `coverlet.collector` 的既有版本保持不变。

共享 `tests/xunit.runner.json` 固定集合级并行、程序集不并行和 Theory 数据预展开。同一个集成测试集合仍顺序执行。自定义 `LocalFaultFactAttribute` 转发源码文件和行号；未设置本地故障容器时仍跳过重启测试。

v3 会执行测试类的 `IAsyncDisposable` 清理。已检查 HTTP AI 测试的异步清理实现，并由完整 HTTP 测试覆盖。没有删除测试、缩减断言或放宽通过条件来适配新框架。

## 本地验证

环境：Windows x64，.NET SDK 10.0.401，Microsoft.NETCore.App 与 Microsoft.AspNetCore.App 8.0.31 / 10.0.12。原机器的 D 盘路径在本机不存在，本次使用 `C:\0-hzkj\project\github\RedNb.Nacos` 的 master 检出。初次缺少 ASP.NET Core 8 runtime 导致的测试宿主启动失败，在安装官方 runtime 后重新执行；该中间失败不计入最终基线。

升级前源码通过 `git archive HEAD` 冻结在忽略的 artifacts 目录，升级前后使用同一运行时比较。以下数字按每个目标框架的一次执行统计，不把覆盖率重跑计入总数。

| 测试项目 | 框架 | v2 基线通过 | v3 通过 |
| --- | --- | ---: | ---: |
| RedNb.Nacos.Tests | net8.0 | 434 | 434 |
| RedNb.Nacos.Tests | net10.0 | 434 | 434 |
| RedNb.Nacos.Http.Tests | net8.0 | 156 | 156 |
| RedNb.Nacos.Http.Tests | net10.0 | 156 | 156 |
| RedNb.Nacos.Grpc.Tests | net8.0 | 98 | 98 |
| RedNb.Nacos.Grpc.Tests | net10.0 | 98 | 98 |
| RedNb.Nacos.Sample.AI.Tests | net10.0 | 12 | 12 |
| **单元测试合计** | **7 次运行** | **1388** | **1388** |

- Release 构建通过，0 错误。
- 上述单元测试均为 0 失败、0 跳过；既有 `verify-test-results.py` 校验通过。
- TRX 中 660 个不同测试方法的集合在升级前后完全一致；Theory 展开后的执行总数也一致。
- Core 项目在 net8.0 / net10.0 分别附加执行 Coverlet：各 434 通过，生成非空 Cobertura，实际命中 SDK 代码，不是只有文件存在。
- 故障注入禁用路径：两个框架各发现并跳过 1 条重启测试，0 失败。
- 六个 NuGet 包重新打包并通过 `verify-packages.py --version 2.1.0` 校验；未发布这些本地产物。
- 五个测试项目的还原图均使用 v3 框架和 4.0.0 adapter，没有直接依赖旧的 `xunit` 框架包。
- 新分析器的 xUnit1051 取消令牌建议仍存在，未全局压制；这不是零警告构建。后续可单独逐项补齐取消传播，不在本次机械改写所有测试调用。

## 真实集成测试

**未完成真实服务运行验证。** 本机最初未运行 Docker，也没有 Nacos 3.2.4 镜像。启动 Docker Desktop 后拉取官方 `nacos/nacos-server:v3.2.4`，约 20 分钟仍未拉取完成。本轮停止了自己启动的下载与等待脚本，没有启动 Nacos 测试容器，没有访问云端 Nacos，也没有操作已有 RabbitMQ 容器。

已经单独完成集成测试发现校验：v2 / v3 在 net8.0 和 net10.0 上均发现 56 条，测试名称集合一致。两个框架的故障注入禁用分支也已验证。**发现成功不等于真实集成测试通过**；Config、Naming、AI、fixture 的真实网络清理和重启恢复仍需镜像就绪后复跑。

后续启动隔离 Nacos 3.2.4 并设置测试环境变量，执行环境文档中的完整 solution 测试命令及 `--minimum-runs 9` 校验。启用本地故障注入时，当前测试清单应执行 1500 次（含两个框架共 112 次集成测试）；这是预期数量，不是本轮已通过数量。

## 复现与产物

完整运行命令、依赖和环境变量见 [构建与测试环境](ENVIRONMENT_BOOTSTRAP.md)。CI 继续使用相同的 VSTest 命令和最少 9 次 TRX 运行校验；本次没有改动工作流，也没有宣称尚未触发的远端 CI 已通过。

本次本地产物在忽略的 `artifacts/xunit-v3/`：

- `baseline-final/`、`migrated/`：升级前后的单元测试 TRX。
- `unit-summary.json`：按程序集和框架对比的计数。
- `coverage/`：覆盖率重跑 TRX 和 Cobertura。
- `fault-disabled/`：故障注入关闭时的发现和跳过结果。
- `integration-discovery.log`、`baseline-integration-discovery.log`：两个框架的集成测试发现清单。
- `nacos-pull.log`：本轮镜像下载进度；真实集成测试未执行。
- `migration-build.log`、`pack.log`、`packages/`：构建与包校验输入。

本轮验证范围是测试框架升级；没有重新认证 SDK 的 NativeAOT、ARM64、TLS/代理或外部 LLM 能力，也没有创建 Release 或发布 NuGet。

参考：[官方 v3 迁移指南](https://xunit.net/docs/getting-started/v3/migration)、[v3 4.0 发布说明](https://xunit.net/releases/v3/4.0.0)。
