using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Maintainer;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Http.Maintainer;

/// <summary>
/// HTTP implementation of the Nacos maintainer service.
/// </summary>
public class NacosMaintainerService : IMaintainerService
{
    private readonly NacosClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<NacosMaintainerService>? _logger;
    private volatile bool _disposed;
    private volatile string _serverStatus = "UP";

    private const string ServiceApiPath = "/nacos/v2/ns/service";
    private const string InstanceApiPath = "/nacos/v2/ns/instance";
    private const string CatalogApiPath = "/nacos/v2/ns/catalog";
    private const string OperatorApiPath = "/nacos/v2/ns/operator";
    private const string HealthApiPath = "/nacos/v2/ns/health";

    /// <summary>
    /// Initializes a new instance of the <see cref="NacosMaintainerService"/> class.
    /// </summary>
    public NacosMaintainerService(NacosClientOptions options, ILogger<NacosMaintainerService>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(GetServerAddress()),
            Timeout = TimeSpan.FromMilliseconds(_options.DefaultTimeout > 0 ? _options.DefaultTimeout : 10000)
        };
    }

    #region IServiceMaintainer

    public async Task<string> CreateServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return await CreateServiceAsync("DEFAULT_GROUP", serviceName, cancellationToken);
    }

    public async Task<string> CreateServiceAsync(string groupName, string serviceName, CancellationToken cancellationToken = default)
    {
        return await CreateServiceAsync(_options.Namespace ?? "", groupName, serviceName, true, 0f, cancellationToken);
    }

    public async Task<string> CreateServiceAsync(
        string namespaceId,
        string groupName,
        string serviceName,
        bool ephemeral = true,
        float protectThreshold = 0f,
        CancellationToken cancellationToken = default)
    {
        var service = new ServiceDefinition
        {
            NamespaceId = namespaceId,
            GroupName = groupName,
            Name = serviceName,
            Ephemeral = ephemeral,
            ProtectThreshold = protectThreshold
        };
        return await CreateServiceAsync(service, cancellationToken);
    }

    public async Task<string> CreateServiceAsync(ServiceDefinition service, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = service.Name,
                ["groupName"] = service.GroupName,
                ["namespaceId"] = service.NamespaceId ?? _options.Namespace ?? "",
                ["ephemeral"] = service.Ephemeral.ToString().ToLower(),
                ["protectThreshold"] = service.ProtectThreshold.ToString()
            };

            if (service.Metadata != null)
            {
                queryParams["metadata"] = JsonSerializer.Serialize(service.Metadata);
            }

            var url = BuildUrl(ServiceApiPath, queryParams);
            var response = await _httpClient.PostAsync(url, null, cancellationToken);
            
            return await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to create service {ServiceName}", service.Name);
            throw new NacosException(NacosException.ServerError, $"Failed to create service: {ex.Message}", ex);
        }
    }

    public async Task<string> UpdateServiceAsync(
        string serviceName,
        Dictionary<string, string>? newMetadata = null,
        float? newProtectThreshold = null,
        CancellationToken cancellationToken = default)
    {
        var service = new ServiceDefinition
        {
            Name = serviceName,
            GroupName = "DEFAULT_GROUP",
            Metadata = newMetadata,
            ProtectThreshold = newProtectThreshold ?? 0f
        };
        return await UpdateServiceAsync(service, cancellationToken);
    }

    public async Task<string> UpdateServiceAsync(ServiceDefinition service, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = service.Name,
                ["groupName"] = service.GroupName,
                ["namespaceId"] = service.NamespaceId ?? _options.Namespace ?? "",
                ["protectThreshold"] = service.ProtectThreshold.ToString()
            };

            if (service.Metadata != null)
            {
                queryParams["metadata"] = JsonSerializer.Serialize(service.Metadata);
            }

            var url = BuildUrl(ServiceApiPath, queryParams);
            var response = await _httpClient.PutAsync(url, null, cancellationToken);
            
            return await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to update service {ServiceName}", service.Name);
            throw new NacosException(NacosException.ServerError, $"Failed to update service: {ex.Message}", ex);
        }
    }

    public async Task<string> RemoveServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return await RemoveServiceAsync("DEFAULT_GROUP", serviceName, cancellationToken);
    }

    public async Task<string> RemoveServiceAsync(string groupName, string serviceName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? ""
            };

            var url = BuildUrl(ServiceApiPath, queryParams);
            var response = await _httpClient.DeleteAsync(url, cancellationToken);
            
            return await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to remove service {ServiceName}", serviceName);
            throw new NacosException(NacosException.ServerError, $"Failed to remove service: {ex.Message}", ex);
        }
    }

    public async Task<ServiceDetailInfo?> GetServiceDetailAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return await GetServiceDetailAsync("DEFAULT_GROUP", serviceName, cancellationToken);
    }

    public async Task<ServiceDetailInfo?> GetServiceDetailAsync(string groupName, string serviceName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? ""
            };

            var url = BuildUrl(ServiceApiPath, queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ServiceDetailInfo>(cancellationToken: cancellationToken);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get service detail {ServiceName}", serviceName);
            throw new NacosException(NacosException.ServerError, $"Failed to get service detail: {ex.Message}", ex);
        }
    }

    public async Task<Page<ServiceView>> ListServicesAsync(string namespaceId, CancellationToken cancellationToken = default)
    {
        return await ListServicesAsync(namespaceId, null, null, false, 1, 10, cancellationToken);
    }

    public async Task<Page<ServiceView>> ListServicesAsync(
        string namespaceId,
        string? groupNameParam = null,
        string? serviceNameParam = null,
        bool ignoreEmptyService = false,
        int pageNo = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["namespaceId"] = namespaceId,
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["ignoreEmptyService"] = ignoreEmptyService.ToString().ToLower()
            };

            if (!string.IsNullOrEmpty(groupNameParam))
            {
                queryParams["groupNameParam"] = groupNameParam;
            }

            if (!string.IsNullOrEmpty(serviceNameParam))
            {
                queryParams["serviceNameParam"] = serviceNameParam;
            }

            var url = BuildUrl($"{CatalogApiPath}/services", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Page<ServiceView>>(cancellationToken: cancellationToken)
                    ?? Page<ServiceView>.Empty();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to list services");
            throw new NacosException(NacosException.ServerError, $"Failed to list services: {ex.Message}", ex);
        }
    }

    public async Task<Page<ServiceDetailInfo>> ListServicesWithDetailAsync(
        string namespaceId,
        int pageNo = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["namespaceId"] = namespaceId,
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["withInstances"] = "true"
            };

            var url = BuildUrl($"{CatalogApiPath}/services", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Page<ServiceDetailInfo>>(cancellationToken: cancellationToken)
                    ?? Page<ServiceDetailInfo>.Empty();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to list services with detail");
            throw new NacosException(NacosException.ServerError, $"Failed to list services: {ex.Message}", ex);
        }
    }

    public async Task<Page<SubscriberInfo>> GetSubscribersAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return await GetSubscribersAsync("DEFAULT_GROUP", serviceName, 1, 10, false, cancellationToken);
    }

    public async Task<Page<SubscriberInfo>> GetSubscribersAsync(
        string groupName,
        string serviceName,
        int pageNo = 1,
        int pageSize = 10,
        bool aggregation = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? "",
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["aggregation"] = aggregation.ToString().ToLower()
            };

            var url = BuildUrl($"{ServiceApiPath}/subscribers", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Page<SubscriberInfo>>(cancellationToken: cancellationToken)
                    ?? Page<SubscriberInfo>.Empty();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get subscribers");
            throw new NacosException(NacosException.ServerError, $"Failed to get subscribers: {ex.Message}", ex);
        }
    }

    public async Task<List<string>> ListSelectorTypesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{ServiceApiPath}/selector/types";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: cancellationToken)
                    ?? new List<string>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to list selector types");
            throw new NacosException(NacosException.ServerError, $"Failed to list selector types: {ex.Message}", ex);
        }
    }

    #endregion

    #region IInstanceMaintainer

    public async Task<string> RegisterInstanceAsync(
        string serviceName,
        string ip,
        int port,
        CancellationToken cancellationToken = default)
    {
        var instance = new Instance { Ip = ip, Port = port };
        return await RegisterInstanceAsync("DEFAULT_GROUP", serviceName, instance, cancellationToken);
    }

    public async Task<string> RegisterInstanceAsync(
        string groupName,
        string serviceName,
        Instance instance,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? "",
                ["ip"] = instance.Ip,
                ["port"] = instance.Port.ToString(),
                ["weight"] = instance.Weight.ToString(),
                ["enabled"] = instance.Enabled.ToString().ToLower(),
                ["healthy"] = instance.Healthy.ToString().ToLower(),
                ["ephemeral"] = instance.Ephemeral.ToString().ToLower(),
                ["clusterName"] = instance.ClusterName ?? "DEFAULT"
            };

            if (instance.Metadata != null)
            {
                queryParams["metadata"] = JsonSerializer.Serialize(instance.Metadata);
            }

            var url = BuildUrl(InstanceApiPath, queryParams);
            var response = await _httpClient.PostAsync(url, null, cancellationToken);
            
            return await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to register instance {Ip}:{Port}", instance.Ip, instance.Port);
            throw new NacosException(NacosException.ServerError, $"Failed to register instance: {ex.Message}", ex);
        }
    }

    public async Task<string> DeregisterInstanceAsync(
        string serviceName,
        string ip,
        int port,
        CancellationToken cancellationToken = default)
    {
        var instance = new Instance { Ip = ip, Port = port };
        return await DeregisterInstanceAsync("DEFAULT_GROUP", serviceName, instance, cancellationToken);
    }

    public async Task<string> DeregisterInstanceAsync(
        string groupName,
        string serviceName,
        Instance instance,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? "",
                ["ip"] = instance.Ip,
                ["port"] = instance.Port.ToString(),
                ["clusterName"] = instance.ClusterName ?? "DEFAULT",
                ["ephemeral"] = instance.Ephemeral.ToString().ToLower()
            };

            var url = BuildUrl(InstanceApiPath, queryParams);
            var response = await _httpClient.DeleteAsync(url, cancellationToken);
            
            return await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to deregister instance {Ip}:{Port}", instance.Ip, instance.Port);
            throw new NacosException(NacosException.ServerError, $"Failed to deregister instance: {ex.Message}", ex);
        }
    }

    public async Task<string> UpdateInstanceAsync(
        string serviceName,
        Instance instance,
        CancellationToken cancellationToken = default)
    {
        return await UpdateInstanceAsync("DEFAULT_GROUP", serviceName, instance, cancellationToken);
    }

    public async Task<string> UpdateInstanceAsync(
        string groupName,
        string serviceName,
        Instance instance,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? "",
                ["ip"] = instance.Ip,
                ["port"] = instance.Port.ToString(),
                ["weight"] = instance.Weight.ToString(),
                ["enabled"] = instance.Enabled.ToString().ToLower(),
                ["healthy"] = instance.Healthy.ToString().ToLower(),
                ["ephemeral"] = instance.Ephemeral.ToString().ToLower(),
                ["clusterName"] = instance.ClusterName ?? "DEFAULT"
            };

            if (instance.Metadata != null)
            {
                queryParams["metadata"] = JsonSerializer.Serialize(instance.Metadata);
            }

            var url = BuildUrl(InstanceApiPath, queryParams);
            var response = await _httpClient.PutAsync(url, null, cancellationToken);
            
            return await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to update instance {Ip}:{Port}", instance.Ip, instance.Port);
            throw new NacosException(NacosException.ServerError, $"Failed to update instance: {ex.Message}", ex);
        }
    }

    public async Task<string> PartialUpdateInstanceAsync(
        string groupName,
        string serviceName,
        Instance instance,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? "",
                ["ip"] = instance.Ip,
                ["port"] = instance.Port.ToString(),
                ["clusterName"] = instance.ClusterName ?? "DEFAULT"
            };

            if (instance.Metadata != null)
            {
                queryParams["metadata"] = JsonSerializer.Serialize(instance.Metadata);
            }

            var url = BuildUrl($"{InstanceApiPath}/metadata", queryParams);
            var response = await _httpClient.PutAsync(url, null, cancellationToken);
            
            return await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to partial update instance {Ip}:{Port}", instance.Ip, instance.Port);
            throw new NacosException(NacosException.ServerError, $"Failed to partial update instance: {ex.Message}", ex);
        }
    }

    public async Task<InstanceMetadataBatchResult> BatchUpdateInstanceMetadataAsync(
        string groupName,
        string serviceName,
        List<Instance> instances,
        Dictionary<string, string> newMetadata,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var instanceKeys = instances.Select(i => $"{i.Ip}:{i.Port}:{i.ClusterName ?? "DEFAULT"}").ToList();
            
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? "",
                ["instances"] = JsonSerializer.Serialize(instanceKeys),
                ["metadata"] = JsonSerializer.Serialize(newMetadata)
            };

            var url = BuildUrl($"{InstanceApiPath}/metadata/batch", queryParams);
            var response = await _httpClient.PutAsync(url, null, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<InstanceMetadataBatchResult>(cancellationToken: cancellationToken)
                    ?? new InstanceMetadataBatchResult { Updated = false };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to batch update instance metadata");
            throw new NacosException(NacosException.ServerError, $"Failed to batch update: {ex.Message}", ex);
        }
    }

    public async Task<InstanceMetadataBatchResult> BatchDeleteInstanceMetadataAsync(
        string groupName,
        string serviceName,
        List<Instance> instances,
        List<string> metadataKeys,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var instanceKeys = instances.Select(i => $"{i.Ip}:{i.Port}:{i.ClusterName ?? "DEFAULT"}").ToList();
            
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? "",
                ["instances"] = JsonSerializer.Serialize(instanceKeys),
                ["keys"] = JsonSerializer.Serialize(metadataKeys)
            };

            var url = BuildUrl($"{InstanceApiPath}/metadata/batch", queryParams);
            var response = await _httpClient.DeleteAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<InstanceMetadataBatchResult>(cancellationToken: cancellationToken)
                    ?? new InstanceMetadataBatchResult { Updated = false };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to batch delete instance metadata");
            throw new NacosException(NacosException.ServerError, $"Failed to batch delete: {ex.Message}", ex);
        }
    }

    public async Task<List<Instance>> ListInstancesAsync(
        string serviceName,
        string? clusterName = null,
        bool healthyOnly = false,
        CancellationToken cancellationToken = default)
    {
        return await ListInstancesAsync("DEFAULT_GROUP", serviceName, clusterName, healthyOnly, cancellationToken);
    }

    public async Task<List<Instance>> ListInstancesAsync(
        string groupName,
        string serviceName,
        string? clusterName = null,
        bool healthyOnly = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? "",
                ["healthyOnly"] = healthyOnly.ToString().ToLower()
            };

            if (!string.IsNullOrEmpty(clusterName))
            {
                queryParams["clusters"] = clusterName;
            }

            var url = BuildUrl($"{InstanceApiPath}/list", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<InstanceListResult>(cancellationToken: cancellationToken);
                return result?.Hosts ?? new List<Instance>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to list instances");
            throw new NacosException(NacosException.ServerError, $"Failed to list instances: {ex.Message}", ex);
        }
    }

    public async Task<Instance?> GetInstanceDetailAsync(
        string serviceName,
        string ip,
        int port,
        CancellationToken cancellationToken = default)
    {
        return await GetInstanceDetailAsync("DEFAULT_GROUP", serviceName, ip, port, null, cancellationToken);
    }

    public async Task<Instance?> GetInstanceDetailAsync(
        string groupName,
        string serviceName,
        string ip,
        int port,
        string? clusterName = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? "",
                ["ip"] = ip,
                ["port"] = port.ToString(),
                ["cluster"] = clusterName ?? "DEFAULT"
            };

            var url = BuildUrl(InstanceApiPath, queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Instance>(cancellationToken: cancellationToken);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get instance detail {Ip}:{Port}", ip, port);
            throw new NacosException(NacosException.ServerError, $"Failed to get instance detail: {ex.Message}", ex);
        }
    }

    #endregion

    #region INamingMaintainer

    public async Task<MetricsInfo> GetMetricsAsync(bool onlyStatus = false, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["onlyStatus"] = onlyStatus.ToString().ToLower()
            };

            var url = BuildUrl($"{OperatorApiPath}/metrics", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MetricsInfo>(cancellationToken: cancellationToken)
                    ?? new MetricsInfo();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get metrics");
            throw new NacosException(NacosException.ServerError, $"Failed to get metrics: {ex.Message}", ex);
        }
    }

    public async Task<string> SetLogLevelAsync(string logName, string logLevel, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["logName"] = logName,
                ["logLevel"] = logLevel
            };

            var url = BuildUrl($"{OperatorApiPath}/log", queryParams);
            var response = await _httpClient.PutAsync(url, null, cancellationToken);
            
            return await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to set log level");
            throw new NacosException(NacosException.ServerError, $"Failed to set log level: {ex.Message}", ex);
        }
    }

    public async Task<string> UpdateInstanceHealthStatusAsync(
        string groupName,
        string serviceName,
        string ip,
        int port,
        bool healthy,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? "",
                ["ip"] = ip,
                ["port"] = port.ToString(),
                ["healthy"] = healthy.ToString().ToLower()
            };

            var url = BuildUrl($"{HealthApiPath}/instance", queryParams);
            var response = await _httpClient.PutAsync(url, null, cancellationToken);
            
            return await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to update instance health status");
            throw new NacosException(NacosException.ServerError, $"Failed to update health status: {ex.Message}", ex);
        }
    }

    public async Task<Dictionary<string, HealthCheckerInfo>> GetHealthCheckersAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{HealthApiPath}/checkers";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Dictionary<string, HealthCheckerInfo>>(cancellationToken: cancellationToken)
                    ?? new Dictionary<string, HealthCheckerInfo>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get health checkers");
            throw new NacosException(NacosException.ServerError, $"Failed to get health checkers: {ex.Message}", ex);
        }
    }

    public async Task<string> UpdateClusterAsync(
        string groupName,
        string serviceName,
        ClusterInfo cluster,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["serviceName"] = serviceName,
                ["groupName"] = groupName,
                ["namespaceId"] = _options.Namespace ?? "",
                ["clusterName"] = cluster.Name,
                ["checkPort"] = cluster.DefaultCheckPort.ToString(),
                ["useInstancePort4Check"] = cluster.UseInstancePortForCheck.ToString().ToLower()
            };

            if (cluster.HealthChecker != null)
            {
                queryParams["healthChecker"] = JsonSerializer.Serialize(cluster.HealthChecker);
            }

            if (cluster.Metadata != null)
            {
                queryParams["metadata"] = JsonSerializer.Serialize(cluster.Metadata);
            }

            var url = BuildUrl($"{CatalogApiPath}/cluster", queryParams);
            var response = await _httpClient.PutAsync(url, null, cancellationToken);
            
            return await HandleResponse(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to update cluster");
            throw new NacosException(NacosException.ServerError, $"Failed to update cluster: {ex.Message}", ex);
        }
    }

    #endregion

    #region IMaintainerService

    public string GetServerStatus() => _serverStatus;

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return;

        _serverStatus = "DOWN";
        _disposed = true;
        _logger?.LogInformation("Maintainer service shutdown completed");
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await ShutdownAsync();
            _httpClient.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Helper Methods

    private string GetServerAddress()
    {
        var addresses = _options.ServerAddresses?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (addresses == null || addresses.Length == 0)
        {
            throw new ArgumentException("Server addresses not configured");
        }

        var address = addresses[0].Trim();
        if (!address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            address = "http://" + address;
        }

        return address;
    }

    private static string BuildUrl(string path, Dictionary<string, string> queryParams)
    {
        var query = string.Join("&", queryParams.Select(kvp => 
            $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));
        return $"{path}?{query}";
    }

    private static async Task<string> HandleResponse(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            return content;
        }

        throw new NacosException((int)response.StatusCode, content);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NacosMaintainerService));
        }
    }

    private class InstanceListResult
    {
        public List<Instance>? Hosts { get; set; }
    }

    #endregion
}
