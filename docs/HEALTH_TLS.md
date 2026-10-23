# 2.3.0 健康检查、TLS 与缓存迁移

## 选择健康检查的含义

无参数 `AddNacos()` 优先检查默认 Config 客户端，保持原有 Config 健康语义；没有注册 Config 时检查 Naming，没有注册任何客户端时返回 Unhealthy。它不会用 Console HTTP 200 代替业务连接状态。需要同时依赖 Config 和 Naming 的应用，应显式使用 `ClientConnections` 模式并设置 `RequiredClients = NacosHealthCheckClients.All`；也可单独选择 Config 或 Naming。所需客户端未注册或尚未连接时返回 Unhealthy，不会为了检查而强行初始化连接。

带 `NacosClientOptions` 的旧重载仍检查 Console；默认 Any 策略允许首节点故障后使用其他可用节点。可用新的 `NacosHealthCheckOptions` 选择：

| 选项 | 含义 |
| --- | --- |
| `Mode = ClientConnections` | 检查提供的 Config/Naming 客户端，不发 HTTP 探测 |
| `RequiredClients` | 客户端模式选择 Config（默认）、Naming 或 All；所需能力缺失或断开时失败 |
| `Mode = Console` | 检查 `/v3/console/health/readiness` |
| `Mode = Server` | 检查 API ContextPath 下 `/v3/admin/core/state/readiness` |
| `Mode = ServerAndConsole` | 两种组件分别检查；Console 成功不会掩盖 Server 全部失败 |
| `Strategy = Any` | 每个所选组件至少一个节点健康即可；满足后取消其他探测 |
| `Strategy = All` | 全部健康为 Healthy，组件仍可用但有节点故障为 Degraded，任一组件完全不可用为 Unhealthy |
| `ProbeAll = true` | 即使使用 Any，也完成全部节点诊断 |
| `EndpointTimeout` / `TotalTimeout` | 默认单项 2 秒、整体 5 秒；整体预算包含排队时间 |
| `MaxConcurrency` | 默认 4，同一检查实例的并发调用也共享上限 |

调用方取消会传播 `OperationCanceledException`。节点结果位于 `HealthCheckResult.Data["nodes"]`，类型为 `NacosNodeHealthResult[]`，含组件、去除凭据的地址、健康状态、耗时、HTTP 状态码和安全失败代码，不返回响应正文或原始异常消息。Any 提前成功时，未完成节点可能标记为已取消；完整巡检应使用 `ProbeAll`。

```csharp
using RedNb.Nacos;
using RedNb.Nacos.AspNetCore.HealthChecks;

builder.Services.AddHealthChecks()
    .AddNacos(name: "nacos-client")
    .AddNacos(new NacosClientOptions
    {
        ServerAddresses = "node1:8848,node2:8848",
        ConsoleAddresses = "console1:8080,console2:8080"
    }, new NacosHealthCheckOptions
    {
        Mode = NacosHealthCheckMode.ServerAndConsole,
        Strategy = NacosHealthCheckStrategy.All,
        ProbeAll = true
    }, name: "nacos-nodes");
```

业务 readiness 端点应按注册名或 tags 选择所需的客户端检查；节点巡检单独暴露给监控，不应让非业务必需的 Console 故障决定业务 readiness。ASP.NET Core 默认把 Degraded 映射为 HTTP 200，宿主可按检查用途调整映射。

## HTTP 生命周期和宿主扩展

DI 注册复用 singleton 检查实例，每次调用从 `IHttpClientFactory` 获取和释放轻量客户端，底层 handler/连接池由工厂复用，尊重工厂的 handler 生命周期。默认传输的连接寿命为 5 分钟。可用以下名称定制客户端、DelegatingHandler、代理或连接设置：

```csharp
builder.Services.AddHttpClient(NacosHealthCheckExtensions.GetHttpClientName("nacos-nodes"))
    .ConfigureHttpClient(client => client.DefaultRequestHeaders.Add("X-Monitor", "my-app"));
```

如替换整个 primary handler，TLS 和证书策略由宿主配置的 handler 负责。直接构造支持 `new NacosHealthCheck(options, externalHttpClient)`；不会修改或释放外部客户端。未传入外部客户端的直接构造会持有可复用客户端，调用者应使用 `using` 或在生命周期结束时 `Dispose`。

## TLS 与 mTLS

请求、登录、Console、健康检查及 gRPC 的底层 HTTP/2 传输统一读取以下选项：

- `TlsCaPath`：PEM CA 证书或 CA 证书 bundle。配置后以这些 CA 为自定义信任根，仍验证主机名、有效期、证书链和服务器用途；不接受任意证书，不修改系统证书信任库。
- `TlsCertPath` 与 `TlsKeyPath`：必须成对提供，分别为 PEM 客户端证书（可含证书链）和未加密 PEM 私钥，用于 mTLS。缺失、无效或不匹配的材料会报错。PFX、带密码私钥不是这两个路径选项支持的输入格式。
- 未配置 CA 时保留系统默认信任。自定义 CA 验证不联网获取中间证书，也不做 CRL/OCSP 吊销检查；服务端应发送完整中间链。证书文件在 handler 创建时读取，轮换后需重建相应客户端/handler。
- `EnableTls` 仅为裸地址选择默认 HTTPS；显式 HTTP/HTTPS URL 的协议优先，沿用 2.2.0 规则。

```csharp
var options = new NacosClientOptions
{
    ServerAddresses = "nacos.internal:8848",
    ConsoleAddresses = "https://console.internal:8443",
    EnableTls = true,
    TlsCaPath = "certs/ca.pem",
    TlsCertPath = "certs/client.pem",
    TlsKeyPath = "certs/client-key.pem"
};
```

本轮真实握手测试覆盖受控回环 HTTPS、登录、Console、健康检查、mTLS 及 gRPC 所用底层 HTTP/2 handler，同时覆盖错误 CA、主机名、过期证书和错误/缺失客户端材料。完整 Nacos gRPC 协议与私有 CA 的组合部署、生产代理、ARM64 和生产集群不应据此视为已认证。

## 缓存迁移与行为变化

2.3.0 的 Naming 磁盘缓存采用 `nacos/naming/v2/<identity>`，身份包含规范化和排序去重后的服务器集合、Endpoint、命名空间与用户名。同一集群地址的顺序、主机名大小写、重复项不造成额外隔离；不同集群、TLS 协议或用户不共享数据。

HTTP 配置的 snapshot/failover 采用 `snapshot/v2/<hash>`、`failover/v2/<hash>`，对带长度边界的 group/dataId 进行哈希，避免下划线和文件系统大小写造成碰撞。**旧 Naming 缓存、旧 HTTP snapshot/failover 不自动读取或迁移**，因为旧文件无法可靠证明归属。升级前应在线启动预热 Naming/snapshot；手工维护 HTTP failover 的用户应通过 `LocalConfigCache.SaveFailover` 重新生成文件。旧文件不会被 SDK 自动删除。gRPC 配置缓存不属于本次文件格式变更。

另外，HTTP 请求的单次超时预算现在覆盖认证锁等待、登录、请求体和响应以及所有重试，不会因切换节点重新获得一份完整预算。无效 JSON 推送保留最后的有效值且不发成功 reload 通知；必需配置初次解析失败会报错并释放内部客户端。显式 reload 对可选配置的确认删除会清除旧来源并恢复其他来源的优先级值；可选配置读取/解析异常则保留上次有效值。

参考：[Nacos 健康端点](https://nacos.io/en/docs/latest/manual/admin/monitor/)、[Microsoft HttpClient 生命周期](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines)。
