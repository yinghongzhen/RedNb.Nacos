namespace RedNb.Nacos.Core.Maintainer;

/// <summary>
/// Combined maintainer service interface that includes all maintenance operations.
/// </summary>
public interface IMaintainerService :
    // Naming maintainer interfaces
    IServiceMaintainer,
    IInstanceMaintainer,
    INamingMaintainer,
    // Config maintainer interfaces
    IConfigMaintainer,
    IConfigHistoryMaintainer,
    IBetaConfigMaintainer,
    IConfigOpsMaintainer,
    // Client maintainer interface
    IClientMaintainer,
    // Core maintainer interface
    ICoreMaintainer,
    IAsyncDisposable
{
    /// <summary>
    /// Gets the server status.
    /// </summary>
    string GetServerStatus();

    /// <summary>
    /// Shuts down the maintainer service.
    /// </summary>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
