namespace RedNb.Nacos.Common.Constants;

/// <summary>
/// Nacos 错误码定义
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// 成功
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// 参数错误
    /// </summary>
    public const int InvalidParam = 400;

    /// <summary>
    /// 未授权
    /// </summary>
    public const int Unauthorized = 401;

    /// <summary>
    /// 禁止访问
    /// </summary>
    public const int Forbidden = 403;

    /// <summary>
    /// 资源未找到
    /// </summary>
    public const int NotFound = 404;

    /// <summary>
    /// 配置冲突
    /// </summary>
    public const int Conflict = 409;

    /// <summary>
    /// 服务端错误
    /// </summary>
    public const int ServerError = 500;

    /// <summary>
    /// 服务不可用
    /// </summary>
    public const int ServiceUnavailable = 503;

    /// <summary>
    /// 根据错误码获取错误消息
    /// </summary>
    public static string GetMessage(int code) => code switch
    {
        Success => "操作成功",
        InvalidParam => "参数错误",
        Unauthorized => "未授权，请检查用户名密码",
        Forbidden => "禁止访问",
        NotFound => "资源未找到",
        Conflict => "资源冲突",
        ServerError => "服务端内部错误",
        ServiceUnavailable => "服务不可用",
        _ => $"未知错误: {code}"
    };
}
