using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Maintainer;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Http.Maintainer;

namespace RedNb.Nacos.Grpc.Maintainer;

/// <summary>
/// gRPC MaintainerService 适配器，实际复用 HTTP 实现。
/// </summary>
public class NacosGrpcMaintainerService : IMaintainerService
{
    private readonly NacosMaintainerService _httpImpl;

    /// <summary>
    /// Initializes a new instance of the <see cref="NacosGrpcMaintainerService"/> class.
    /// </summary>
    public NacosGrpcMaintainerService(NacosClientOptions options, ILogger<NacosGrpcMaintainerService>? logger = null)
    {
        // Note: We pass null for logger since the types don't match; HTTP impl will create its own if needed
        _httpImpl = new NacosMaintainerService(options, null);
    }

    /// <inheritdoc/>
    public string GetServerStatus() => _httpImpl.GetServerStatus();
    public Task ShutdownAsync(CancellationToken cancellationToken = default) => _httpImpl.ShutdownAsync(cancellationToken);
    public ValueTask DisposeAsync() => _httpImpl.DisposeAsync();

    // IServiceMaintainer
    public Task<string> CreateServiceAsync(string serviceName, CancellationToken cancellationToken = default) => _httpImpl.CreateServiceAsync(serviceName, cancellationToken);
    public Task<string> CreateServiceAsync(string groupName, string serviceName, CancellationToken cancellationToken = default) => _httpImpl.CreateServiceAsync(groupName, serviceName, cancellationToken);
    public Task<string> CreateServiceAsync(string namespaceId, string groupName, string serviceName, bool ephemeral = true, float protectThreshold = 0f, CancellationToken cancellationToken = default) => _httpImpl.CreateServiceAsync(namespaceId, groupName, serviceName, ephemeral, protectThreshold, cancellationToken);
    public Task<string> CreateServiceAsync(ServiceDefinition service, CancellationToken cancellationToken = default) => _httpImpl.CreateServiceAsync(service, cancellationToken);
    public Task<string> UpdateServiceAsync(string serviceName, Dictionary<string, string>? newMetadata = null, float? newProtectThreshold = null, CancellationToken cancellationToken = default) => _httpImpl.UpdateServiceAsync(serviceName, newMetadata, newProtectThreshold, cancellationToken);
    public Task<string> UpdateServiceAsync(ServiceDefinition service, CancellationToken cancellationToken = default) => _httpImpl.UpdateServiceAsync(service, cancellationToken);
    public Task<string> RemoveServiceAsync(string serviceName, CancellationToken cancellationToken = default) => _httpImpl.RemoveServiceAsync(serviceName, cancellationToken);
    public Task<string> RemoveServiceAsync(string groupName, string serviceName, CancellationToken cancellationToken = default) => _httpImpl.RemoveServiceAsync(groupName, serviceName, cancellationToken);
    public Task<ServiceDetailInfo?> GetServiceDetailAsync(string serviceName, CancellationToken cancellationToken = default) => _httpImpl.GetServiceDetailAsync(serviceName, cancellationToken);
    public Task<ServiceDetailInfo?> GetServiceDetailAsync(string groupName, string serviceName, CancellationToken cancellationToken = default) => _httpImpl.GetServiceDetailAsync(groupName, serviceName, cancellationToken);
    public Task<Page<ServiceView>> ListServicesAsync(string namespaceId, CancellationToken cancellationToken = default) => _httpImpl.ListServicesAsync(namespaceId, cancellationToken);
    public Task<Page<ServiceView>> ListServicesAsync(string namespaceId, string? groupNameParam = null, string? serviceNameParam = null, bool ignoreEmptyService = false, int pageNo = 1, int pageSize = 10, CancellationToken cancellationToken = default) => _httpImpl.ListServicesAsync(namespaceId, groupNameParam, serviceNameParam, ignoreEmptyService, pageNo, pageSize, cancellationToken);
    public Task<Page<ServiceDetailInfo>> ListServicesWithDetailAsync(string namespaceId, int pageNo = 1, int pageSize = 10, CancellationToken cancellationToken = default) => _httpImpl.ListServicesWithDetailAsync(namespaceId, pageNo, pageSize, cancellationToken);
    public Task<Page<SubscriberInfo>> GetSubscribersAsync(string serviceName, CancellationToken cancellationToken = default) => _httpImpl.GetSubscribersAsync(serviceName, cancellationToken);
    public Task<Page<SubscriberInfo>> GetSubscribersAsync(string groupName, string serviceName, int pageNo = 1, int pageSize = 10, bool aggregation = false, CancellationToken cancellationToken = default) => _httpImpl.GetSubscribersAsync(groupName, serviceName, pageNo, pageSize, aggregation, cancellationToken);
    public Task<List<string>> ListSelectorTypesAsync(CancellationToken cancellationToken = default) => _httpImpl.ListSelectorTypesAsync(cancellationToken);

    // IInstanceMaintainer
    public Task<string> RegisterInstanceAsync(string serviceName, string ip, int port, CancellationToken cancellationToken = default) => _httpImpl.RegisterInstanceAsync(serviceName, ip, port, cancellationToken);
    public Task<string> RegisterInstanceAsync(string groupName, string serviceName, Instance instance, CancellationToken cancellationToken = default) => _httpImpl.RegisterInstanceAsync(groupName, serviceName, instance, cancellationToken);
    public Task<string> DeregisterInstanceAsync(string serviceName, string ip, int port, CancellationToken cancellationToken = default) => _httpImpl.DeregisterInstanceAsync(serviceName, ip, port, cancellationToken);
    public Task<string> DeregisterInstanceAsync(string groupName, string serviceName, Instance instance, CancellationToken cancellationToken = default) => _httpImpl.DeregisterInstanceAsync(groupName, serviceName, instance, cancellationToken);
    public Task<string> UpdateInstanceAsync(string serviceName, Instance instance, CancellationToken cancellationToken = default) => _httpImpl.UpdateInstanceAsync(serviceName, instance, cancellationToken);
    public Task<string> UpdateInstanceAsync(string groupName, string serviceName, Instance instance, CancellationToken cancellationToken = default) => _httpImpl.UpdateInstanceAsync(groupName, serviceName, instance, cancellationToken);
    public Task<string> PartialUpdateInstanceAsync(string groupName, string serviceName, Instance instance, CancellationToken cancellationToken = default) => _httpImpl.PartialUpdateInstanceAsync(groupName, serviceName, instance, cancellationToken);
    public Task<InstanceMetadataBatchResult> BatchUpdateInstanceMetadataAsync(string groupName, string serviceName, List<Instance> instances, Dictionary<string, string> newMetadata, CancellationToken cancellationToken = default) => _httpImpl.BatchUpdateInstanceMetadataAsync(groupName, serviceName, instances, newMetadata, cancellationToken);
    public Task<InstanceMetadataBatchResult> BatchDeleteInstanceMetadataAsync(string groupName, string serviceName, List<Instance> instances, List<string> metadataKeys, CancellationToken cancellationToken = default) => _httpImpl.BatchDeleteInstanceMetadataAsync(groupName, serviceName, instances, metadataKeys, cancellationToken);
    public Task<List<Instance>> ListInstancesAsync(string serviceName, string? clusterName = null, bool healthyOnly = false, CancellationToken cancellationToken = default) => _httpImpl.ListInstancesAsync(serviceName, clusterName, healthyOnly, cancellationToken);
    public Task<List<Instance>> ListInstancesAsync(string groupName, string serviceName, string? clusterName = null, bool healthyOnly = false, CancellationToken cancellationToken = default) => _httpImpl.ListInstancesAsync(groupName, serviceName, clusterName, healthyOnly, cancellationToken);
    public Task<Instance?> GetInstanceDetailAsync(string serviceName, string ip, int port, CancellationToken cancellationToken = default) => _httpImpl.GetInstanceDetailAsync(serviceName, ip, port, cancellationToken);
    public Task<Instance?> GetInstanceDetailAsync(string groupName, string serviceName, string ip, int port, string? clusterName = null, CancellationToken cancellationToken = default) => _httpImpl.GetInstanceDetailAsync(groupName, serviceName, ip, port, clusterName, cancellationToken);

    // INamingMaintainer
    public Task<MetricsInfo> GetMetricsAsync(bool onlyStatus = false, CancellationToken cancellationToken = default) => _httpImpl.GetMetricsAsync(onlyStatus, cancellationToken);
    public Task<string> SetLogLevelAsync(string logName, string logLevel, CancellationToken cancellationToken = default) => _httpImpl.SetLogLevelAsync(logName, logLevel, cancellationToken);
    public Task<string> UpdateInstanceHealthStatusAsync(string groupName, string serviceName, string ip, int port, bool healthy, CancellationToken cancellationToken = default) => _httpImpl.UpdateInstanceHealthStatusAsync(groupName, serviceName, ip, port, healthy, cancellationToken);
    public Task<Dictionary<string, HealthCheckerInfo>> GetHealthCheckersAsync(CancellationToken cancellationToken = default) => _httpImpl.GetHealthCheckersAsync(cancellationToken);
    public Task<string> UpdateClusterAsync(string groupName, string serviceName, ClusterInfo cluster, CancellationToken cancellationToken = default) => _httpImpl.UpdateClusterAsync(groupName, serviceName, cluster, cancellationToken);
}
