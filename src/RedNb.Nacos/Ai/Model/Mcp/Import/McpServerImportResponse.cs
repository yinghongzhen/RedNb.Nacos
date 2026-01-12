using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp.Import;

/// <summary>
/// Response for MCP server import operation.
/// </summary>
public class McpServerImportResponse
{
    /// <summary>
    /// Whether the overall import was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Error message if import failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total number of servers processed.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of successfully imported servers.
    /// </summary>
    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed imports.
    /// </summary>
    [JsonPropertyName("failedCount")]
    public int FailedCount { get; set; }

    /// <summary>
    /// Number of skipped servers.
    /// </summary>
    [JsonPropertyName("skippedCount")]
    public int SkippedCount { get; set; }

    /// <summary>
    /// Individual import results for each server.
    /// </summary>
    [JsonPropertyName("results")]
    public List<McpServerImportResult>? Results { get; set; }

    /// <summary>
    /// Creates a failed response with error message.
    /// </summary>
    public static McpServerImportResponse Error(string message)
    {
        return new McpServerImportResponse
        {
            Success = false,
            ErrorMessage = message
        };
    }
}
