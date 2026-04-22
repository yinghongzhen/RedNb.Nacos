namespace RedNb.Nacos.Monitor;

/// <summary>
/// 指标名称常量
/// </summary>
public static class MetricNames
{
    /// <summary>
    /// 命名空间前缀
    /// </summary>
    public const string Namespace = "nacos_client";

    // ===== Gauge 指标 =====

    /// <summary>
    /// 服务信息缓存数量
    /// </summary>
    public const string ServiceInfoMapSize = "nacos_client_service_info_map_size";

    /// <summary>
    /// 监听器配置数量
    /// </summary>
    public const string ListenConfigCount = "nacos_client_listen_config_count";

    /// <summary>
    /// 连接状态 (1=已连接, 0=未连接)
    /// </summary>
    public const string ConnectionStatus = "nacos_client_connection_status";

    /// <summary>
    /// 故障转移是否启用 (1=启用, 0=禁用)
    /// </summary>
    public const string FailoverEnabled = "nacos_client_failover_enabled";

    // ===== Counter 指标 =====

    /// <summary>
    /// 配置请求成功总数
    /// </summary>
    public const string ConfigRequestSuccessTotal = "nacos_client_config_request_success_total";

    /// <summary>
    /// 配置请求失败总数
    /// </summary>
    public const string ConfigRequestFailedTotal = "nacos_client_config_request_failed_total";

    /// <summary>
    /// 命名请求成功总数
    /// </summary>
    public const string NamingRequestSuccessTotal = "nacos_client_naming_request_success_total";

    /// <summary>
    /// 命名请求失败总数
    /// </summary>
    public const string NamingRequestFailedTotal = "nacos_client_naming_request_failed_total";

    /// <summary>
    /// 服务变更推送总数
    /// </summary>
    public const string ServiceChangePushTotal = "nacos_client_service_change_push_total";

    /// <summary>
    /// 配置变更推送总数
    /// </summary>
    public const string ConfigChangePushTotal = "nacos_client_config_change_push_total";

    /// <summary>
    /// 重做操作总数
    /// </summary>
    public const string RedoOperationTotal = "nacos_client_redo_operation_total";

    /// <summary>
    /// 故障转移使用总数
    /// </summary>
    public const string FailoverUsedTotal = "nacos_client_failover_used_total";

    // ===== Histogram 指标 =====

    /// <summary>
    /// 请求延迟直方图
    /// </summary>
    public const string RequestLatency = "nacos_client_request_latency";

    /// <summary>
    /// 配置请求延迟直方图
    /// </summary>
    public const string ConfigRequestLatency = "nacos_client_config_request_latency";

    /// <summary>
    /// 命名请求延迟直方图
    /// </summary>
    public const string NamingRequestLatency = "nacos_client_naming_request_latency";
}
