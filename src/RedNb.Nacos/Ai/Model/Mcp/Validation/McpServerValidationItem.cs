using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp.Validation;

/// <summary>
/// Validation status constants.
/// </summary>
public static class McpServerValidationStatus
{
    /// <summary>
    /// Server configuration is valid.
    /// </summary>
    public const string Valid = "valid";

    /// <summary>
    /// Server configuration is invalid.
    /// </summary>
    public const string Invalid = "invalid";

    /// <summary>
    /// Server already exists (duplicate).
    /// </summary>
    public const string Duplicate = "duplicate";
}

/// <summary>
/// Validation item for a single MCP server.
/// </summary>
public class McpServerValidationItem
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
    /// Validation status: valid, invalid, or duplicate.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = McpServerValidationStatus.Valid;

    /// <summary>
    /// Whether the server already exists.
    /// </summary>
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    /// <summary>
    /// Validation errors for this server.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }

    /// <summary>
    /// The full server detail info if valid.
    /// </summary>
    [JsonPropertyName("server")]
    public McpServerDetailInfo? Server { get; set; }
}
