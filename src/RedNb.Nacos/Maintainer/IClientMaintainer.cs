namespace RedNb.Nacos.Core.Maintainer;

/// <summary>
/// Nacos client connection maintainer interface.
/// </summary>
public interface IClientMaintainer
{
    /// <summary>
    /// Lists all client connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of client connections.</returns>
    Task<IEnumerable<ClientConnectionInfo>> ListClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all naming client connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection list result.</returns>
    Task<ConnectionListResult> ListNamingClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all config client connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection list result.</returns>
    Task<ConnectionListResult> ListConfigClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets client connection detail.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Client detail information.</returns>
    Task<ClientDetailInfo?> GetClientDetailAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subscribers of a client.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of subscribed services.</returns>
    Task<IEnumerable<ClientSubscribedService>> GetClientSubscribersAsync(
        string connectionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all published services of a client.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of published services.</returns>
    Task<IEnumerable<ClientPublishedService>> GetClientPublishedServicesAsync(
        string connectionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all listen configs of a client.
    /// </summary>
    /// <param name="connectionId">Connection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of listen configs.</returns>
    Task<IEnumerable<ConfigListenerInfo>> GetClientListenConfigsAsync(
        string connectionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads client connections count.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if reloaded successfully.</returns>
    Task<bool> ReloadConnectionCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets client SDK versions statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of SDK version to count.</returns>
    Task<IDictionary<string, int>> GetSdkVersionStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current node stats.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current node connection stats.</returns>
    Task<IDictionary<string, object>> GetCurrentNodeStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets connection limit for specified namespace.
    /// </summary>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="limitCount">Limit count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if reset successfully.</returns>
    Task<bool> ResetConnectionLimitAsync(
        string namespaceId,
        int limitCount,
        CancellationToken cancellationToken = default);
}
