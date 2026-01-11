using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// MCP server detail information with endpoints and tools.
/// </summary>
public class McpServerDetailInfo : McpServerBasicInfo
{
    /// <summary>
    /// Gets or sets the backend endpoints.
    /// </summary>
    [JsonPropertyName("backendEndpoints")]
    public List<McpEndpointInfo>? BackendEndpoints { get; set; }

    /// <summary>
    /// Gets or sets the frontend endpoints.
    /// </summary>
    [JsonPropertyName("frontendEndpoints")]
    public List<McpEndpointInfo>? FrontendEndpoints { get; set; }

    /// <summary>
    /// Gets or sets the tool specification.
    /// </summary>
    [JsonPropertyName("toolSpec")]
    public McpToolSpecification? ToolSpec { get; set; }

    /// <summary>
    /// Gets or sets all available versions.
    /// </summary>
    [JsonPropertyName("allVersions")]
    public List<ServerVersionDetail>? AllVersions { get; set; }
}
