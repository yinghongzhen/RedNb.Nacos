namespace RedNb.Nacos.Core.Maintainer;

/// <summary>
/// Nacos core maintainer interface for namespace, cluster and server management.
/// </summary>
public interface ICoreMaintainer
{
    #region Namespace Management

    /// <summary>
    /// Gets all namespaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of namespaces.</returns>
    Task<IEnumerable<NamespaceInfo>> GetNamespacesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets namespace by ID.
    /// </summary>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Namespace information.</returns>
    Task<NamespaceInfo?> GetNamespaceAsync(string namespaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a namespace.
    /// </summary>
    /// <param name="request">Namespace request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if created successfully.</returns>
    Task<bool> CreateNamespaceAsync(NamespaceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a namespace.
    /// </summary>
    /// <param name="request">Namespace request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated successfully.</returns>
    Task<bool> UpdateNamespaceAsync(NamespaceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a namespace.
    /// </summary>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteNamespaceAsync(string namespaceId, CancellationToken cancellationToken = default);

    #endregion

    #region Cluster Management

    /// <summary>
    /// Gets all cluster members.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of cluster members.</returns>
    Task<IEnumerable<ClusterMemberInfo>> GetClusterMembersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current node's address.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current node address.</returns>
    Task<string> GetSelfNodeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets leader of the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Leader member information.</returns>
    Task<ClusterMemberInfo?> GetClusterLeaderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a cluster member's lookup address.
    /// </summary>
    /// <param name="address">Member address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated successfully.</returns>
    Task<bool> UpdateClusterMemberLookupAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leaves from cluster.
    /// </summary>
    /// <param name="addresses">Addresses to leave.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if left successfully.</returns>
    Task<bool> LeaveClusterAsync(IEnumerable<string> addresses, CancellationToken cancellationToken = default);

    #endregion

    #region Server State Management

    /// <summary>
    /// Gets current server state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server state information.</returns>
    Task<ServerStateInfo> GetServerStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server switches configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server switch information.</returns>
    Task<ServerSwitchInfo> GetServerSwitchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates server switches configuration.
    /// </summary>
    /// <param name="entry">Switch entry name.</param>
    /// <param name="value">Switch value.</param>
    /// <param name="debug">Debug mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated successfully.</returns>
    Task<bool> UpdateServerSwitchAsync(
        string entry,
        string value,
        bool debug = false,
        CancellationToken cancellationToken = default);

    #endregion

    #region Health Management

    /// <summary>
    /// Gets server readiness status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if server is ready.</returns>
    Task<bool> GetReadinessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server liveness status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if server is alive.</returns>
    Task<bool> GetLivenessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if server is up.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if server is up.</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Metrics

    /// <summary>
    /// Gets server metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Metrics information.</returns>
    Task<MetricsInfo> GetMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Prometheus metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Prometheus format metrics string.</returns>
    Task<string> GetPrometheusMetricsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Raft Operations

    /// <summary>
    /// Gets raft cluster leader.
    /// </summary>
    /// <param name="groupId">Raft group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Leader address.</returns>
    Task<string> GetRaftLeaderAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfers raft leadership.
    /// </summary>
    /// <param name="groupId">Raft group ID.</param>
    /// <param name="targetAddress">Target address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if transferred successfully.</returns>
    Task<bool> TransferRaftLeaderAsync(
        string groupId,
        string targetAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets raft cluster.
    /// </summary>
    /// <param name="groupId">Raft group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if reset successfully.</returns>
    Task<bool> ResetRaftClusterAsync(string groupId, CancellationToken cancellationToken = default);

    #endregion
}
