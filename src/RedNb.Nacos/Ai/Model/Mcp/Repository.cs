using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// Repository information for MCP server.
/// </summary>
public class Repository
{
    /// <summary>
    /// Gets or sets the repository URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the source type (e.g., github, gitlab).
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the repository ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
