using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// MCP capability information.
/// </summary>
public class McpCapability
{
    /// <summary>
    /// Gets or sets the capability name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the capability description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
