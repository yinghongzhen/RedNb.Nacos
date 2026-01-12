using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp.Validation;

/// <summary>
/// Result of MCP server import validation.
/// </summary>
public class McpServerImportValidationResult
{
    /// <summary>
    /// Whether the validation passed overall.
    /// </summary>
    [JsonPropertyName("valid")]
    public bool IsValid { get; set; }

    /// <summary>
    /// Overall validation errors.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Validation results for each server.
    /// </summary>
    [JsonPropertyName("servers")]
    public List<McpServerValidationItem>? Servers { get; set; }

    /// <summary>
    /// Number of valid servers.
    /// </summary>
    [JsonPropertyName("validCount")]
    public int ValidCount { get; set; }

    /// <summary>
    /// Number of invalid servers.
    /// </summary>
    [JsonPropertyName("invalidCount")]
    public int InvalidCount { get; set; }

    /// <summary>
    /// Number of duplicate servers.
    /// </summary>
    [JsonPropertyName("duplicateCount")]
    public int DuplicateCount { get; set; }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static McpServerImportValidationResult Failed(string errorMessage)
    {
        return new McpServerImportValidationResult
        {
            IsValid = false,
            Errors = new List<string> { errorMessage }
        };
    }

    /// <summary>
    /// Creates a failed validation result with multiple errors.
    /// </summary>
    public static McpServerImportValidationResult Failed(List<string> errors)
    {
        return new McpServerImportValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static McpServerImportValidationResult Success(List<McpServerValidationItem> servers)
    {
        var valid = servers.Count(s => s.Status == McpServerValidationStatus.Valid);
        var invalid = servers.Count(s => s.Status == McpServerValidationStatus.Invalid);
        var duplicate = servers.Count(s => s.Status == McpServerValidationStatus.Duplicate);

        return new McpServerImportValidationResult
        {
            IsValid = invalid == 0,
            Servers = servers,
            ValidCount = valid,
            InvalidCount = invalid,
            DuplicateCount = duplicate
        };
    }
}
