using RedNb.Nacos.Core.Ai.Listener;
using RedNb.Nacos.Core.Ai.Model.Mcp;

namespace RedNb.Nacos.Core.Ai;

/// <summary>
/// Nacos AI client service interface.
/// Extends A2A service with MCP (Model Context Protocol) capabilities.
/// </summary>
public interface IAiService : IA2aService, IAsyncDisposable
{
    #region MCP Server Operations

    /// <summary>
    /// Gets MCP server detail info for the latest version.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detail information of the MCP server.</returns>
    Task<McpServerDetailInfo?> GetMcpServerAsync(string mcpName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets MCP server detail info for a specific version.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="version">Version of the MCP server (null for latest).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detail information of the MCP server.</returns>
    Task<McpServerDetailInfo?> GetMcpServerAsync(string mcpName, string? version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a new MCP server or new version with auto-created service reference.
    /// </summary>
    /// <param name="serverSpecification">MCP server specification.</param>
    /// <param name="toolSpecification">MCP tool specification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>MCP server ID.</returns>
    Task<string> ReleaseMcpServerAsync(McpServerBasicInfo serverSpecification, McpToolSpecification? toolSpecification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a new MCP server or new version.
    /// </summary>
    /// <param name="serverSpecification">MCP server specification.</param>
    /// <param name="toolSpecification">MCP tool specification.</param>
    /// <param name="endpointSpecification">MCP endpoint specification (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>MCP server ID.</returns>
    Task<string> ReleaseMcpServerAsync(McpServerBasicInfo serverSpecification, McpToolSpecification? toolSpecification, McpEndpointSpec? endpointSpecification, CancellationToken cancellationToken = default);

    #endregion

    #region MCP Endpoint Operations

    /// <summary>
    /// Registers an endpoint to an MCP server for all versions.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="address">Address of the endpoint.</param>
    /// <param name="port">Port of the endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterMcpServerEndpointAsync(string mcpName, string address, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an endpoint to an MCP server for a specific version.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="address">Address of the endpoint.</param>
    /// <param name="port">Port of the endpoint.</param>
    /// <param name="version">Version of the MCP server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterMcpServerEndpointAsync(string mcpName, string address, int port, string? version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters an endpoint from an MCP server.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="address">Address of the endpoint.</param>
    /// <param name="port">Port of the endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeregisterMcpServerEndpointAsync(string mcpName, string address, int port, CancellationToken cancellationToken = default);

    #endregion

    #region MCP Server Subscription

    /// <summary>
    /// Subscribes to an MCP server for the latest version.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="listener">Callback listener for MCP server changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current MCP server detail info when subscription succeeds.</returns>
    Task<McpServerDetailInfo?> SubscribeMcpServerAsync(string mcpName, AbstractNacosMcpServerListener listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to an MCP server for a specific version.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="version">Version of the MCP server.</param>
    /// <param name="listener">Callback listener for MCP server changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current MCP server detail info when subscription succeeds.</returns>
    Task<McpServerDetailInfo?> SubscribeMcpServerAsync(string mcpName, string? version, AbstractNacosMcpServerListener listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from an MCP server for the latest version.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="listener">Callback listener for MCP server changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnsubscribeMcpServerAsync(string mcpName, AbstractNacosMcpServerListener listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from an MCP server for a specific version.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="version">Version of the MCP server.</param>
    /// <param name="listener">Callback listener for MCP server changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnsubscribeMcpServerAsync(string mcpName, string? version, AbstractNacosMcpServerListener listener, CancellationToken cancellationToken = default);

    #endregion

    #region Lifecycle

    /// <summary>
    /// Shuts down the AI service and releases resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    #endregion
}
