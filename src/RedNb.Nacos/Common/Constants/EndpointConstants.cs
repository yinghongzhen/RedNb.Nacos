namespace RedNb.Nacos.Common.Constants;

/// <summary>
/// Nacos v3 API 端点定义
/// </summary>
public static class EndpointConstants
{
    /// <summary>
    /// API 版本前缀
    /// </summary>
    public const string ApiVersion = "/nacos/v3";

    #region 鉴权 API

    /// <summary>
    /// 登录获取 Token
    /// </summary>
    public const string Auth_Login = $"{ApiVersion}/auth/user/login";

    #endregion

    #region 配置中心 API (Client)

    /// <summary>
    /// 获取配置
    /// </summary>
    public const string Config_Get = $"{ApiVersion}/client/cs/config";

    /// <summary>
    /// 监听配置变更
    /// </summary>
    public const string Config_Listen = $"{ApiVersion}/client/cs/config/listener";

    #endregion

    #region 配置中心 API (Admin - 需要 Token)

    /// <summary>
    /// 发布配置
    /// </summary>
    public const string Config_Publish = $"{ApiVersion}/admin/cs/config";

    /// <summary>
    /// 删除配置
    /// </summary>
    public const string Config_Delete = $"{ApiVersion}/admin/cs/config";

    /// <summary>
    /// 查询配置历史
    /// </summary>
    public const string Config_History = $"{ApiVersion}/admin/cs/config/history";

    #endregion

    #region 服务注册发现 API (Client)

    /// <summary>
    /// 注册实例
    /// </summary>
    public const string Instance_Register = $"{ApiVersion}/client/ns/instance";

    /// <summary>
    /// 注销实例
    /// </summary>
    public const string Instance_Deregister = $"{ApiVersion}/client/ns/instance";

    /// <summary>
    /// 更新实例
    /// </summary>
    public const string Instance_Update = $"{ApiVersion}/client/ns/instance";

    /// <summary>
    /// 实例心跳
    /// </summary>
    public const string Instance_Beat = $"{ApiVersion}/client/ns/instance/beat";

    /// <summary>
    /// 查询实例列表
    /// </summary>
    public const string Instance_List = $"{ApiVersion}/client/ns/instance/list";

    /// <summary>
    /// 查询实例详情
    /// </summary>
    public const string Instance_Detail = $"{ApiVersion}/client/ns/instance";

    #endregion

    #region 服务 API

    /// <summary>
    /// 查询服务列表
    /// </summary>
    public const string Service_List = $"{ApiVersion}/client/ns/service/list";

    /// <summary>
    /// 查询服务详情
    /// </summary>
    public const string Service_Detail = $"{ApiVersion}/client/ns/service";

    #endregion

    #region 健康检查

    /// <summary>
    /// 服务端健康检查
    /// </summary>
    public const string Health_Server = $"{ApiVersion}/console/health";

    /// <summary>
    /// 就绪检查
    /// </summary>
    public const string Health_Ready = $"{ApiVersion}/console/health/readiness";

    #endregion
}
