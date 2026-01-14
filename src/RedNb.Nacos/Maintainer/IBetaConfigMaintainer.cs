namespace RedNb.Nacos.Core.Maintainer;

/// <summary>
/// Nacos beta/gray configuration maintainer interface.
/// </summary>
public interface IBetaConfigMaintainer
{
    /// <summary>
    /// Gets beta configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Beta configuration information.</returns>
    Task<BetaConfigInfo?> GetBetaConfigAsync(string dataId, string group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets beta configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Beta configuration information.</returns>
    Task<BetaConfigInfo?> GetBetaConfigAsync(string dataId, string group, string namespaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes beta configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="content">Configuration content.</param>
    /// <param name="betaIps">Beta IPs (comma-separated).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if published successfully.</returns>
    Task<bool> PublishBetaConfigAsync(
        string dataId,
        string group,
        string content,
        string betaIps,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes beta configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="content">Configuration content.</param>
    /// <param name="betaIps">Beta IPs (comma-separated).</param>
    /// <param name="description">Description.</param>
    /// <param name="type">Configuration type.</param>
    /// <param name="appName">Application name.</param>
    /// <param name="srcUser">Source user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if published successfully.</returns>
    Task<bool> PublishBetaConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        string content,
        string betaIps,
        string? description = null,
        string? type = null,
        string? appName = null,
        string? srcUser = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops beta configuration and promotes to formal.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if stopped successfully.</returns>
    Task<bool> StopBetaConfigAsync(string dataId, string group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops beta configuration and promotes to formal.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if stopped successfully.</returns>
    Task<bool> StopBetaConfigAsync(string dataId, string group, string namespaceId, CancellationToken cancellationToken = default);

    #region Gray Config (Nacos 3.0)

    /// <summary>
    /// Gets gray configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="grayName">Gray name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Gray configuration information.</returns>
    Task<GrayConfigInfo?> GetGrayConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        string grayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes gray configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="content">Configuration content.</param>
    /// <param name="grayRule">Gray rule.</param>
    /// <param name="description">Description.</param>
    /// <param name="type">Configuration type.</param>
    /// <param name="srcUser">Source user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if published successfully.</returns>
    Task<bool> PublishGrayConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        string content,
        GrayConfigRule grayRule,
        string? description = null,
        string? type = null,
        string? srcUser = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes gray configuration.
    /// </summary>
    /// <param name="dataId">Data ID.</param>
    /// <param name="group">Group name.</param>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="grayName">Gray name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteGrayConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        string grayName,
        CancellationToken cancellationToken = default);

    #endregion
}
