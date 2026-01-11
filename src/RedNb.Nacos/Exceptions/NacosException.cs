namespace RedNb.Nacos.Core;

/// <summary>
/// Nacos exception representing various error conditions.
/// </summary>
public class NacosException : Exception
{
    /// <summary>
    /// Error code.
    /// </summary>
    public int ErrorCode { get; }

    /// <summary>
    /// Error message.
    /// </summary>
    public string? ErrorMessage { get; }

    public NacosException() : base()
    {
    }

    public NacosException(string message) : base(message)
    {
        ErrorMessage = message;
    }

    public NacosException(int errorCode, string message) : base(message)
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

    public NacosException(int errorCode, Exception innerException) 
        : base(innerException.Message, innerException)
    {
        ErrorCode = errorCode;
    }

    public override string ToString()
    {
        return $"ErrCode:{ErrorCode}, ErrMsg:{ErrorMessage}";
    }

    #region Client Error Codes
    
    /// <summary>
    /// Invalid param.
    /// </summary>
    public const int ClientInvalidParam = -400;

    /// <summary>
    /// Client disconnect.
    /// </summary>
    public const int ClientDisconnect = -401;

    /// <summary>
    /// Over client threshold.
    /// </summary>
    public const int ClientOverThreshold = -503;

    /// <summary>
    /// Client error.
    /// </summary>
    public const int ClientError = -500;

    /// <summary>
    /// Resource not found.
    /// </summary>
    public const int ResourceNotFound = -404;

    /// <summary>
    /// HTTP client error code.
    /// </summary>
    public const int HttpClientErrorCode = -500;

    #endregion

    #region Server Error Codes

    /// <summary>
    /// Invalid param.
    /// </summary>
    public const int InvalidParam = 400;

    /// <summary>
    /// No right (authentication failed).
    /// </summary>
    public const int NoRight = 403;

    /// <summary>
    /// Not found.
    /// </summary>
    public const int NotFound = 404;

    /// <summary>
    /// Conflict (write concurrency conflict).
    /// </summary>
    public const int Conflict = 409;

    /// <summary>
    /// Config already exists.
    /// </summary>
    public const int ConfigAlreadyExists = 410;

    /// <summary>
    /// Server error (server exception, such as timeout).
    /// </summary>
    public const int ServerError = 500;

    /// <summary>
    /// Server not implemented.
    /// </summary>
    public const int ServerNotImplemented = 501;

    /// <summary>
    /// Bad gateway.
    /// </summary>
    public const int BadGateway = 502;

    /// <summary>
    /// Over threshold.
    /// </summary>
    public const int OverThreshold = 503;

    /// <summary>
    /// Server is not started.
    /// </summary>
    public const int InvalidServerStatus = 300;

    /// <summary>
    /// Connection is not registered.
    /// </summary>
    public const int UnRegister = 301;

    /// <summary>
    /// No handler found.
    /// </summary>
    public const int NoHandler = 302;

    #endregion
}
