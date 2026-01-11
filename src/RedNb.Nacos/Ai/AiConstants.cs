namespace RedNb.Nacos.Core.Ai;

/// <summary>
/// AI module constants for Nacos.
/// </summary>
public static class AiConstants
{
    /// <summary>
    /// MCP (Model Context Protocol) related constants.
    /// </summary>
    public static class Mcp
    {
        /// <summary>
        /// Default namespace for MCP.
        /// </summary>
        public const string DefaultNamespace = "public";

        /// <summary>
        /// STDIO protocol type.
        /// </summary>
        public const string ProtocolStdio = "stdio";

        /// <summary>
        /// SSE (Server-Sent Events) protocol type.
        /// </summary>
        public const string ProtocolSse = "mcp-sse";

        /// <summary>
        /// SSE protocol type (short form).
        /// </summary>
        public const string McpProtocolSse = "SSE";

        /// <summary>
        /// Streamable protocol type.
        /// </summary>
        public const string ProtocolStreamable = "mcp-streamable";

        /// <summary>
        /// Streamable protocol type (short form).
        /// </summary>
        public const string McpProtocolStreamable = "STREAMABLE";

        /// <summary>
        /// HTTP protocol type.
        /// </summary>
        public const string ProtocolHttp = "http";

        /// <summary>
        /// Dubbo protocol type.
        /// </summary>
        public const string ProtocolDubbo = "dubbo";

        /// <summary>
        /// REF endpoint type - references an existing service.
        /// </summary>
        public const string EndpointTypeRef = "REF";

        /// <summary>
        /// REF endpoint type (alias).
        /// </summary>
        public const string McpEndpointTypeRef = "REF";

        /// <summary>
        /// DIRECT endpoint type - direct address and port.
        /// </summary>
        public const string EndpointTypeDirect = "DIRECT";

        /// <summary>
        /// DIRECT endpoint type (alias).
        /// </summary>
        public const string McpEndpointTypeDirect = "DIRECT";

        /// <summary>
        /// Front endpoint type pointing to backend.
        /// </summary>
        public const string FrontEndpointTypeToBack = "BACKEND";

        /// <summary>
        /// Active status for MCP server.
        /// </summary>
        public const string StatusActive = "ACTIVE";

        /// <summary>
        /// Deprecated status for MCP server.
        /// </summary>
        public const string StatusDeprecated = "DEPRECATED";

        /// <summary>
        /// Deleted status for MCP server.
        /// </summary>
        public const string StatusDeleted = "DELETED";

        /// <summary>
        /// Official SSE transport.
        /// </summary>
        public const string OfficialTransportSse = "sse";

        /// <summary>
        /// Official streamable HTTP transport.
        /// </summary>
        public const string OfficialTransportStreamable = "streamable-http";
    }

    /// <summary>
    /// A2A (Agent-to-Agent) related constants.
    /// </summary>
    public static class A2a
    {
        /// <summary>
        /// Default namespace for A2A.
        /// </summary>
        public const string DefaultNamespace = "public";

        /// <summary>
        /// URL endpoint type - uses URL field of agent card directly.
        /// </summary>
        public const string EndpointTypeUrl = "URL";

        /// <summary>
        /// URL endpoint type (alias).
        /// </summary>
        public const string A2aEndpointTypeUrl = "URL";

        /// <summary>
        /// SERVICE endpoint type - uses backend service of agent.
        /// </summary>
        public const string EndpointTypeService = "SERVICE";

        /// <summary>
        /// SERVICE endpoint type (alias).
        /// </summary>
        public const string A2aEndpointTypeService = "SERVICE";

        /// <summary>
        /// Default transport for A2A endpoints.
        /// </summary>
        public const string EndpointDefaultTransport = "JSONRPC";

        /// <summary>
        /// JSONRPC transport.
        /// </summary>
        public const string TransportJsonRpc = "JSONRPC";

        /// <summary>
        /// gRPC transport.
        /// </summary>
        public const string TransportGrpc = "GRPC";

        /// <summary>
        /// HTTP+JSON transport.
        /// </summary>
        public const string TransportHttpJson = "HTTP+JSON";

        /// <summary>
        /// Default protocol for A2A endpoints.
        /// </summary>
        public const string EndpointDefaultProtocol = "HTTP";
    }

    /// <summary>
    /// Default AI cache update interval in milliseconds.
    /// </summary>
    public const long DefaultAiCacheUpdateInterval = 10000L;
}
