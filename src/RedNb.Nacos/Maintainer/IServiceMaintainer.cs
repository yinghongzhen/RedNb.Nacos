using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Core.Maintainer;

/// <summary>
/// Nacos service maintainer interface for service management operations.
/// </summary>
public interface IServiceMaintainer
{
    /// <summary>
    /// Creates a new service.
    /// </summary>
    Task<string> CreateServiceAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new service with group name.
    /// </summary>
    Task<string> CreateServiceAsync(string groupName, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new service with full configuration.
    /// </summary>
    Task<string> CreateServiceAsync(
        string namespaceId,
        string groupName,
        string serviceName,
        bool ephemeral = true,
        float protectThreshold = 0f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new service from service definition.
    /// </summary>
    Task<string> CreateServiceAsync(ServiceDefinition service, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing service.
    /// </summary>
    Task<string> UpdateServiceAsync(
        string serviceName,
        Dictionary<string, string>? newMetadata = null,
        float? newProtectThreshold = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing service from service definition.
    /// </summary>
    Task<string> UpdateServiceAsync(ServiceDefinition service, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a service.
    /// </summary>
    Task<string> RemoveServiceAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a service with group name.
    /// </summary>
    Task<string> RemoveServiceAsync(string groupName, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets service details.
    /// </summary>
    Task<ServiceDetailInfo?> GetServiceDetailAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets service details with group name.
    /// </summary>
    Task<ServiceDetailInfo?> GetServiceDetailAsync(string groupName, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists services in a namespace.
    /// </summary>
    Task<Page<ServiceView>> ListServicesAsync(string namespaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists services with pagination and filters.
    /// </summary>
    Task<Page<ServiceView>> ListServicesAsync(
        string namespaceId,
        string? groupNameParam = null,
        string? serviceNameParam = null,
        bool ignoreEmptyService = false,
        int pageNo = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists services with full detail information.
    /// </summary>
    Task<Page<ServiceDetailInfo>> ListServicesWithDetailAsync(
        string namespaceId,
        int pageNo = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscribers of a service.
    /// </summary>
    Task<Page<SubscriberInfo>> GetSubscribersAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscribers of a service with pagination.
    /// </summary>
    Task<Page<SubscriberInfo>> GetSubscribersAsync(
        string groupName,
        string serviceName,
        int pageNo = 1,
        int pageSize = 10,
        bool aggregation = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available selector types.
    /// </summary>
    Task<List<string>> ListSelectorTypesAsync(CancellationToken cancellationToken = default);
}
