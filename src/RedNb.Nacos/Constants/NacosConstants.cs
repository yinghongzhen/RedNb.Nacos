namespace RedNb.Nacos.Core;

/// <summary>
/// Common constants used throughout the Nacos SDK.
/// </summary>
public static class NacosConstants
{
    /// <summary>
    /// Default group name.
    /// </summary>
    public const string DefaultGroup = "DEFAULT_GROUP";

    /// <summary>
    /// Default cluster name.
    /// </summary>
    public const string DefaultClusterName = "DEFAULT";

    /// <summary>
    /// Default namespace (public).
    /// </summary>
    public const string DefaultNamespace = "public";

    /// <summary>
    /// Service info splitter.
    /// </summary>
    public const string ServiceInfoSplitter = "@@";

    /// <summary>
    /// Default heart beat interval in milliseconds.
    /// </summary>
    public const long DefaultHeartBeatInterval = 5000;

    /// <summary>
    /// Default heart beat timeout in milliseconds.
    /// </summary>
    public const long DefaultHeartBeatTimeout = 15000;

    /// <summary>
    /// Default IP delete timeout in milliseconds.
    /// </summary>
    public const long DefaultIpDeleteTimeout = 30000;

    /// <summary>
    /// Default instance ID generator type.
    /// </summary>
    public const string DefaultInstanceIdGenerator = "simple";

    /// <summary>
    /// NULL string constant.
    /// </summary>
    public const string Null = "null";

    /// <summary>
    /// ALL pattern for fuzzy matching.
    /// </summary>
    public const string AllPattern = "*";

    /// <summary>
    /// Any pattern for fuzzy matching.
    /// </summary>
    public const string AnyPattern = "*";

    /// <summary>
    /// Default encoding.
    /// </summary>
    public const string DefaultEncoding = "UTF-8";

    /// <summary>
    /// Config module.
    /// </summary>
    public const string ConfigModule = "config";

    /// <summary>
    /// Naming module.
    /// </summary>
    public const string NamingModule = "naming";

    /// <summary>
    /// Client version header.
    /// </summary>
    public const string ClientVersionHeader = "Client-Version";

    /// <summary>
    /// User agent header.
    /// </summary>
    public const string UserAgentHeader = "User-Agent";

    /// <summary>
    /// Request source header.
    /// </summary>
    public const string RequestSourceHeader = "Request-Source";

    /// <summary>
    /// Content type.
    /// </summary>
    public const string ContentType = "Content-Type";

    /// <summary>
    /// Content type form urlencoded.
    /// </summary>
    public const string ContentTypeFormUrlEncoded = "application/x-www-form-urlencoded";

    /// <summary>
    /// Content type JSON.
    /// </summary>
    public const string ContentTypeJson = "application/json";

    /// <summary>
    /// Default timeout in milliseconds.
    /// </summary>
    public const int DefaultTimeout = 3000;

    /// <summary>
    /// Default long poll timeout in milliseconds.
    /// </summary>
    public const int DefaultLongPollTimeout = 30000;

    /// <summary>
    /// Access token header.
    /// </summary>
    public const string AccessToken = "accessToken";

    /// <summary>
    /// Token TTL header.
    /// </summary>
    public const string TokenTtl = "tokenTtl";

    /// <summary>
    /// Token refresh window.
    /// </summary>
    public const long TokenRefreshWindow = 120000;

    /// <summary>
    /// Nacos server protocol version 3.
    /// </summary>
    public const string ProtocolV3 = "v3";
}
