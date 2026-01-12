namespace RedNb.Nacos.Core.Maintainer;

/// <summary>
/// Combined maintainer service interface that includes all maintenance operations.
/// </summary>
public interface IMaintainerService : IServiceMaintainer, IInstanceMaintainer, INamingMaintainer, IAsyncDisposable
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
