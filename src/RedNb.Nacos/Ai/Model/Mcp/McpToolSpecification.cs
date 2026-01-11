using System.Text.Json.Serialization;
using RedNb.Nacos.Core.Ai.Model.A2a;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// MCP tool specification.
/// </summary>
public class McpToolSpecification
{
    /// <summary>
    /// Gets or sets the specification type.
    /// Defaults to "normal" (plaintext storage).
    /// When set to "encrypted", server will persist encryptData as-is.
    /// </summary>
    [JsonPropertyName("specificationType")]
    public string? SpecificationType { get; set; }

    /// <summary>
    /// Gets or sets the encrypted payload when specificationType indicates encryption.
    /// </summary>
    [JsonPropertyName("encryptData")]
    public EncryptObject? EncryptData { get; set; }

    /// <summary>
    /// Gets or sets the list of MCP tools.
    /// </summary>
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = new();

    /// <summary>
    /// Gets or sets the tool metadata map.
    /// </summary>
    [JsonPropertyName("toolsMeta")]
    public Dictionary<string, McpToolMeta> ToolsMeta { get; set; } = new();

    /// <summary>
    /// Gets or sets the security schemes.
    /// </summary>
    [JsonPropertyName("securitySchemes")]
    public List<SecurityScheme> SecuritySchemes { get; set; } = new();

    /// <summary>
    /// Gets or sets custom extensions.
    /// </summary>
    [JsonPropertyName("extensions")]
    public Dictionary<string, object> Extensions { get; set; } = new();
}
