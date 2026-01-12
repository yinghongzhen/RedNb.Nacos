namespace RedNb.Nacos.Core.Ai.Model.Mcp.Import;

/// <summary>
/// External data type for MCP server import.
/// </summary>
public enum ExternalDataType
{
    /// <summary>
    /// MCP server JSON text.
    /// </summary>
    Json,

    /// <summary>
    /// MCP registry URL.
    /// </summary>
    Url,

    /// <summary>
    /// MCP seed file.
    /// </summary>
    File
}

/// <summary>
/// Extension methods for ExternalDataType.
/// </summary>
public static class ExternalDataTypeExtensions
{
    /// <summary>
    /// Converts string to ExternalDataType.
    /// </summary>
    public static ExternalDataType? ParseType(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "json" => ExternalDataType.Json,
            "url" => ExternalDataType.Url,
            "file" => ExternalDataType.File,
            _ => null
        };
    }

    /// <summary>
    /// Converts ExternalDataType to string.
    /// </summary>
    public static string ToStringValue(this ExternalDataType type)
    {
        return type switch
        {
            ExternalDataType.Json => "json",
            ExternalDataType.Url => "url",
            ExternalDataType.File => "file",
            _ => "json"
        };
    }
}
