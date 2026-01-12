using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp.Import;

/// <summary>
/// Request for importing MCP servers.
/// </summary>
public class McpServerImportRequest
{
    /// <summary>
    /// Import type: json, url, or file.
    /// </summary>
    [JsonPropertyName("importType")]
    public string ImportType { get; set; } = "json";

    /// <summary>
    /// Import data (JSON content, URL, or file content).
    /// </summary>
    [JsonPropertyName("importData")]
    public string? ImportData { get; set; }

    /// <summary>
    /// Whether to override existing servers.
    /// </summary>
    [JsonPropertyName("overrideExisting")]
    public bool OverrideExisting { get; set; }

    /// <summary>
    /// Whether to only validate without importing.
    /// </summary>
    [JsonPropertyName("validateOnly")]
    public bool ValidateOnly { get; set; }

    /// <summary>
    /// Whether to skip invalid servers during import.
    /// </summary>
    [JsonPropertyName("skipInvalid")]
    public bool SkipInvalid { get; set; }

    /// <summary>
    /// Selected server IDs to import (null or empty for all).
    /// </summary>
    [JsonPropertyName("selectedServers")]
    public string[]? SelectedServers { get; set; }

    /// <summary>
    /// Pagination cursor for URL imports.
    /// </summary>
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }

    /// <summary>
    /// Page size for URL imports.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int? PageSize { get; set; }

    /// <summary>
    /// Search keyword for URL imports.
    /// </summary>
    [JsonPropertyName("searchKeyword")]
    public string? SearchKeyword { get; set; }
}
