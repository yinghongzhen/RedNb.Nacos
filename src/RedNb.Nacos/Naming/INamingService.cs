using RedNb.Nacos.Core.Naming.FuzzyWatch;

namespace RedNb.Nacos.Core.Naming;

/// <summary>
/// Naming service interface for service registration and discovery.
/// </summary>
public interface INamingService : IAsyncDisposable
{
    #region Service Registration

    /// <summary>
    /// Registers an instance to service.
    /// </summary>
    Task RegisterInstanceAsync(string serviceName, string ip, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an instance to service with group name.
    /// </summary>
    Task RegisterInstanceAsync(string serviceName, string groupName, string ip, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an instance to service with cluster name.
    /// </summary>
    Task RegisterInstanceAsync(string serviceName, string ip, int port, string clusterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an instance to service with group and cluster name.
    /// </summary>
    Task RegisterInstanceAsync(string serviceName, string groupName, string ip, int port, string clusterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an instance to service.
    /// </summary>
    Task RegisterInstanceAsync(string serviceName, Instance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an instance to service with group name.
    /// </summary>
    Task RegisterInstanceAsync(string serviceName, string groupName, Instance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch registers instances to service.
    /// </summary>
    Task BatchRegisterInstanceAsync(string serviceName, string groupName, List<Instance> instances, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch deregisters instances from service.
    /// </summary>
    Task BatchDeregisterInstanceAsync(string serviceName, string groupName, List<Instance> instances, CancellationToken cancellationToken = default);

    #endregion

    #region Service Deregistration

    /// <summary>
    /// Deregisters an instance from service.
    /// </summary>
    Task DeregisterInstanceAsync(string serviceName, string ip, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters an instance from service with group name.
    /// </summary>
    Task DeregisterInstanceAsync(string serviceName, string groupName, string ip, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters an instance from service with cluster name.
    /// </summary>
    Task DeregisterInstanceAsync(string serviceName, string ip, int port, string clusterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters an instance from service with group and cluster name.
    /// </summary>
    Task DeregisterInstanceAsync(string serviceName, string groupName, string ip, int port, string clusterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters an instance from service.
    /// </summary>
    Task DeregisterInstanceAsync(string serviceName, Instance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters an instance from service with group name.
    /// </summary>
    Task DeregisterInstanceAsync(string serviceName, string groupName, Instance instance, CancellationToken cancellationToken = default);

    #endregion

    #region Instance Query

    /// <summary>
    /// Gets all instances of a service.
    /// </summary>
    Task<List<Instance>> GetAllInstancesAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all instances of a service with group name.
    /// </summary>
    Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all instances of a service.
    /// </summary>
    Task<List<Instance>> GetAllInstancesAsync(string serviceName, bool subscribe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all instances of a service with group name.
    /// </summary>
    Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName, bool subscribe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all instances within specified clusters.
    /// </summary>
    Task<List<Instance>> GetAllInstancesAsync(string serviceName, List<string> clusters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all instances within specified clusters with group name.
    /// </summary>
    Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName, List<string> clusters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all instances within specified clusters.
    /// </summary>
    Task<List<Instance>> GetAllInstancesAsync(string serviceName, List<string> clusters, bool subscribe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all instances within specified clusters with group name.
    /// </summary>
    Task<List<Instance>> GetAllInstancesAsync(string serviceName, string groupName, List<string> clusters, bool subscribe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects healthy or unhealthy instances.
    /// </summary>
    Task<List<Instance>> SelectInstancesAsync(string serviceName, bool healthy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects healthy or unhealthy instances with group name.
    /// </summary>
    Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, bool healthy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects healthy or unhealthy instances.
    /// </summary>
    Task<List<Instance>> SelectInstancesAsync(string serviceName, bool healthy, bool subscribe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects healthy or unhealthy instances with group name.
    /// </summary>
    Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, bool healthy, bool subscribe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects healthy or unhealthy instances within specified clusters.
    /// </summary>
    Task<List<Instance>> SelectInstancesAsync(string serviceName, List<string> clusters, bool healthy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects healthy or unhealthy instances within specified clusters with group name.
    /// </summary>
    Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, List<string> clusters, bool healthy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects healthy or unhealthy instances within specified clusters.
    /// </summary>
    Task<List<Instance>> SelectInstancesAsync(string serviceName, List<string> clusters, bool healthy, bool subscribe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects healthy or unhealthy instances within specified clusters with group name.
    /// </summary>
    Task<List<Instance>> SelectInstancesAsync(string serviceName, string groupName, List<string> clusters, bool healthy, bool subscribe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects one healthy instance using load balancing.
    /// </summary>
    Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects one healthy instance with group name.
    /// </summary>
    Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects one healthy instance.
    /// </summary>
    Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, bool subscribe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects one healthy instance with group name.
    /// </summary>
    Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, bool subscribe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects one healthy instance within specified clusters.
    /// </summary>
    Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, List<string> clusters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects one healthy instance within specified clusters with group name.
    /// </summary>
    Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, List<string> clusters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects one healthy instance within specified clusters.
    /// </summary>
    Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, List<string> clusters, bool subscribe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects one healthy instance within specified clusters with group name.
    /// </summary>
    Task<Instance?> SelectOneHealthyInstanceAsync(string serviceName, string groupName, List<string> clusters, bool subscribe, CancellationToken cancellationToken = default);

    #endregion

    #region Subscription

    /// <summary>
    /// Subscribes to service to receive events of instances change.
    /// </summary>
    Task SubscribeAsync(string serviceName, Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to service with group name.
    /// </summary>
    Task SubscribeAsync(string serviceName, string groupName, Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to service within specified clusters.
    /// </summary>
    Task SubscribeAsync(string serviceName, List<string> clusters, Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to service within specified clusters with group name.
    /// </summary>
    Task SubscribeAsync(string serviceName, string groupName, List<string> clusters, Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from service.
    /// </summary>
    Task UnsubscribeAsync(string serviceName, Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from service with group name.
    /// </summary>
    Task UnsubscribeAsync(string serviceName, string groupName, Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from service within specified clusters.
    /// </summary>
    Task UnsubscribeAsync(string serviceName, List<string> clusters, Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from service within specified clusters with group name.
    /// </summary>
    Task UnsubscribeAsync(string serviceName, string groupName, List<string> clusters, Action<IInstancesChangeEvent> listener, CancellationToken cancellationToken = default);

    #endregion

    #region Service List

    /// <summary>
    /// Gets all service names from server.
    /// </summary>
    Task<ListView<string>> GetServicesOfServerAsync(int pageNo, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all service names from server with group name.
    /// </summary>
    Task<ListView<string>> GetServicesOfServerAsync(int pageNo, int pageSize, string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subscribed services.
    /// </summary>
    Task<List<ServiceInfo>> GetSubscribeServicesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Server Status

    /// <summary>
    /// Gets server health status.
    /// </summary>
    string GetServerStatus();

    /// <summary>
    /// Shuts down the naming service.
    /// </summary>
    Task ShutdownAsync();

    #endregion

    #region Fuzzy Watch (Nacos 3.0)

    /// <summary>
    /// Adds a fuzzy listener to services matching the service name pattern.
    /// </summary>
    /// <param name="serviceNamePattern">Service name pattern (supports wildcards)</param>
    /// <param name="watcher">Event watcher</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task FuzzyWatchAsync(string serviceNamePattern, INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a fuzzy listener to services matching both service name and group name patterns.
    /// </summary>
    /// <param name="serviceNamePattern">Service name pattern (supports wildcards)</param>
    /// <param name="groupNamePattern">Group name pattern (supports wildcards)</param>
    /// <param name="watcher">Event watcher</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task FuzzyWatchAsync(string serviceNamePattern, string groupNamePattern, INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a fuzzy listener and retrieves matching group keys.
    /// </summary>
    /// <param name="serviceNamePattern">Service name pattern</param>
    /// <param name="watcher">Event watcher</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching group keys</returns>
    Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string serviceNamePattern, INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a fuzzy listener and retrieves matching group keys.
    /// </summary>
    /// <param name="serviceNamePattern">Service name pattern</param>
    /// <param name="groupNamePattern">Group name pattern</param>
    /// <param name="watcher">Event watcher</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching group keys</returns>
    Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string serviceNamePattern, string groupNamePattern, INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a fuzzy watch.
    /// </summary>
    /// <param name="serviceNamePattern">Service name pattern</param>
    /// <param name="watcher">Event watcher to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CancelFuzzyWatchAsync(string serviceNamePattern, INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a fuzzy watch.
    /// </summary>
    /// <param name="serviceNamePattern">Service name pattern</param>
    /// <param name="groupNamePattern">Group name pattern</param>
    /// <param name="watcher">Event watcher to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CancelFuzzyWatchAsync(string serviceNamePattern, string groupNamePattern, INamingFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default);

    #endregion
}
