namespace RedNb.Nacos.Core.Ai.Model.Mcp.Import;

/// <summary>
/// Status of MCP server import result.
/// </summary>
public enum McpImportResultStatus
{
    /// <summary>
    /// Import was skipped (e.g., server already exists).
    /// </summary>
    Skipped,

    /// <summary>
    /// Import failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Import succeeded.
    /// </summary>
    Success
}

/// <summary>
/// Extension methods for McpImportResultStatus.
/// </summary>
public static class McpImportResultStatusExtensions
{
    /// <summary>
    /// Converts McpImportResultStatus to string.
    /// </summary>
    public static string ToStringValue(this McpImportResultStatus status)
    {
        return status switch
        {
            McpImportResultStatus.Skipped => "skipped",
            McpImportResultStatus.Failed => "failed",
            McpImportResultStatus.Success => "success",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Parses string to McpImportResultStatus.
    /// </summary>
    public static McpImportResultStatus? ParseStatus(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "skipped" => McpImportResultStatus.Skipped,
            "failed" => McpImportResultStatus.Failed,
            "success" => McpImportResultStatus.Success,
            _ => null
        };
    }
}
