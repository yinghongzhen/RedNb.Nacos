using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// MCP endpoint information.
/// </summary>
public class McpEndpointInfo
{
    /// <summary>
    /// Gets or sets the protocol (http/https).
    /// </summary>
    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    /// <summary>
    /// Gets or sets the endpoint address.
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
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the custom headers.
    /// </summary>
    [JsonPropertyName("headers")]
    public List<KeyValueInput>? Headers { get; set; }
}
