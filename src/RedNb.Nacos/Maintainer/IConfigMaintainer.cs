namespace RedNb.Nacos.Core.Maintainer;

/// <summary>
/// Nacos configuration maintainer interface for configuration management operations.
/// </summary>
public interface IConfigMaintainer
{
    #region Config CRUD

    /// <summary>
    /// Gets configuration information.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration detail information.</returns>
    Task<ConfigDetailInfo?> GetConfigAsync(string dataId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration information.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration detail information.</returns>
    Task<ConfigDetailInfo?> GetConfigAsync(string dataId, string group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration information.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration detail information.</returns>
    Task<ConfigDetailInfo?> GetConfigAsync(string dataId, string group, string namespaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="content">Configuration content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if published successfully.</returns>
    Task<bool> PublishConfigAsync(string dataId, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="content">Configuration content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if published successfully.</returns>
    Task<bool> PublishConfigAsync(string dataId, string group, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes configuration with full parameters.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="content">Configuration content.</param>
    /// <param name="description">Description.</param>
    /// <param name="type">Configuration type (TEXT, JSON, YAML, etc.).</param>
    /// <param name="appName">Application name.</param>
    /// <param name="srcUser">Source user.</param>
    /// <param name="configTags">Configuration tags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if published successfully.</returns>
    Task<bool> PublishConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        string content,
        string? description = null,
        string? type = null,
        string? appName = null,
        string? srcUser = null,
        string? configTags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates configuration metadata.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="description">Description.</param>
    /// <param name="configTags">Configuration tags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated successfully.</returns>
    Task<bool> UpdateConfigMetadataAsync(
        string dataId,
        string group,
        string namespaceId,
        string? description,
        string? configTags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteConfigAsync(string dataId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteConfigAsync(string dataId, string group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteConfigAsync(string dataId, string group, string namespaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple configurations by IDs.
    /// </summary>
    /// <param name="ids">Configuration IDs to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteConfigsAsync(List<long> ids, CancellationToken cancellationToken = default);

    #endregion

    #region Config List/Search

    /// <summary>
    /// Lists configurations in a namespace.
    /// </summary>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of configurations.</returns>
    Task<Page<ConfigBasicInfo>> ListConfigsAsync(string namespaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists configurations with filters.
    /// </summary>
    /// <param name="dataId">Data ID (exact match).</param>
    /// <param name="group">Group name (exact match).</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="type">Configuration type.</param>
    /// <param name="configTags">Configuration tags.</param>
    /// <param name="appName">Application name.</param>
    /// <param name="pageNo">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of configurations.</returns>
    Task<Page<ConfigBasicInfo>> ListConfigsAsync(
        string? dataId,
        string? group,
        string namespaceId,
        string? type = null,
        string? configTags = null,
        string? appName = null,
        int pageNo = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches configurations with pattern matching.
    /// </summary>
    /// <param name="dataIdPattern">Data ID pattern.</param>
    /// <param name="groupPattern">Group name pattern.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of configurations.</returns>
    Task<Page<ConfigBasicInfo>> SearchConfigsAsync(
        string? dataIdPattern,
        string? groupPattern,
        string namespaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches configurations with full parameters.
    /// </summary>
    /// <param name="dataIdPattern">Data ID pattern.</param>
    /// <param name="groupPattern">Group name pattern.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="configDetail">Content pattern.</param>
    /// <param name="type">Configuration type.</param>
    /// <param name="configTags">Configuration tags.</param>
    /// <param name="appName">Application name.</param>
    /// <param name="pageNo">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of configurations.</returns>
    Task<Page<ConfigBasicInfo>> SearchConfigsAsync(
        string? dataIdPattern,
        string? groupPattern,
        string namespaceId,
        string? configDetail,
        string? type,
        string? configTags,
        string? appName,
        int pageNo = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configurations in a namespace.
    /// </summary>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of configurations.</returns>
    Task<List<ConfigBasicInfo>> GetConfigListByNamespaceAsync(string namespaceId, CancellationToken cancellationToken = default);

    #endregion

    #region Config Listeners

    /// <summary>
    /// Gets listeners for a configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Listener information.</returns>
    Task<ConfigListenerInfo> GetListenersAsync(string dataId, string group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets listeners for a configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="aggregation">Whether to aggregate from other servers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Listener information.</returns>
    Task<ConfigListenerInfo> GetListenersAsync(
        string dataId,
        string group,
        string namespaceId,
        bool aggregation = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subscribed configurations by client IP.
    /// </summary>
    /// <param name="ip">Client IP address.</param>
    /// <param name="all">Whether to include all subscriptions.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="aggregation">Whether to aggregate from other servers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Listener information.</returns>
    Task<ConfigListenerInfo> GetAllSubClientConfigByIpAsync(
        string ip,
        bool all = false,
        string? namespaceId = null,
        bool aggregation = false,
        CancellationToken cancellationToken = default);

    #endregion

    #region Config Clone

    /// <summary>
    /// Clones configurations within the same namespace.
    /// </summary>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="cloneInfos">Configurations to clone.</param>
    /// <param name="policy">Policy for handling same config.</param>
    /// <param name="srcUser">Source user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Clone result.</returns>
    Task<CloneResult> CloneConfigAsync(
        string namespaceId,
        List<ConfigCloneInfo> cloneInfos,
        SameConfigPolicy policy,
        string? srcUser = null,
        CancellationToken cancellationToken = default);

    #endregion
}
