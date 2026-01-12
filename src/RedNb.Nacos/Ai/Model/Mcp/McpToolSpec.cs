using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// MCP Tool specification for independent tool management.
/// </summary>
public class McpToolSpec
{
    /// <summary>
    /// Server ID that owns this tool.
    /// </summary>
    [JsonPropertyName("serverId")]
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// Tool name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tool description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Tool input schema (JSON Schema format).
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public Dictionary<string, object>? InputSchema { get; set; }

    /// <summary>
    /// Tool annotations.
    /// </summary>
    [JsonPropertyName("annotations")]
    public McpToolSpecAnnotations? Annotations { get; set; }

    /// <summary>
    /// Tool version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Tool enabled state.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// MCP Tool spec annotations.
/// </summary>
public class McpToolSpecAnnotations
{
    /// <summary>
    /// Title of the tool.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Whether the tool operates in read-only mode.
    /// </summary>
    [JsonPropertyName("readOnlyHint")]
    public bool? ReadOnlyHint { get; set; }

    /// <summary>
    /// Whether the tool may perform destructive operations.
    /// </summary>
    [JsonPropertyName("destructiveHint")]
    public bool? DestructiveHint { get; set; }

    /// <summary>
    /// Whether the tool may be slow to execute.
    /// </summary>
    [JsonPropertyName("openWorldHint")]
    public bool? OpenWorldHint { get; set; }
}
