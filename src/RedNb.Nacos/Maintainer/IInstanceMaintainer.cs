using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Core.Maintainer;

/// <summary>
/// Nacos instance maintainer interface for instance management operations.
/// </summary>
public interface IInstanceMaintainer
{
    /// <summary>
    /// Registers an instance.
    /// </summary>
    Task<string> RegisterInstanceAsync(
        string serviceName,
        string ip,
        int port,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an instance with full configuration.
    /// </summary>
    Task<string> RegisterInstanceAsync(
        string groupName,
        string serviceName,
        Instance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters an instance.
    /// </summary>
    Task<string> DeregisterInstanceAsync(
        string serviceName,
        string ip,
        int port,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters an instance with full configuration.
    /// </summary>
    Task<string> DeregisterInstanceAsync(
        string groupName,
        string serviceName,
        Instance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an instance.
    /// </summary>
    Task<string> UpdateInstanceAsync(
        string serviceName,
        Instance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an instance with group name.
    /// </summary>
    Task<string> UpdateInstanceAsync(
        string groupName,
        string serviceName,
        Instance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially updates an instance (only specified fields).
    /// </summary>
    Task<string> PartialUpdateInstanceAsync(
        string groupName,
        string serviceName,
        Instance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch updates instance metadata.
    /// </summary>
    Task<InstanceMetadataBatchResult> BatchUpdateInstanceMetadataAsync(
        string groupName,
        string serviceName,
        List<Instance> instances,
        Dictionary<string, string> newMetadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch deletes instance metadata.
    /// </summary>
    Task<InstanceMetadataBatchResult> BatchDeleteInstanceMetadataAsync(
        string groupName,
        string serviceName,
        List<Instance> instances,
        List<string> metadataKeys,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists instances of a service.
    /// </summary>
    Task<List<Instance>> ListInstancesAsync(
        string serviceName,
        string? clusterName = null,
        bool healthyOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists instances with group name.
    /// </summary>
    Task<List<Instance>> ListInstancesAsync(
        string groupName,
        string serviceName,
        string? clusterName = null,
        bool healthyOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets instance details.
    /// </summary>
    Task<Instance?> GetInstanceDetailAsync(
        string serviceName,
        string ip,
        int port,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets instance details with full parameters.
    /// </summary>
    Task<Instance?> GetInstanceDetailAsync(
        string groupName,
        string serviceName,
        string ip,
        int port,
        string? clusterName = null,
        CancellationToken cancellationToken = default);
}
