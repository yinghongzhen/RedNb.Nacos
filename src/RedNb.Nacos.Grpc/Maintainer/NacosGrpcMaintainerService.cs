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

    // IConfigMaintainer
    public Task<ConfigDetailInfo?> GetConfigAsync(string dataId, CancellationToken cancellationToken = default) => _httpImpl.GetConfigAsync(dataId, cancellationToken);
    public Task<ConfigDetailInfo?> GetConfigAsync(string dataId, string group, CancellationToken cancellationToken = default) => _httpImpl.GetConfigAsync(dataId, group, cancellationToken);
    public Task<ConfigDetailInfo?> GetConfigAsync(string dataId, string group, string namespaceId, CancellationToken cancellationToken = default) => _httpImpl.GetConfigAsync(dataId, group, namespaceId, cancellationToken);
    public Task<bool> PublishConfigAsync(string dataId, string content, CancellationToken cancellationToken = default) => _httpImpl.PublishConfigAsync(dataId, content, cancellationToken);
    public Task<bool> PublishConfigAsync(string dataId, string group, string content, CancellationToken cancellationToken = default) => _httpImpl.PublishConfigAsync(dataId, group, content, cancellationToken);
    public Task<bool> PublishConfigAsync(string dataId, string group, string namespaceId, string content, string? description = null, string? type = null, string? appName = null, string? srcUser = null, string? configTags = null, CancellationToken cancellationToken = default) => _httpImpl.PublishConfigAsync(dataId, group, namespaceId, content, description, type, appName, srcUser, configTags, cancellationToken);
    public Task<bool> UpdateConfigMetadataAsync(string dataId, string group, string namespaceId, string? description, string? configTags, CancellationToken cancellationToken = default) => _httpImpl.UpdateConfigMetadataAsync(dataId, group, namespaceId, description, configTags, cancellationToken);
    public Task<bool> DeleteConfigAsync(string dataId, CancellationToken cancellationToken = default) => _httpImpl.DeleteConfigAsync(dataId, cancellationToken);
    public Task<bool> DeleteConfigAsync(string dataId, string group, CancellationToken cancellationToken = default) => _httpImpl.DeleteConfigAsync(dataId, group, cancellationToken);
    public Task<bool> DeleteConfigAsync(string dataId, string group, string namespaceId, CancellationToken cancellationToken = default) => _httpImpl.DeleteConfigAsync(dataId, group, namespaceId, cancellationToken);
    public Task<bool> DeleteConfigsAsync(List<long> ids, CancellationToken cancellationToken = default) => _httpImpl.DeleteConfigsAsync(ids, cancellationToken);
    public Task<Page<ConfigBasicInfo>> ListConfigsAsync(string namespaceId, CancellationToken cancellationToken = default) => _httpImpl.ListConfigsAsync(namespaceId, cancellationToken);
    public Task<Page<ConfigBasicInfo>> ListConfigsAsync(string? dataId, string? group, string namespaceId, string? type = null, string? configTags = null, string? appName = null, int pageNo = 1, int pageSize = 100, CancellationToken cancellationToken = default) => _httpImpl.ListConfigsAsync(dataId, group, namespaceId, type, configTags, appName, pageNo, pageSize, cancellationToken);
    public Task<Page<ConfigBasicInfo>> SearchConfigsAsync(string? dataIdPattern, string? groupPattern, string namespaceId, CancellationToken cancellationToken = default) => _httpImpl.SearchConfigsAsync(dataIdPattern, groupPattern, namespaceId, cancellationToken);
    public Task<Page<ConfigBasicInfo>> SearchConfigsAsync(string? dataIdPattern, string? groupPattern, string namespaceId, string? configDetail, string? type, string? configTags, string? appName, int pageNo = 1, int pageSize = 100, CancellationToken cancellationToken = default) => _httpImpl.SearchConfigsAsync(dataIdPattern, groupPattern, namespaceId, configDetail, type, configTags, appName, pageNo, pageSize, cancellationToken);
    public Task<List<ConfigBasicInfo>> GetConfigListByNamespaceAsync(string namespaceId, CancellationToken cancellationToken = default) => _httpImpl.GetConfigListByNamespaceAsync(namespaceId, cancellationToken);
    public Task<ConfigListenerInfo> GetListenersAsync(string dataId, string group, CancellationToken cancellationToken = default) => _httpImpl.GetListenersAsync(dataId, group, cancellationToken);
    public Task<ConfigListenerInfo> GetListenersAsync(string dataId, string group, string namespaceId, bool aggregation = true, CancellationToken cancellationToken = default) => _httpImpl.GetListenersAsync(dataId, group, namespaceId, aggregation, cancellationToken);
    public Task<ConfigListenerInfo> GetAllSubClientConfigByIpAsync(string ip, bool all = false, string? namespaceId = null, bool aggregation = false, CancellationToken cancellationToken = default) => _httpImpl.GetAllSubClientConfigByIpAsync(ip, all, namespaceId, aggregation, cancellationToken);
    public Task<CloneResult> CloneConfigAsync(string namespaceId, List<ConfigCloneInfo> cloneInfos, SameConfigPolicy policy, string? srcUser = null, CancellationToken cancellationToken = default) => _httpImpl.CloneConfigAsync(namespaceId, cloneInfos, policy, srcUser, cancellationToken);

    // IConfigHistoryMaintainer
    public Task<Page<ConfigHistoryBasicInfo>> ListConfigHistoryAsync(string dataId, string group, string namespaceId, int pageNo = 1, int pageSize = 100, CancellationToken cancellationToken = default) => _httpImpl.ListConfigHistoryAsync(dataId, group, namespaceId, pageNo, pageSize, cancellationToken);
    public Task<ConfigHistoryDetailInfo?> GetConfigHistoryInfoAsync(string dataId, string group, string namespaceId, long nid, CancellationToken cancellationToken = default) => _httpImpl.GetConfigHistoryInfoAsync(dataId, group, namespaceId, nid, cancellationToken);
    public Task<ConfigHistoryDetailInfo?> GetPreviousConfigHistoryInfoAsync(string dataId, string group, string namespaceId, long id, CancellationToken cancellationToken = default) => _httpImpl.GetPreviousConfigHistoryInfoAsync(dataId, group, namespaceId, id, cancellationToken);

    // IBetaConfigMaintainer
    public Task<BetaConfigInfo?> GetBetaConfigAsync(string dataId, string group, CancellationToken cancellationToken = default) => _httpImpl.GetBetaConfigAsync(dataId, group, cancellationToken);
    public Task<BetaConfigInfo?> GetBetaConfigAsync(string dataId, string group, string namespaceId, CancellationToken cancellationToken = default) => _httpImpl.GetBetaConfigAsync(dataId, group, namespaceId, cancellationToken);
    public Task<bool> PublishBetaConfigAsync(string dataId, string group, string content, string betaIps, CancellationToken cancellationToken = default) => _httpImpl.PublishBetaConfigAsync(dataId, group, content, betaIps, cancellationToken);
    public Task<bool> PublishBetaConfigAsync(string dataId, string group, string namespaceId, string content, string betaIps, string? description = null, string? type = null, string? appName = null, string? srcUser = null, CancellationToken cancellationToken = default) => _httpImpl.PublishBetaConfigAsync(dataId, group, namespaceId, content, betaIps, description, type, appName, srcUser, cancellationToken);
    public Task<bool> StopBetaConfigAsync(string dataId, string group, CancellationToken cancellationToken = default) => _httpImpl.StopBetaConfigAsync(dataId, group, cancellationToken);
    public Task<bool> StopBetaConfigAsync(string dataId, string group, string namespaceId, CancellationToken cancellationToken = default) => _httpImpl.StopBetaConfigAsync(dataId, group, namespaceId, cancellationToken);
    public Task<GrayConfigInfo?> GetGrayConfigAsync(string dataId, string group, string namespaceId, string grayName, CancellationToken cancellationToken = default) => _httpImpl.GetGrayConfigAsync(dataId, group, namespaceId, grayName, cancellationToken);
    public Task<bool> PublishGrayConfigAsync(string dataId, string group, string namespaceId, string content, GrayConfigRule grayRule, string? description = null, string? type = null, string? srcUser = null, CancellationToken cancellationToken = default) => _httpImpl.PublishGrayConfigAsync(dataId, group, namespaceId, content, grayRule, description, type, srcUser, cancellationToken);
    public Task<bool> DeleteGrayConfigAsync(string dataId, string group, string namespaceId, string grayName, CancellationToken cancellationToken = default) => _httpImpl.DeleteGrayConfigAsync(dataId, group, namespaceId, grayName, cancellationToken);

    // IConfigOpsMaintainer
    public Task<ConfigImportResult> ImportConfigAsync(string namespaceId, SameConfigPolicy policy, byte[] fileContent, string fileName, string? srcUser = null, CancellationToken cancellationToken = default) => _httpImpl.ImportConfigAsync(namespaceId, policy, fileContent, fileName, srcUser, cancellationToken);
    public Task<byte[]> ExportConfigAsync(ConfigExportRequest request, CancellationToken cancellationToken = default) => _httpImpl.ExportConfigAsync(request, cancellationToken);
    public Task<byte[]> ExportConfigByIdsAsync(string namespaceId, IEnumerable<long> ids, CancellationToken cancellationToken = default) => _httpImpl.ExportConfigByIdsAsync(namespaceId, ids, cancellationToken);
    public Task<byte[]> ExportAllConfigAsync(string namespaceId, string? dataId = null, string? group = null, string? appName = null, CancellationToken cancellationToken = default) => _httpImpl.ExportAllConfigAsync(namespaceId, dataId, group, appName, cancellationToken);
    public Task<CloneResult> CloneConfigAsync(string sourceNamespaceId, string targetNamespaceId, IEnumerable<long> ids, SameConfigPolicy policy = SameConfigPolicy.Abort, CancellationToken cancellationToken = default) => _httpImpl.CloneConfigAsync(sourceNamespaceId, targetNamespaceId, ids, policy, cancellationToken);
    public Task<CloneResult> CloneAllConfigAsync(string sourceNamespaceId, string targetNamespaceId, SameConfigPolicy policy = SameConfigPolicy.Abort, string? dataId = null, string? group = null, string? appName = null, CancellationToken cancellationToken = default) => _httpImpl.CloneAllConfigAsync(sourceNamespaceId, targetNamespaceId, policy, dataId, group, appName, cancellationToken);

    // IClientMaintainer
    public Task<IEnumerable<ClientConnectionInfo>> ListClientsAsync(CancellationToken cancellationToken = default) => _httpImpl.ListClientsAsync(cancellationToken);
    public Task<ConnectionListResult> ListNamingClientsAsync(CancellationToken cancellationToken = default) => _httpImpl.ListNamingClientsAsync(cancellationToken);
    public Task<ConnectionListResult> ListConfigClientsAsync(CancellationToken cancellationToken = default) => _httpImpl.ListConfigClientsAsync(cancellationToken);
    public Task<ClientDetailInfo?> GetClientDetailAsync(string clientId, CancellationToken cancellationToken = default) => _httpImpl.GetClientDetailAsync(clientId, cancellationToken);
    public Task<IEnumerable<ClientSubscribedService>> GetClientSubscribersAsync(string clientId, CancellationToken cancellationToken = default) => _httpImpl.GetClientSubscribersAsync(clientId, cancellationToken);
    public Task<IEnumerable<ClientPublishedService>> GetClientPublishedServicesAsync(string clientId, CancellationToken cancellationToken = default) => _httpImpl.GetClientPublishedServicesAsync(clientId, cancellationToken);
    public Task<IEnumerable<ConfigListenerInfo>> GetClientListenConfigsAsync(string clientId, CancellationToken cancellationToken = default) => _httpImpl.GetClientListenConfigsAsync(clientId, cancellationToken);
    public Task<bool> ReloadConnectionCountAsync(CancellationToken cancellationToken = default) => _httpImpl.ReloadConnectionCountAsync(cancellationToken);
    public Task<IDictionary<string, int>> GetSdkVersionStatisticsAsync(CancellationToken cancellationToken = default) => _httpImpl.GetSdkVersionStatisticsAsync(cancellationToken);
    public Task<IDictionary<string, object>> GetCurrentNodeStatsAsync(CancellationToken cancellationToken = default) => _httpImpl.GetCurrentNodeStatsAsync(cancellationToken);
    public Task<bool> ResetConnectionLimitAsync(string clientId, int count, CancellationToken cancellationToken = default) => _httpImpl.ResetConnectionLimitAsync(clientId, count, cancellationToken);

    // ICoreMaintainer
    public Task<IEnumerable<NamespaceInfo>> GetNamespacesAsync(CancellationToken cancellationToken = default) => _httpImpl.GetNamespacesAsync(cancellationToken);
    public Task<NamespaceInfo?> GetNamespaceAsync(string namespaceId, CancellationToken cancellationToken = default) => _httpImpl.GetNamespaceAsync(namespaceId, cancellationToken);
    public Task<bool> CreateNamespaceAsync(NamespaceRequest request, CancellationToken cancellationToken = default) => _httpImpl.CreateNamespaceAsync(request, cancellationToken);
    public Task<bool> UpdateNamespaceAsync(NamespaceRequest request, CancellationToken cancellationToken = default) => _httpImpl.UpdateNamespaceAsync(request, cancellationToken);
    public Task<bool> DeleteNamespaceAsync(string namespaceId, CancellationToken cancellationToken = default) => _httpImpl.DeleteNamespaceAsync(namespaceId, cancellationToken);
    public Task<IEnumerable<ClusterMemberInfo>> GetClusterMembersAsync(CancellationToken cancellationToken = default) => _httpImpl.GetClusterMembersAsync(cancellationToken);
    public Task<string> GetSelfNodeAsync(CancellationToken cancellationToken = default) => _httpImpl.GetSelfNodeAsync(cancellationToken);
    public Task<ClusterMemberInfo?> GetClusterLeaderAsync(CancellationToken cancellationToken = default) => _httpImpl.GetClusterLeaderAsync(cancellationToken);
    public Task<bool> UpdateClusterMemberLookupAsync(string type, CancellationToken cancellationToken = default) => _httpImpl.UpdateClusterMemberLookupAsync(type, cancellationToken);
    public Task<bool> LeaveClusterAsync(IEnumerable<string> addresses, CancellationToken cancellationToken = default) => _httpImpl.LeaveClusterAsync(addresses, cancellationToken);
    public async Task<ServerStateInfo> GetServerStateAsync(CancellationToken cancellationToken = default) => await _httpImpl.GetServerStateAsync(cancellationToken) ?? new ServerStateInfo();
    public async Task<ServerSwitchInfo> GetServerSwitchesAsync(CancellationToken cancellationToken = default) => await _httpImpl.GetServerSwitchesAsync(cancellationToken) ?? new ServerSwitchInfo();
    public Task<bool> UpdateServerSwitchAsync(string entry, string value, bool debug = false, CancellationToken cancellationToken = default) => _httpImpl.UpdateServerSwitchAsync(entry, value, debug, cancellationToken);
    public Task<bool> GetReadinessAsync(CancellationToken cancellationToken = default) => _httpImpl.GetReadinessAsync(cancellationToken);
    public Task<bool> GetLivenessAsync(CancellationToken cancellationToken = default) => _httpImpl.GetLivenessAsync(cancellationToken);
    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default) => _httpImpl.CheckHealthAsync(cancellationToken);
    public Task<MetricsInfo> GetMetricsAsync(CancellationToken cancellationToken = default) => _httpImpl.GetMetricsAsync(false, cancellationToken);
    public Task<string> GetPrometheusMetricsAsync(CancellationToken cancellationToken = default) => _httpImpl.GetPrometheusMetricsAsync(cancellationToken);
    public Task<string> GetRaftLeaderAsync(string groupId, CancellationToken cancellationToken = default) => _httpImpl.GetRaftLeaderAsync(groupId, cancellationToken);
    public Task<bool> TransferRaftLeaderAsync(string groupId, string newLeader, CancellationToken cancellationToken = default) => _httpImpl.TransferRaftLeaderAsync(groupId, newLeader, cancellationToken);
    public Task<bool> ResetRaftClusterAsync(string groupId, CancellationToken cancellationToken = default) => _httpImpl.ResetRaftClusterAsync(groupId, cancellationToken);
}
