using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// Icon information for MCP server.
/// </summary>
public class Icon
{
    /// <summary>
    /// Gets or sets the icon URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the icon type (e.g., favicon, logo).
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
