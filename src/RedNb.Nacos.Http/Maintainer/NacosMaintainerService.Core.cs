using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Maintainer;

namespace RedNb.Nacos.Http.Maintainer;

/// <summary>
/// HTTP implementation of Client and Core Maintainer interfaces.
/// </summary>
public partial class NacosMaintainerService : IClientMaintainer, ICoreMaintainer
{
    private const string NamespaceApiPath = "/nacos/v2/console/namespace";
    private const string ClusterApiPath = "/nacos/v2/core/cluster";
    private const string ConnectionApiPath = "/nacos/v2/core/loader";
    private const string ServerStateApiPath = "/nacos/v2/ns/operator";
    private const string RaftApiPath = "/nacos/v2/core/ops/raft";

    #region IClientMaintainer

    public async Task<IEnumerable<ClientConnectionInfo>> ListClientsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{ConnectionApiPath}/current";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<IEnumerable<ClientConnectionInfo>>>(cancellationToken: cancellationToken);
                return result?.Data ?? Enumerable.Empty<ClientConnectionInfo>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to list clients");
            throw new NacosException(NacosException.ServerError, $"Failed to list clients: {ex.Message}", ex);
        }
    }

    public async Task<ConnectionListResult> ListNamingClientsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = "/nacos/v2/ns/client/list";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ConnectionListResult>>(cancellationToken: cancellationToken);
                return result?.Data ?? new ConnectionListResult();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to list naming clients");
            throw new NacosException(NacosException.ServerError, $"Failed to list naming clients: {ex.Message}", ex);
        }
    }

    public async Task<ConnectionListResult> ListConfigClientsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = "/nacos/v2/cs/client/list";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ConnectionListResult>>(cancellationToken: cancellationToken);
                return result?.Data ?? new ConnectionListResult();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to list config clients");
            throw new NacosException(NacosException.ServerError, $"Failed to list config clients: {ex.Message}", ex);
        }
    }

    public async Task<ClientDetailInfo?> GetClientDetailAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["connectionId"] = connectionId
            };

            var url = BuildUrl("/nacos/v2/ns/client", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ClientDetailInfo>>(cancellationToken: cancellationToken);
                return result?.Data;
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
            _logger?.LogError(ex, "Failed to get client detail");
            throw new NacosException(NacosException.ServerError, $"Failed to get client detail: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<ClientSubscribedService>> GetClientSubscribersAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["connectionId"] = connectionId
            };

            var url = BuildUrl("/nacos/v2/ns/client/subscribe/list", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<IEnumerable<ClientSubscribedService>>>(cancellationToken: cancellationToken);
                return result?.Data ?? Enumerable.Empty<ClientSubscribedService>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get client subscribers");
            throw new NacosException(NacosException.ServerError, $"Failed to get client subscribers: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<ClientPublishedService>> GetClientPublishedServicesAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["connectionId"] = connectionId
            };

            var url = BuildUrl("/nacos/v2/ns/client/publish/list", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<IEnumerable<ClientPublishedService>>>(cancellationToken: cancellationToken);
                return result?.Data ?? Enumerable.Empty<ClientPublishedService>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get client published services");
            throw new NacosException(NacosException.ServerError, $"Failed to get client published services: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<ConfigListenerInfo>> GetClientListenConfigsAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["connectionId"] = connectionId
            };

            var url = BuildUrl("/nacos/v2/cs/client/listenConfig/list", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<IEnumerable<ConfigListenerInfo>>>(cancellationToken: cancellationToken);
                return result?.Data ?? Enumerable.Empty<ConfigListenerInfo>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get client listen configs");
            throw new NacosException(NacosException.ServerError, $"Failed to get client listen configs: {ex.Message}", ex);
        }
    }

    public async Task<bool> ReloadConnectionCountAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{ConnectionApiPath}/reloadCurrent";
            var response = await _httpClient.PostAsync(url, null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<bool>>(cancellationToken: cancellationToken);
                return result?.Data ?? false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to reload connection count");
            throw new NacosException(NacosException.ServerError, $"Failed to reload connection count: {ex.Message}", ex);
        }
    }

    public async Task<IDictionary<string, int>> GetSdkVersionStatisticsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{ConnectionApiPath}/cluster";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<IDictionary<string, int>>>(cancellationToken: cancellationToken);
                return result?.Data ?? new Dictionary<string, int>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get SDK version statistics");
            throw new NacosException(NacosException.ServerError, $"Failed to get SDK version statistics: {ex.Message}", ex);
        }
    }

    public async Task<IDictionary<string, object>> GetCurrentNodeStatsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{ConnectionApiPath}/currentMetrics";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<IDictionary<string, object>>>(cancellationToken: cancellationToken);
                return result?.Data ?? new Dictionary<string, object>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get current node stats");
            throw new NacosException(NacosException.ServerError, $"Failed to get current node stats: {ex.Message}", ex);
        }
    }

    public async Task<bool> ResetConnectionLimitAsync(
        string namespaceId,
        int limitCount,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["namespaceId"] = namespaceId,
                ["limitCount"] = limitCount.ToString()
            };

            var url = BuildUrl($"{ConnectionApiPath}/limitRule", queryParams);
            var response = await _httpClient.PostAsync(url, null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<bool>>(cancellationToken: cancellationToken);
                return result?.Data ?? false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to reset connection limit");
            throw new NacosException(NacosException.ServerError, $"Failed to reset connection limit: {ex.Message}", ex);
        }
    }

    #endregion

    #region ICoreMaintainer - Namespace Management

    public async Task<IEnumerable<NamespaceInfo>> GetNamespacesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{NamespaceApiPath}/list";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<IEnumerable<NamespaceInfo>>>(cancellationToken: cancellationToken);
                return result?.Data ?? Enumerable.Empty<NamespaceInfo>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get namespaces");
            throw new NacosException(NacosException.ServerError, $"Failed to get namespaces: {ex.Message}", ex);
        }
    }

    public async Task<NamespaceInfo?> GetNamespaceAsync(string namespaceId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["namespaceId"] = namespaceId
            };

            var url = BuildUrl(NamespaceApiPath, queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<NamespaceInfo>>(cancellationToken: cancellationToken);
                return result?.Data;
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
            _logger?.LogError(ex, "Failed to get namespace");
            throw new NacosException(NacosException.ServerError, $"Failed to get namespace: {ex.Message}", ex);
        }
    }

    public async Task<bool> CreateNamespaceAsync(NamespaceRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["customNamespaceId"] = request.CustomNamespaceId ?? string.Empty,
                ["namespaceName"] = request.NamespaceName
            };

            if (!string.IsNullOrEmpty(request.NamespaceDesc))
                queryParams["namespaceDesc"] = request.NamespaceDesc;

            var url = BuildUrl(NamespaceApiPath, queryParams);
            var response = await _httpClient.PostAsync(url, null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<bool>>(cancellationToken: cancellationToken);
                return result?.Data ?? false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to create namespace");
            throw new NacosException(NacosException.ServerError, $"Failed to create namespace: {ex.Message}", ex);
        }
    }

    public async Task<bool> UpdateNamespaceAsync(NamespaceRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["namespaceId"] = request.CustomNamespaceId ?? string.Empty,
                ["namespaceName"] = request.NamespaceName
            };

            if (!string.IsNullOrEmpty(request.NamespaceDesc))
                queryParams["namespaceDesc"] = request.NamespaceDesc;

            var url = BuildUrl(NamespaceApiPath, queryParams);
            var response = await _httpClient.PutAsync(url, null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<bool>>(cancellationToken: cancellationToken);
                return result?.Data ?? false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to update namespace");
            throw new NacosException(NacosException.ServerError, $"Failed to update namespace: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteNamespaceAsync(string namespaceId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["namespaceId"] = namespaceId
            };

            var url = BuildUrl(NamespaceApiPath, queryParams);
            var response = await _httpClient.DeleteAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<bool>>(cancellationToken: cancellationToken);
                return result?.Data ?? false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to delete namespace");
            throw new NacosException(NacosException.ServerError, $"Failed to delete namespace: {ex.Message}", ex);
        }
    }

    #endregion

    #region ICoreMaintainer - Cluster Management

    public async Task<IEnumerable<ClusterMemberInfo>> GetClusterMembersAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{ClusterApiPath}/nodes";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<IEnumerable<ClusterMemberInfo>>>(cancellationToken: cancellationToken);
                return result?.Data ?? Enumerable.Empty<ClusterMemberInfo>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get cluster members");
            throw new NacosException(NacosException.ServerError, $"Failed to get cluster members: {ex.Message}", ex);
        }
    }

    public async Task<string> GetSelfNodeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{ClusterApiPath}/node/self";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ClusterMemberInfo>>(cancellationToken: cancellationToken);
                return result?.Data?.Address ?? "";
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get self node");
            throw new NacosException(NacosException.ServerError, $"Failed to get self node: {ex.Message}", ex);
        }
    }

    public async Task<ClusterMemberInfo?> GetClusterLeaderAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{ClusterApiPath}/node/leader";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ClusterMemberInfo>>(cancellationToken: cancellationToken);
                return result?.Data;
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
            _logger?.LogError(ex, "Failed to get cluster leader");
            throw new NacosException(NacosException.ServerError, $"Failed to get cluster leader: {ex.Message}", ex);
        }
    }

    public async Task<bool> UpdateClusterMemberLookupAsync(string address, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["type"] = "file",
                ["address"] = address
            };

            var url = BuildUrl($"{ClusterApiPath}/lookup", queryParams);
            var response = await _httpClient.PutAsync(url, null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<bool>>(cancellationToken: cancellationToken);
                return result?.Data ?? false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to update cluster member lookup");
            throw new NacosException(NacosException.ServerError, $"Failed to update cluster member lookup: {ex.Message}", ex);
        }
    }

    public async Task<bool> LeaveClusterAsync(IEnumerable<string> addresses, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["addresses"] = string.Join(",", addresses)
            };

            var url = BuildUrl($"{ClusterApiPath}/leave", queryParams);
            var response = await _httpClient.DeleteAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<bool>>(cancellationToken: cancellationToken);
                return result?.Data ?? false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to leave cluster");
            throw new NacosException(NacosException.ServerError, $"Failed to leave cluster: {ex.Message}", ex);
        }
    }

    #endregion

    #region ICoreMaintainer - Server State Management

    public async Task<ServerStateInfo> GetServerStateAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = "/nacos/v2/console/server/state";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ServerStateInfo>>(cancellationToken: cancellationToken);
                return result?.Data ?? new ServerStateInfo();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get server state");
            throw new NacosException(NacosException.ServerError, $"Failed to get server state: {ex.Message}", ex);
        }
    }

    public async Task<ServerSwitchInfo> GetServerSwitchesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{ServerStateApiPath}/switches";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ServerSwitchInfo>>(cancellationToken: cancellationToken);
                return result?.Data ?? new ServerSwitchInfo();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get server switches");
            throw new NacosException(NacosException.ServerError, $"Failed to get server switches: {ex.Message}", ex);
        }
    }

    public async Task<bool> UpdateServerSwitchAsync(
        string entry,
        string value,
        bool debug = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["entry"] = entry,
                ["value"] = value,
                ["debug"] = debug.ToString().ToLower()
            };

            var url = BuildUrl($"{ServerStateApiPath}/switches", queryParams);
            var response = await _httpClient.PutAsync(url, null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<bool>>(cancellationToken: cancellationToken);
                return result?.Data ?? false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to update server switch");
            throw new NacosException(NacosException.ServerError, $"Failed to update server switch: {ex.Message}", ex);
        }
    }

    #endregion

    #region ICoreMaintainer - Health Management

    public async Task<bool> GetReadinessAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = "/nacos/v2/health/readiness";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get readiness");
            return false;
        }
    }

    public async Task<bool> GetLivenessAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = "/nacos/v2/health/liveness";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get liveness");
            return false;
        }
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = "/nacos/health";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to check health");
            return false;
        }
    }

    #endregion

    #region ICoreMaintainer - Metrics

    async Task<MetricsInfo> ICoreMaintainer.GetMetricsAsync(CancellationToken cancellationToken)
    {
        return await GetMetricsAsync(false, cancellationToken);
    }

    public async Task<string> GetPrometheusMetricsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = "/nacos/actuator/prometheus";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get prometheus metrics");
            throw new NacosException(NacosException.ServerError, $"Failed to get prometheus metrics: {ex.Message}", ex);
        }
    }

    #endregion

    #region ICoreMaintainer - Raft Operations

    public async Task<string> GetRaftLeaderAsync(string groupId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["groupId"] = groupId
            };

            var url = BuildUrl($"{RaftApiPath}/leader", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<string>>(cancellationToken: cancellationToken);
                return result?.Data ?? "";
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get raft leader");
            throw new NacosException(NacosException.ServerError, $"Failed to get raft leader: {ex.Message}", ex);
        }
    }

    public async Task<bool> TransferRaftLeaderAsync(
        string groupId,
        string targetAddress,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["groupId"] = groupId,
                ["targetPeer"] = targetAddress
            };

            var url = BuildUrl($"{RaftApiPath}/transfer", queryParams);
            var response = await _httpClient.PostAsync(url, null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<bool>>(cancellationToken: cancellationToken);
                return result?.Data ?? false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to transfer raft leader");
            throw new NacosException(NacosException.ServerError, $"Failed to transfer raft leader: {ex.Message}", ex);
        }
    }

    public async Task<bool> ResetRaftClusterAsync(string groupId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["groupId"] = groupId
            };

            var url = BuildUrl($"{RaftApiPath}/reset", queryParams);
            var response = await _httpClient.PostAsync(url, null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<bool>>(cancellationToken: cancellationToken);
                return result?.Data ?? false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to reset raft cluster");
            throw new NacosException(NacosException.ServerError, $"Failed to reset raft cluster: {ex.Message}", ex);
        }
    }

    #endregion
}
