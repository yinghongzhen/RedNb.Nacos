using RedNb.Nacos.Core.Ai.Model.Mcp;

namespace RedNb.Nacos.Core.Ai.Listener;

/// <summary>
/// Event for MCP server changes.
/// </summary>
public class NacosMcpServerEvent : INacosAiEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NacosMcpServerEvent"/> class.
    /// </summary>
    /// <param name="mcpServerDetailInfo">The MCP server detail info.</param>
    public NacosMcpServerEvent(McpServerDetailInfo mcpServerDetailInfo)
    {
        McpId = mcpServerDetailInfo.Id ?? string.Empty;
        NamespaceId = mcpServerDetailInfo.NamespaceId ?? string.Empty;
        McpName = mcpServerDetailInfo.Name ?? string.Empty;
        McpServerDetailInfo = mcpServerDetailInfo;
    }

    /// <summary>
    /// Gets the MCP server ID.
    /// </summary>
    public string McpId { get; }

    /// <summary>
    /// Gets the namespace ID.
    /// </summary>
    public string NamespaceId { get; }

    /// <summary>
    /// Gets the MCP server name.
    /// </summary>
    public string McpName { get; }

    /// <summary>
    /// Gets the MCP server detail info.
    /// </summary>
    public McpServerDetailInfo McpServerDetailInfo { get; }
}
