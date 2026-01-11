using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// MCP service reference configuration.
/// </summary>
public class McpServiceRef
{
    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    [JsonPropertyName("namespaceId")]
    public string? NamespaceId { get; set; }

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    [JsonPropertyName("groupName")]
    public string? GroupName { get; set; }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }
}
