# 2.3.0 问题修复与验收记录

日期：2026-10-23（Asia/Shanghai）。基线：`f400c0c27386d3b049d87d7d435d81d1c2a2deb6` / 已发布 NuGet 2.2.0。本轮范围为开始工作时所有未关闭的 10 个 issue。

## 逐项复现与修复

| Issue | 本地复现 | 修复与回归入口 |
| --- | --- | --- |
| #10 | 第一 Console 返回 503，第二正常，旧检查仍失败 | Any/All、多组件结果、并发与总预算、工厂池复用；`HealthFailoverTests`、`HealthDiagnosticsTests`、真实 `HealthDiagnosticsIntegrationTests` |
| #11 | 取消订阅后缓存命中，RPC 请求为空 | 用 redo 订阅状态独立判断；`CachedResubscribeTests` 覆盖仅有缓存及再次查询 |
| #12 | 注销 A 后剩余 B 的 redo 消失 | 删除错误的整批清除；`BatchDeregisterRedoTests` 覆盖子集、全部、空集合和中途失败 |
| #13 | 同 namespace 的另一集群读到前者缓存 | 规范化集群身份及 v2 目录；`NamingCacheIsolationTests` 同时验证等价地址集合可共享 |
| #14 | group/dataId 下划线键碰撞 | 带长度边界的哈希；`HttpConfigCacheKeyTests` 另覆盖大小写及特殊字符 |
| #15 | 无效 JSON 推送清空有效值，首次加载不报错 | 解析失败不提交、不通知；`InvalidJsonUpdateTests`、`ConfigurationAtomicityTests` |
| #16 | 首次 Build 后续源失败，已持有客户端未 Dispose | 初次失败回收监听及客户端，保留原始异常；`FailedLoadCleanupTests` |
| #17 | optional 删除后显式 reload 仍保留旧键 | 确认缺失提交空来源，异常保留旧值；`DeletedOptionalReloadTests` 和多来源回退用例 |
| #18 | 100ms 请求等约 2 秒登录 | 认证/锁/传输共享总预算；`AuthTimeoutTests` 覆盖文本、raw、锁等待、调用方取消 |
| #19 | 正确私有 CA 未生效，SDK 握手失败 | HTTP/登录/健康/gRPC 共用 TLS 策略；`TlsCaPathTests`、`TlsTransportTests` 包含 mTLS、HTTP/2 和拒绝用例 |

原始 issue 测试移植到现有 xUnit v3 工程，并调整了测试目录隔离。TLS 原始脚本在 Windows 上首先遇到 SChannel 对测试证书私钥导入的差异；改为等价 PKCS#12 内存导入后，控制客户端成功，**独立引用已发布 2.2.0 的 SDK 仍在 CA 信任断言处失败**，修复版本通过。没有通过禁用证书验证获得成功。

本地原始记录位于忽略目录 `artifacts/issues-20261023/`：`before/`、`before-health/`、`tls-before-confirmed/` 为修复前证据，回归和完整测试结果独立保存，失败或中断记录不混入最终通过计数。

## 发布验收

本地完整 Nacos 3.2.4 套件通过 1634 次（0 失败、0 跳过），包含重启恢复与两次 Aspire 启动。随后增加实际 TCP 连接复用及默认 DI 兼容回归，最终 HTTP 套件在 .NET 8/10 各 212 次通过；最终提交的远端全量 CI 另行记录。失败/中断的尝试不计入这些数字。

`src` 下 HttpClient 创建点已逐一检查：HTTP 请求及认证由客户端生命周期持有；gRPC 由连接 generation 持有并随 channel 释放；周期性健康检查使用工厂管理的管道。原 2.2.0 的两次检查产生两条 TCP 连接，修复后的同一检查实例复用一条连接。

最终发布提交：`124c2a24bfe98de09fc3cb00a857b682ef491f94`。

| 验证 | 结果 | 证据 |
| --- | --- | --- |
| Linux .NET 8/10 完整套件 | 1638 通过，0 失败、0 跳过；10 份 TRX | [CI 39893514654](https://github.com/yinghongzhen/RedNb.Nacos/actions/runs/39893514654) |
| Windows .NET 8 专项 | 50 通过 | 同一 CI 的 windows-transport / net8.0 |
| Windows .NET 10 专项 | 50 通过 | 同一 CI 的 windows-transport / net10.0 |
| 禁用反射、原生 SDK/Web、包消费者及恢复 | 所有对应检查成功 | .NET 8/10 × Windows/Linux 矩阵 |
| 总 CI 门槛 | 13/13 成功 | 同上；Windows 专项为重复执行的回归，不是额外 100 个不同用例 |
| GitHub Release | v2.3.0 正式版，六个附件 | [Release](https://github.com/yinghongzhen/RedNb.Nacos/releases/tag/v2.3.0) |
| NuGet 可信发布 | 六次推送成功 | [运行 39894565451](https://github.com/yinghongzhen/RedNb.Nacos/actions/runs/39894565451) |

六包来源为 CI 的 `validation-results`（artifact ID `12475977406`）；包版本、内部依赖及源码提交一致，Release 附件 SHA-256 与 CI 产物逐项一致。已从 NuGet 公共地址重新下载六包，除仓库签名外，包内文件集合与字节内容均与 CI/Release 一致。#10–#19 已完成修复、测试和发布验收。构建仍有既有 XML 文档/xUnit 建议类警告，没有通过全局压制 AOT/裁剪警告放行。

完整使用方法与缓存迁移要求见 [健康检查、TLS 与缓存迁移](HEALTH_TLS.md)。本轮不认证 ARM64、生产代理、完整 Nacos gRPC 私有 CA 组合部署、生产集群和外部 LLM。
