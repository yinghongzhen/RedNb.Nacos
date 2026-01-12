using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp.Import;

/// <summary>
/// Result of importing a single MCP server.
/// </summary>
public class McpServerImportResult
{
    /// <summary>
    /// Server name.
    /// </summary>
    [JsonPropertyName("serverName")]
    public string? ServerName { get; set; }

    /// <summary>
    /// Server ID.
    /// </summary>
    [JsonPropertyName("serverId")]
    public string? ServerId { get; set; }

    /// <summary>
    /// Import status: success, failed, or skipped.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Error message if import failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Conflict type if skipped (e.g., "existing").
    /// </summary>
    [JsonPropertyName("conflictType")]
    public string? ConflictType { get; set; }
}
