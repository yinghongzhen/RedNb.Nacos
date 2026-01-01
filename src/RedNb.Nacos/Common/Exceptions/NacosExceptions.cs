namespace RedNb.Nacos.Common.Exceptions;

/// <summary>
/// Nacos 基础异常
/// </summary>
public class NacosException : Exception
{
    /// <summary>
    /// 错误码
    /// </summary>
    public int ErrorCode { get; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; }

    public NacosException(string message)
        : base(message)
    {
        ErrorCode = ErrorCodes.ServerError;
        ErrorMessage = message;
    }

    public NacosException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = ErrorCodes.ServerError;
        ErrorMessage = message;
    }

    public NacosException(int errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
        ErrorMessage = message;
    }

    public NacosException(int errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ErrorMessage = message;
    }
}

/// <summary>
/// Nacos 鉴权异常
/// </summary>
public class NacosAuthException : NacosException
{
    public NacosAuthException(string message)
        : base(ErrorCodes.Unauthorized, message)
    {
    }

    public NacosAuthException(string message, Exception innerException)
        : base(ErrorCodes.Unauthorized, message, innerException)
    {
    }
}

/// <summary>
/// Nacos 连接异常
/// </summary>
public class NacosConnectionException : NacosException
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    public string? ServerAddress { get; }

    public NacosConnectionException(string message)
        : base(ErrorCodes.ServiceUnavailable, message)
    {
    }

    public NacosConnectionException(string message, string serverAddress)
        : base(ErrorCodes.ServiceUnavailable, message)
    {
        ServerAddress = serverAddress;
    }

    public NacosConnectionException(string message, Exception innerException)
        : base(ErrorCodes.ServiceUnavailable, message, innerException)
    {
    }

    public NacosConnectionException(string message, string serverAddress, Exception innerException)
        : base(ErrorCodes.ServiceUnavailable, message, innerException)
    {
        ServerAddress = serverAddress;
    }
}

/// <summary>
/// Nacos 配置异常
/// </summary>
public class NacosConfigException : NacosException
{
    /// <summary>
    /// 配置 DataId
    /// </summary>
    public string? DataId { get; }

    /// <summary>
    /// 配置 Group
    /// </summary>
    public string? Group { get; }

    public NacosConfigException(string message)
        : base(message)
    {
    }

    public NacosConfigException(string message, string dataId, string group)
        : base(message)
    {
        DataId = dataId;
        Group = group;
    }

    public NacosConfigException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Nacos 服务发现异常
/// </summary>
public class NacosNamingException : NacosException
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public string? ServiceName { get; }

    public NacosNamingException(string message)
        : base(message)
    {
    }

    public NacosNamingException(string message, string serviceName)
        : base(message)
    {
        ServiceName = serviceName;
    }

    public NacosNamingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
