using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// Server version detail for MCP server.
/// </summary>
public class ServerVersionDetail
{
    /// <summary>
    /// Gets or sets the version string.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the release date.
    /// </summary>
    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets whether this is the latest version.
    /// </summary>
    [JsonPropertyName("isLatest")]
    public bool? IsLatest { get; set; }

    /// <summary>
    /// Gets or sets the release notes.
    /// </summary>
    [JsonPropertyName("releaseNotes")]
    public string? ReleaseNotes { get; set; }
}
