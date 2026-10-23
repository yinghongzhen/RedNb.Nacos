# Changelog

## 2.3.0（待发布）

- #10：健康检查支持 Config/Naming 状态、Server/Console 独立节点探测、Any/All、完整诊断、取消、并发及总时限；DI 使用可定制的命名 HttpClient 管道，直接构造支持外部客户端所有权。
- #11–#14：恢复缓存命中后的 Naming 订阅、保留部分注销后的 redo、按集群隔离 Naming 磁盘缓存，修复 HTTP snapshot/failover 的键碰撞。
- #15–#17：无效 JSON 不覆盖有效配置、不发成功通知；首次加载失败释放客户端；显式 reload 删除 optional 旧键并正确恢复来源优先级。
- #18：认证锁等待、登录与请求共享单次超时预算。
- #19：统一消费 PEM CA、客户端证书和私钥，支持私有 CA/mTLS 并保留证书链、主机名、有效期检查。
- 缓存格式、全部节点失败状态及默认 DI 健康检查行为有所调整，升级前阅读[迁移说明](docs/HEALTH_TLS.md)。[逐项验收记录](docs/RELEASE_2.3.0.md)。

## 2.2.0（2026-10-17）

已发布 [GitHub Release](https://github.com/yinghongzhen/RedNb.Nacos/releases/tag/v2.2.0)；[NuGet 可信发布 38959494354](https://github.com/yinghongzhen/RedNb.Nacos/actions/runs/38959494354) 成功推送六包。[CI 38958382389](https://github.com/yinghongzhen/RedNb.Nacos/actions/runs/38958382389) 的 1534 次测试、11 项检查全部通过（0 失败、0 跳过），包含随机/显式密码的两次完整 Aspire 启动，以及原生 SDK/Web 源码和 NuGet 消费者验证。

发布提交为 `4b1882417ed21044fdb27f7fa2b07538faca6949`，六包来自该 CI 的 `validation-results`（artifact ID `12072431010`），Release 附件 SHA-256 与 CI 产物逐项一致。Aspire 和 MessagePack 未进入六个 SDK 包的依赖。

六个包均已从 NuGet 公共地址重新下载，身份、版本和内容校验通过；除 NuGet 仓库签名外，包内文件集合与字节内容均与 CI/Release 附件一致。

- 修复 #9：健康检查重复拼接协议头的问题；统一 API/Console URL 的协议处理，支持裸地址、显式 HTTP/HTTPS、TLS 默认值及带协议的 Console 地址推导。
- 实现 #3：新增 Aspire AppHost，F5 启动 Nacos 3.2.4、初始化鉴权和示例配置，再启动 Web API、Console、echo AI 示例。
- 由 AppHost 分配端口并计算 gRPC 偏移；服务注册使用实际可访问的 Web API 地址，示例健康检查探测配置的控制台。
- 新增 URL、实际 HTTP 健康检查和 Aspire 全流程测试；Aspire 依赖仅用于示例与测试，SDK 继续支持 .NET 8/10。
- TRX 校验器拒绝整体失败但失败计数为零的运行，避免中断的测试被统计为通过。

## 2.1.1（2026-10-17）

已发布 [GitHub Release](https://github.com/yinghongzhen/RedNb.Nacos/releases/tag/v2.1.1) 与六个 NuGet 包。[CI 38954662962](https://github.com/yinghongzhen/RedNb.Nacos/actions/runs/38954662962) 的 1500 次测试及 11 项检查全部通过；[可信发布 38955578158](https://github.com/yinghongzhen/RedNb.Nacos/actions/runs/38955578158) 成功。

- 五个测试项目升级到 xUnit.net v3，继续使用 VSTest、TRX 和 Coverlet。
- 适配异步 fixture 生命周期、测试输出接口、并发限制和本地故障注入特性。
- SDK 公共 API 和运行时功能保持不变；本次为测试基础设施维护版本。
- 升级前后对比及后续完整集成验收见 [xUnit v3 升级报告](docs/XUNIT_V3_MIGRATION.md)。

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
