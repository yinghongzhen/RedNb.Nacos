using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// MCP endpoint specification.
/// </summary>
public class McpEndpointSpec
{
    /// <summary>
    /// Gets or sets the endpoint type (REF or DIRECT).
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the endpoint data.
    /// For DIRECT type: includes "address" and "port".
    /// For REF type: includes "namespaceId", "groupName", and "serviceName".
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, string> Data { get; set; } = new();
}
