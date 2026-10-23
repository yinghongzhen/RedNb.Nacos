# Nacos 3.2.4 能力与验证矩阵

验收版本：Nacos 3.2.4；运行框架：.NET 8 / .NET 10。2.x 不支持，其他 3.x 版本未经本次矩阵认证。

以下既有功能表以普通 JIT 验证为基础。2.1.0 的 NativeAOT 验收独立记录在 [NativeAOT 测试报告](NATIVEAOT_TEST_REPORT.md)，不能从 JIT 功能通过推断全部原生平台已通过。

| 能力 | 推荐通道/入口 | 验证范围 |
| --- | --- | --- |
| 配置读取、发布、删除 | gRPC / IConfigService | 本地与云端、独立读写客户端、删除一致性 |
| 配置 CAS | gRPC | 正确 MD5 成功、旧 MD5 拒绝且不覆盖内容 |
| 普通配置监听 | gRPC | 推送、删除通知、退订、初始差异修复；HTTP 明确不支持 |
| IConfiguration 热更新 | AspNetCore | 新值、移除键、整个配置删除 |
| Config Fuzzy Watch | gRPC | 3.2.4 groupKeyPattern 协议、初始同步、新增事件、取消 |
| 服务注册/注销/发现 | gRPC 默认，HTTP 可选 | 实例列表、筛选、推送、HTTP 心跳及缓存 |
| Naming Fuzzy Watch | gRPC | public 规范化、初始列表、新增服务、取消 |
| 断线恢复 | gRPC | 本地容器重启；不靠原客户端主动请求，Config/Naming/MCP 后端自动恢复 |
| 鉴权 | 默认用户名密码/token | 实际登录和带鉴权访问；AK/SK-only 明确拒绝 |
| 命名空间管理 | v3 Admin / IAdministrationService | 创建、修改、列表、删除及配置跨空间隔离 |
| MCP/A2A 元数据与订阅 | HTTP Console | 创建/更新、查询/列表、版本、删除、轮询回调；public 限制明确 |
| MCP/A2A 端点 | gRPC / NacosAiClient 自动路由 | 单个/批量登记、注销、跨通道使用；MCP 重启恢复额外验证 |
| Prompt | HTTP Client/Admin | 草稿、提交审核、强制发布、版本/标签读取、渲染、下线、删除 |
| Skill | HTTP Client/Admin | ZIP 上传、提交审核、强制发布、下载内容、MD5、下线、删除 |
| AgentSpec | HTTP Client/Admin | manifest + AGENTS.md 上传、发布、标签读取、资源保留、下线、删除 |
| 原生互斥锁 | gRPC LockOperationRequest | 双客户端竞争、释放、未持有客户端拒绝释放；不支持本地伪可重入 |
| MCP 独立 tool CRUD | 无对应 3.2.4 能力 | 明确不支持；工具描述可随 MCP server specification 发布 |
| 旧 Maintainer API | 迁移提示 | 不发送 v1/v2 请求，旧操作明确拒绝，不能视为完整运维 API |
| 多客户端 / DI / 聚合包 | DependencyInjection / All | 独立 options/lifecycle，6 个 NuGet 包校验、独立项目安装 |

完整执行结果见 [测试报告](TEST_REPORT.md)。这张表描述主要行为的覆盖，不等于每个可选字段和每个服务器插件组合都完成了端到端验证。

## 约束

- MCP/A2A Console 操作在 3.2.4 限定 public，不能将自定义 namespace 悄悄映射到 public。
- 默认 AI pipeline 可能要求审核；本次仅对测试资源使用 ForcePublish。普通发布需要遵守服务器审批状态，SDK 不伪造审批成功。
- AgentSpec 的主 Content 是 manifest；AGENTS.md 保存在 Resource 中。
- Nacos 原生 mutex 没有 fencing token 和服务端持有者校验。SDK 本地租约检查不构成跨进程所有权保证，不自动重新获取失去的锁。
- 2.3.0 已验证受控回环 HTTPS、私有 CA、mTLS 及底层 HTTP/2 传输，见[验收报告](RELEASE_2.3.0.md)。完整 Nacos gRPC 私有 CA 组合部署、自定义反向代理、多节点生产集群故障切换、极端并发压力及所有 AI 可选管理操作，不包含在本次认证范围。
- legacy 静态/具体 transport 工厂仍可用于显式选择通道；推荐 DI 默认入口或 NacosAiClient，避免将不支持能力误当成可用。
