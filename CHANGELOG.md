# Changelog

## 2.1.1（待发布）

- 五个测试项目升级到 xUnit.net v3，继续使用 VSTest、TRX 和 Coverlet。
- 适配异步 fixture 生命周期、测试输出接口、并发限制和本地故障注入特性。
- SDK 公共 API 和运行时功能保持不变；本次为测试基础设施维护版本。
- 升级验证与尚未完成的真实集成测试见 [xUnit v3 升级报告](docs/XUNIT_V3_MIGRATION.md)。

## 2.1.0（2026-09-15）

已发布到 GitHub Release 与 NuGet.org，见 [发布记录](docs/RELEASE_2.1.0.md)。

- 保留 JSON 数值精度、无符号边界、值类型集合与 Base64 语义，拒绝循环容器。
- 修复 YAML 合并优先级、合并序列、引号键和循环别名处理。
- 新增客户端级 JsonTypeInfoResolver 和泛型缓存 JsonTypeInfo 重载；SDK 元数据优先，保留用户类型 JIT 反射回退。
- 增加 .NET 8/10 原生 SDK、Minimal API、禁用反射与 NuGet 消费者验收入口。
- 发布校验接收显式版本，继续使用 nuget-publish.yml 可信发布入口。
- HTTP/gRPC 客户端标识改为读取程序集版本；Windows/Linux 原生源码/包消费者与恢复验收、完整 CI 均通过。
- 修复 Skill/AgentSpec 管理路由、Prompt 版本分页和 AgentSpec 数字时间戳读取；验收同时处理自动审核上线与手工发布。

## 2.0.0

- Nacos 3.2.4 / .NET 8、10 验收基线，停止 2.x 兼容。
- 统一公开命名空间、模型目录、DI 与命名客户端，提供 AI 自动路由 facade。
- 修复配置旧缓存、监听差异、外部删除、热更新、命名空间隔离与后台重连重放。
- 按真实 3.2.4 协议实现模糊监听和原生 mutex；修复 MCP/A2A 创建与更新的区分。
- 增加 v3 命名空间管理；旧 Maintainer 与不支持的 HTTP 操作提供明确迁移错误。
- 集中依赖和打包元数据，All 改为纯依赖包，清理重复运行时实现与跟踪生成物。
- 增加本地/云端集成测试、重启故障测试、测试发现门禁与 NuGet 安装验证。

破坏性变更和行为限制详见 docs/MIGRATION.md；执行证据详见 docs/TEST_REPORT.md。
