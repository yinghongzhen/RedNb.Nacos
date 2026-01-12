using RedNb.Nacos.Core.Ai.Listener;
using RedNb.Nacos.Core.Ai.Model;
using RedNb.Nacos.Core.Ai.Model.Mcp;
using RedNb.Nacos.Core.Ai.Model.Mcp.Import;
using RedNb.Nacos.Core.Ai.Model.Mcp.Validation;

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

    #region MCP Server Management

    /// <summary>
    /// Deletes an MCP server.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server to delete.</param>
    /// <param name="version">Version to delete (null to delete all versions).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteMcpServerAsync(string mcpName, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists MCP servers with pagination.
    /// </summary>
    /// <param name="mcpName">Optional MCP server name filter.</param>
    /// <param name="search">Optional search keyword.</param>
    /// <param name="pageNo">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of MCP servers.</returns>
    Task<PageResult<McpServerBasicInfo>> ListMcpServersAsync(
        string? mcpName = null,
        string? search = null,
        int pageNo = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    #endregion

    #region MCP Server Import/Validation

    /// <summary>
    /// Validates MCP server import data without actually importing.
    /// </summary>
    /// <param name="request">Import request with validation-only flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with details about each server.</returns>
    Task<McpServerImportValidationResult> ValidateImportAsync(
        McpServerImportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports MCP servers from external data sources.
    /// </summary>
    /// <param name="request">Import request containing source data and options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import response with results for each server.</returns>
    Task<McpServerImportResponse> ImportMcpServersAsync(
        McpServerImportRequest request,
        CancellationToken cancellationToken = default);

    #endregion

    #region MCP Tool Management

    /// <summary>
    /// Refreshes a specific MCP tool from the server.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="toolName">Name of the tool to refresh.</param>
    /// <param name="version">Version of the MCP server (null for latest).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Refreshed tool specification.</returns>
    Task<McpToolSpec?> RefreshMcpToolAsync(
        string mcpName,
        string toolName,
        string? version = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific MCP tool specification.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="toolName">Name of the tool.</param>
    /// <param name="version">Version of the MCP server (null for latest).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tool specification if found.</returns>
    Task<McpToolSpec?> GetMcpToolAsync(
        string mcpName,
        string toolName,
        string? version = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific MCP tool.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="toolName">Name of the tool to delete.</param>
    /// <param name="version">Version of the MCP server (null for latest).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteMcpToolAsync(
        string mcpName,
        string toolName,
        string? version = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a specific MCP tool.
    /// </summary>
    /// <param name="mcpName">Name of the MCP server.</param>
    /// <param name="toolSpec">Updated tool specification.</param>
    /// <param name="version">Version of the MCP server (null for latest).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateMcpToolAsync(
        string mcpName,
        McpToolSpec toolSpec,
        string? version = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Lifecycle

    /// <summary>
    /// Shuts down the AI service and releases resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    #endregion
}
