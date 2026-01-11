using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// MCP server remote service configuration.
/// </summary>
public class McpServerRemoteServiceConfig
{
    /// <summary>
    /// Gets or sets the export protocol.
    /// </summary>
    [JsonPropertyName("exportProtocol")]
    public string? ExportProtocol { get; set; }

    /// <summary>
    /// Gets or sets the export URL path.
    /// </summary>
    [JsonPropertyName("exportPath")]
    public string? ExportPath { get; set; }

    /// <summary>
    /// Gets or sets the service reference configuration.
    /// </summary>
    [JsonPropertyName("serviceRef")]
    public McpServiceRef? ServiceRef { get; set; }
}
