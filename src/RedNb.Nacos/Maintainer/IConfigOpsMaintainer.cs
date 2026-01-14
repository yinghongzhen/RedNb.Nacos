namespace RedNb.Nacos.Core.Maintainer;

/// <summary>
/// Nacos configuration operations maintainer interface (import/export).
/// </summary>
public interface IConfigOpsMaintainer
{
    /// <summary>
    /// Imports configurations from file.
    /// </summary>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="policy">Same config policy.</param>
    /// <param name="fileContent">File content as byte array.</param>
    /// <param name="fileName">File name (e.g., config.zip).</param>
    /// <param name="srcUser">Source user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import result.</returns>
    Task<ConfigImportResult> ImportConfigAsync(
        string namespaceId,
        SameConfigPolicy policy,
        byte[] fileContent,
        string fileName,
        string? srcUser = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports configurations to a zip file.
    /// </summary>
    /// <param name="request">Export request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Zip file content as byte array.</returns>
    Task<byte[]> ExportConfigAsync(ConfigExportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports configurations by IDs.
    /// </summary>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="ids">Configuration IDs to export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Zip file content as byte array.</returns>
    Task<byte[]> ExportConfigByIdsAsync(
        string namespaceId,
        IEnumerable<long> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all configurations in a namespace.
    /// </summary>
    /// <param name="namespaceId">Namespace ID.</param>
    /// <param name="dataId">Optional data ID filter.</param>
    /// <param name="group">Optional group filter.</param>
    /// <param name="appName">Optional app name filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Zip file content as byte array.</returns>
    Task<byte[]> ExportAllConfigAsync(
        string namespaceId,
        string? dataId = null,
        string? group = null,
        string? appName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones configurations to another namespace.
    /// </summary>
    /// <param name="sourceNamespaceId">Source namespace ID.</param>
    /// <param name="targetNamespaceId">Target namespace ID.</param>
    /// <param name="ids">Configuration IDs to clone.</param>
    /// <param name="policy">Same config policy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Clone result.</returns>
    Task<CloneResult> CloneConfigAsync(
        string sourceNamespaceId,
        string targetNamespaceId,
        IEnumerable<long> ids,
        SameConfigPolicy policy = SameConfigPolicy.Abort,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones all configurations to another namespace.
    /// </summary>
    /// <param name="sourceNamespaceId">Source namespace ID.</param>
    /// <param name="targetNamespaceId">Target namespace ID.</param>
    /// <param name="policy">Same config policy.</param>
    /// <param name="dataId">Optional data ID filter.</param>
    /// <param name="group">Optional group filter.</param>
    /// <param name="appName">Optional app name filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Clone result.</returns>
    Task<CloneResult> CloneAllConfigAsync(
        string sourceNamespaceId,
        string targetNamespaceId,
        SameConfigPolicy policy = SameConfigPolicy.Abort,
        string? dataId = null,
        string? group = null,
        string? appName = null,
        CancellationToken cancellationToken = default);
}
