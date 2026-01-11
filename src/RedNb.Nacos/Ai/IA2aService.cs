using RedNb.Nacos.Core.Ai.Listener;
using RedNb.Nacos.Core.Ai.Model.A2a;

namespace RedNb.Nacos.Core.Ai;

/// <summary>
/// Nacos A2A (Agent-to-Agent) client service interface.
/// </summary>
public interface IA2aService
{
    #region Agent Card Operations

    /// <summary>
    /// Gets an agent card with Nacos extension detail for the latest version.
    /// </summary>
    /// <param name="agentName">Name of the agent card.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agent card with Nacos extension detail.</returns>
    Task<AgentCardDetailInfo?> GetAgentCardAsync(string agentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an agent card with Nacos extension detail for a specific version.
    /// </summary>
    /// <param name="agentName">Name of the agent card.</param>
    /// <param name="version">Target version (null or empty for latest).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agent card with Nacos extension detail.</returns>
    Task<AgentCardDetailInfo?> GetAgentCardAsync(string agentName, string? version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an agent card with Nacos extension detail for a specific version and registration type.
    /// </summary>
    /// <param name="agentName">Name of the agent card.</param>
    /// <param name="version">Target version (null or empty for latest).</param>
    /// <param name="registrationType">Registration type (URL or SERVICE).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agent card with Nacos extension detail.</returns>
    Task<AgentCardDetailInfo?> GetAgentCardAsync(string agentName, string? version, string? registrationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a new agent card or new version with default service type endpoint.
    /// </summary>
    /// <param name="agentCard">Agent card to release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReleaseAgentCardAsync(AgentCard agentCard, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a new agent card or new version with specified registration type.
    /// </summary>
    /// <param name="agentCard">Agent card to release.</param>
    /// <param name="registrationType">Registration type (URL or SERVICE).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReleaseAgentCardAsync(AgentCard agentCard, string registrationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a new agent card or new version.
    /// </summary>
    /// <param name="agentCard">Agent card to release.</param>
    /// <param name="registrationType">Registration type (URL or SERVICE).</param>
    /// <param name="setAsLatest">Whether to set this version as latest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReleaseAgentCardAsync(AgentCard agentCard, string registrationType, bool setAsLatest, CancellationToken cancellationToken = default);

    #endregion

    #region Agent Endpoint Operations

    /// <summary>
    /// Registers an endpoint to an agent card.
    /// </summary>
    /// <param name="agentName">Name of the agent.</param>
    /// <param name="version">Version of this endpoint.</param>
    /// <param name="address">Address for this endpoint.</param>
    /// <param name="port">Port of this endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterAgentEndpointAsync(string agentName, string version, string address, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an endpoint to an agent card with transport.
    /// </summary>
    /// <param name="agentName">Name of the agent.</param>
    /// <param name="version">Version of this endpoint.</param>
    /// <param name="address">Address for this endpoint.</param>
    /// <param name="port">Port of this endpoint.</param>
    /// <param name="transport">Supported transport (JSONRPC, GRPC, HTTP+JSON).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterAgentEndpointAsync(string agentName, string version, string address, int port, string transport, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an endpoint to an agent card.
    /// </summary>
    /// <param name="agentName">Name of the agent.</param>
    /// <param name="endpoint">Endpoint information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterAgentEndpointAsync(string agentName, AgentEndpoint endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch registers endpoints to an agent card.
    /// </summary>
    /// <param name="agentName">Name of the agent.</param>
    /// <param name="endpoints">Collection of endpoints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterAgentEndpointsAsync(string agentName, IEnumerable<AgentEndpoint> endpoints, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters an endpoint from an agent card.
    /// </summary>
    /// <param name="agentName">Name of the agent.</param>
    /// <param name="version">Version of this endpoint.</param>
    /// <param name="address">Address for this endpoint.</param>
    /// <param name="port">Port of this endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeregisterAgentEndpointAsync(string agentName, string version, string address, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters an endpoint from an agent card.
    /// </summary>
    /// <param name="agentName">Name of the agent.</param>
    /// <param name="endpoint">Endpoint information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeregisterAgentEndpointAsync(string agentName, AgentEndpoint endpoint, CancellationToken cancellationToken = default);

    #endregion

    #region Agent Card Subscription

    /// <summary>
    /// Subscribes to an agent card for the latest version.
    /// </summary>
    /// <param name="agentName">Name of the agent.</param>
    /// <param name="listener">Callback listener for agent card changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current agent card when subscription succeeds.</returns>
    Task<AgentCardDetailInfo?> SubscribeAgentCardAsync(string agentName, AbstractNacosAgentCardListener listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to an agent card for a specific version.
    /// </summary>
    /// <param name="agentName">Name of the agent.</param>
    /// <param name="version">Version of the agent (null or empty for latest).</param>
    /// <param name="listener">Callback listener for agent card changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current agent card when subscription succeeds.</returns>
    Task<AgentCardDetailInfo?> SubscribeAgentCardAsync(string agentName, string? version, AbstractNacosAgentCardListener listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from an agent card for the latest version.
    /// </summary>
    /// <param name="agentName">Name of the agent.</param>
    /// <param name="listener">Callback listener for agent card changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnsubscribeAgentCardAsync(string agentName, AbstractNacosAgentCardListener listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from an agent card for a specific version.
    /// </summary>
    /// <param name="agentName">Name of the agent.</param>
    /// <param name="version">Version of the agent (null or empty for latest).</param>
    /// <param name="listener">Callback listener for agent card changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnsubscribeAgentCardAsync(string agentName, string? version, AbstractNacosAgentCardListener listener, CancellationToken cancellationToken = default);

    #endregion
}
