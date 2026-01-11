using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.A2a;

/// <summary>
/// Agent endpoint for A2A protocol.
/// </summary>
public class AgentEndpoint
{
    /// <summary>
    /// Gets or sets the transport type (e.g., "JSONRPC", "GRPC", "HTTP+JSON").
    /// Default is "JSONRPC".
    /// </summary>
    [JsonPropertyName("transport")]
    public string Transport { get; set; } = AiConstants.A2a.EndpointDefaultTransport;

    /// <summary>
    /// Gets or sets the endpoint address (IP or domain).
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the endpoint port.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the endpoint path.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether TLS is supported.
    /// If true, the target AgentInterface should use "https", otherwise "http".
    /// </summary>
    [JsonPropertyName("supportTls")]
    public bool SupportTls { get; set; }

    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the custom protocol for A2A transport. Default is "HTTP".
    /// </summary>
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = AiConstants.A2a.EndpointDefaultProtocol;

    /// <summary>
    /// Gets or sets the custom query for A2A URL.
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    /// Simple check comparing only address and port.
    /// </summary>
    /// <param name="other">The other endpoint to compare.</param>
    /// <returns>True if address and port match.</returns>
    public bool SimpleEquals(AgentEndpoint other)
    {
        return Address == other.Address && Port == other.Port;
    }
}
