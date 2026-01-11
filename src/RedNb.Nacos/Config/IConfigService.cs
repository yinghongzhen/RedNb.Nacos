using RedNb.Nacos.Core.Config.Filter;
using RedNb.Nacos.Core.Config.FuzzyWatch;

namespace RedNb.Nacos.Core.Config;

/// <summary>
/// Config service interface for configuration management.
/// </summary>
public interface IConfigService : IAsyncDisposable
{
    /// <summary>
    /// Gets config value.
    /// </summary>
    /// <param name="dataId">Data ID</param>
    /// <param name="group">Group name</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Config value</returns>
    Task<string?> GetConfigAsync(string dataId, string group, long timeoutMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets config value and registers a listener.
    /// </summary>
    /// <param name="dataId">Data ID</param>
    /// <param name="group">Group name</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    /// <param name="listener">Config change listener</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Config value</returns>
    Task<string?> GetConfigAndSignListenerAsync(string dataId, string group, long timeoutMs, IConfigChangeListener listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a listener to the configuration.
    /// </summary>
    /// <param name="dataId">Data ID</param>
    /// <param name="group">Group name</param>
    /// <param name="listener">Config change listener</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddListenerAsync(string dataId, string group, IConfigChangeListener listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a listener from the configuration.
    /// </summary>
    /// <param name="dataId">Data ID</param>
    /// <param name="group">Group name</param>
    /// <param name="listener">Config change listener</param>
    void RemoveListener(string dataId, string group, IConfigChangeListener listener);

    /// <summary>
    /// Publishes config.
    /// </summary>
    /// <param name="dataId">Data ID</param>
    /// <param name="group">Group name</param>
    /// <param name="content">Config content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if published successfully</returns>
    Task<bool> PublishConfigAsync(string dataId, string group, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes config with type.
    /// </summary>
    /// <param name="dataId">Data ID</param>
    /// <param name="group">Group name</param>
    /// <param name="content">Config content</param>
    /// <param name="type">Config type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if published successfully</returns>
    Task<bool> PublishConfigAsync(string dataId, string group, string content, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes config with CAS (Compare-And-Swap).
    /// </summary>
    /// <param name="dataId">Data ID</param>
    /// <param name="group">Group name</param>
    /// <param name="content">Config content</param>
    /// <param name="casMd5">Previous content's MD5 for CAS</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if published successfully</returns>
    Task<bool> PublishConfigCasAsync(string dataId, string group, string content, string casMd5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes config with CAS and type.
    /// </summary>
    /// <param name="dataId">Data ID</param>
    /// <param name="group">Group name</param>
    /// <param name="content">Config content</param>
    /// <param name="casMd5">Previous content's MD5 for CAS</param>
    /// <param name="type">Config type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if published successfully</returns>
    Task<bool> PublishConfigCasAsync(string dataId, string group, string content, string casMd5, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes config.
    /// </summary>
    /// <param name="dataId">Data ID</param>
    /// <param name="group">Group name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if removed successfully</returns>
    Task<bool> RemoveConfigAsync(string dataId, string group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server status.
    /// </summary>
    /// <returns>"UP" if healthy, "DOWN" otherwise</returns>
    string GetServerStatus();

    /// <summary>
    /// Adds a config filter for intercepting config operations.
    /// </summary>
    /// <param name="configFilter">The filter to add</param>
    void AddConfigFilter(IConfigFilter configFilter);

    #region Fuzzy Watch (Nacos 3.0)

    /// <summary>
    /// Adds a fuzzy listener to configurations matching the group name pattern.
    /// </summary>
    /// <param name="groupNamePattern">Group name pattern (supports wildcards)</param>
    /// <param name="watcher">Event watcher</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task FuzzyWatchAsync(string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a fuzzy listener to configurations matching both dataId and group name patterns.
    /// </summary>
    /// <param name="dataIdPattern">DataId pattern (supports wildcards)</param>
    /// <param name="groupNamePattern">Group name pattern (supports wildcards)</param>
    /// <param name="watcher">Event watcher</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task FuzzyWatchAsync(string dataIdPattern, string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a fuzzy listener and retrieves matching group keys.
    /// </summary>
    /// <param name="groupNamePattern">Group name pattern</param>
    /// <param name="watcher">Event watcher</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching group keys</returns>
    Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a fuzzy listener and retrieves matching group keys.
    /// </summary>
    /// <param name="dataIdPattern">DataId pattern</param>
    /// <param name="groupNamePattern">Group name pattern</param>
    /// <param name="watcher">Event watcher</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching group keys</returns>
    Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string dataIdPattern, string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a fuzzy watch.
    /// </summary>
    /// <param name="groupNamePattern">Group name pattern</param>
    /// <param name="watcher">Event watcher to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CancelFuzzyWatchAsync(string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a fuzzy watch.
    /// </summary>
    /// <param name="dataIdPattern">DataId pattern</param>
    /// <param name="groupNamePattern">Group name pattern</param>
    /// <param name="watcher">Event watcher to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CancelFuzzyWatchAsync(string dataIdPattern, string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    #endregion

    /// <summary>
    /// Shuts down the config service.
    /// </summary>
    Task ShutdownAsync();
}
