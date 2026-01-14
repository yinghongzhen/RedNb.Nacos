using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Maintainer;

namespace RedNb.Nacos.Http.Maintainer;

/// <summary>
/// HTTP implementation of Config Maintainer interfaces.
/// </summary>
public partial class NacosMaintainerService : IConfigMaintainer, IConfigHistoryMaintainer, IBetaConfigMaintainer, IConfigOpsMaintainer
{
    private const string ConfigApiPath = "/nacos/v2/cs/config";
    private const string ConfigHistoryApiPath = "/nacos/v2/cs/history";

    #region IConfigMaintainer

    public async Task<ConfigDetailInfo?> GetConfigAsync(
        string dataId,
        CancellationToken cancellationToken = default)
    {
        return await GetConfigAsync(dataId, "DEFAULT_GROUP", _options.Namespace ?? "", cancellationToken);
    }

    public async Task<ConfigDetailInfo?> GetConfigAsync(
        string dataId,
        string group,
        CancellationToken cancellationToken = default)
    {
        return await GetConfigAsync(dataId, group, _options.Namespace ?? "", cancellationToken);
    }

    public async Task<ConfigDetailInfo?> GetConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId
            };

            var url = BuildUrl(ConfigApiPath, queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ConfigDetailInfo>>(cancellationToken: cancellationToken);
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
            _logger?.LogError(ex, "Failed to get config {DataId}:{Group}", dataId, group);
            throw new NacosException(NacosException.ServerError, $"Failed to get config: {ex.Message}", ex);
        }
    }

    public async Task<bool> PublishConfigAsync(
        string dataId,
        string content,
        CancellationToken cancellationToken = default)
    {
        return await PublishConfigAsync(dataId, "DEFAULT_GROUP", _options.Namespace ?? "", content, cancellationToken: cancellationToken);
    }

    public async Task<bool> PublishConfigAsync(
        string dataId,
        string group,
        string content,
        CancellationToken cancellationToken = default)
    {
        return await PublishConfigAsync(dataId, group, _options.Namespace ?? "", content, cancellationToken: cancellationToken);
    }

    public async Task<bool> PublishConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        string content,
        string? description = null,
        string? type = null,
        string? appName = null,
        string? srcUser = null,
        string? configTags = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId,
                ["content"] = content
            };

            if (!string.IsNullOrEmpty(type))
                queryParams["type"] = type;
            if (!string.IsNullOrEmpty(appName))
                queryParams["appName"] = appName;
            if (!string.IsNullOrEmpty(srcUser))
                queryParams["srcUser"] = srcUser;
            if (!string.IsNullOrEmpty(description))
                queryParams["desc"] = description;
            if (!string.IsNullOrEmpty(configTags))
                queryParams["config_tags"] = configTags;

            var url = BuildUrl(ConfigApiPath, queryParams);
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
            _logger?.LogError(ex, "Failed to publish config {DataId}:{Group}", dataId, group);
            throw new NacosException(NacosException.ServerError, $"Failed to publish config: {ex.Message}", ex);
        }
    }

    public async Task<bool> UpdateConfigMetadataAsync(
        string dataId,
        string group,
        string namespaceId,
        string? description,
        string? configTags,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId
            };

            if (!string.IsNullOrEmpty(description))
                queryParams["desc"] = description;
            if (!string.IsNullOrEmpty(configTags))
                queryParams["config_tags"] = configTags;

            var url = BuildUrl($"{ConfigApiPath}/metadata", queryParams);
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
            _logger?.LogError(ex, "Failed to update config metadata {DataId}:{Group}", dataId, group);
            throw new NacosException(NacosException.ServerError, $"Failed to update config metadata: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteConfigAsync(
        string dataId,
        CancellationToken cancellationToken = default)
    {
        return await DeleteConfigAsync(dataId, "DEFAULT_GROUP", _options.Namespace ?? "", cancellationToken);
    }

    public async Task<bool> DeleteConfigAsync(
        string dataId,
        string group,
        CancellationToken cancellationToken = default)
    {
        return await DeleteConfigAsync(dataId, group, _options.Namespace ?? "", cancellationToken);
    }

    public async Task<bool> DeleteConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId
            };

            var url = BuildUrl(ConfigApiPath, queryParams);
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
            _logger?.LogError(ex, "Failed to delete config {DataId}:{Group}", dataId, group);
            throw new NacosException(NacosException.ServerError, $"Failed to delete config: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteConfigsAsync(
        List<long> ids,
        CancellationToken cancellationToken = default)
    {
        return await DeleteConfigsAsync(_options.Namespace ?? "", ids, cancellationToken);
    }

    public async Task<bool> DeleteConfigsAsync(
        string namespaceId,
        IEnumerable<long> ids,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["namespaceId"] = namespaceId,
                ["ids"] = string.Join(",", ids)
            };

            var url = BuildUrl($"{ConfigApiPath}/batch", queryParams);
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
            _logger?.LogError(ex, "Failed to delete configs batch");
            throw new NacosException(NacosException.ServerError, $"Failed to delete configs: {ex.Message}", ex);
        }
    }

    public async Task<Page<ConfigBasicInfo>> ListConfigsAsync(
        string namespaceId,
        CancellationToken cancellationToken = default)
    {
        return await ListConfigsAsync(null, null, namespaceId, null, null, null, 1, 100, cancellationToken);
    }

    public async Task<Page<ConfigBasicInfo>> ListConfigsAsync(
        string? dataId,
        string? group,
        string namespaceId,
        string? type = null,
        string? configTags = null,
        string? appName = null,
        int pageNo = 1,
        int pageSize = 100,
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
                ["search"] = "accurate"
            };

            if (!string.IsNullOrEmpty(dataId))
                queryParams["dataId"] = dataId;
            if (!string.IsNullOrEmpty(group))
                queryParams["group"] = group;
            if (!string.IsNullOrEmpty(type))
                queryParams["type"] = type;
            if (!string.IsNullOrEmpty(appName))
                queryParams["appName"] = appName;
            if (!string.IsNullOrEmpty(configTags))
                queryParams["config_tags"] = configTags;

            var url = BuildUrl($"{ConfigApiPath}/searchDetail", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<Page<ConfigBasicInfo>>>(cancellationToken: cancellationToken);
                return result?.Data ?? Page<ConfigBasicInfo>.Empty();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to list configs");
            throw new NacosException(NacosException.ServerError, $"Failed to list configs: {ex.Message}", ex);
        }
    }

    public async Task<Page<ConfigBasicInfo>> SearchConfigsAsync(
        string? dataIdPattern,
        string? groupPattern,
        string namespaceId,
        CancellationToken cancellationToken = default)
    {
        return await SearchConfigsAsync(dataIdPattern, groupPattern, namespaceId, null, null, null, null, 1, 100, cancellationToken);
    }

    public async Task<Page<ConfigBasicInfo>> SearchConfigsAsync(
        string? dataIdPattern,
        string? groupPattern,
        string namespaceId,
        string? configDetail,
        string? type,
        string? configTags,
        string? appName,
        int pageNo = 1,
        int pageSize = 100,
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
                ["search"] = "blur"
            };

            if (!string.IsNullOrEmpty(dataIdPattern))
                queryParams["dataId"] = dataIdPattern;
            if (!string.IsNullOrEmpty(groupPattern))
                queryParams["group"] = groupPattern;
            if (!string.IsNullOrEmpty(configDetail))
                queryParams["config_detail"] = configDetail;
            if (!string.IsNullOrEmpty(type))
                queryParams["type"] = type;
            if (!string.IsNullOrEmpty(appName))
                queryParams["appName"] = appName;
            if (!string.IsNullOrEmpty(configTags))
                queryParams["config_tags"] = configTags;

            var url = BuildUrl($"{ConfigApiPath}/searchDetail", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<Page<ConfigBasicInfo>>>(cancellationToken: cancellationToken);
                return result?.Data ?? Page<ConfigBasicInfo>.Empty();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to search configs");
            throw new NacosException(NacosException.ServerError, $"Failed to search configs: {ex.Message}", ex);
        }
    }

    public async Task<List<ConfigBasicInfo>> GetConfigListByNamespaceAsync(
        string namespaceId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["namespaceId"] = namespaceId
            };

            var url = BuildUrl($"{ConfigApiPath}/list", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<List<ConfigBasicInfo>>>(cancellationToken: cancellationToken);
                return result?.Data ?? new List<ConfigBasicInfo>();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get config list by namespace");
            throw new NacosException(NacosException.ServerError, $"Failed to get config list: {ex.Message}", ex);
        }
    }

    public async Task<ConfigListenerInfo> GetListenersAsync(
        string dataId,
        string group,
        CancellationToken cancellationToken = default)
    {
        return await GetListenersAsync(dataId, group, _options.Namespace ?? "", true, cancellationToken);
    }

    public async Task<ConfigListenerInfo> GetListenersAsync(
        string dataId,
        string group,
        string namespaceId,
        bool aggregation = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId,
                ["aggregation"] = aggregation.ToString().ToLower()
            };

            var url = BuildUrl($"{ConfigApiPath}/listener", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ConfigListenerInfo>>(cancellationToken: cancellationToken);
                return result?.Data ?? new ConfigListenerInfo();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get listeners");
            throw new NacosException(NacosException.ServerError, $"Failed to get listeners: {ex.Message}", ex);
        }
    }

    public async Task<ConfigListenerInfo> GetAllSubClientConfigByIpAsync(
        string ip,
        bool all = false,
        string? namespaceId = null,
        bool aggregation = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["ip"] = ip,
                ["all"] = all.ToString().ToLower(),
                ["aggregation"] = aggregation.ToString().ToLower()
            };

            if (!string.IsNullOrEmpty(namespaceId))
                queryParams["namespaceId"] = namespaceId;

            var url = BuildUrl($"{ConfigApiPath}/listener/ip", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ConfigListenerInfo>>(cancellationToken: cancellationToken);
                return result?.Data ?? new ConfigListenerInfo();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to get config listeners by ip");
            throw new NacosException(NacosException.ServerError, $"Failed to get listeners by ip: {ex.Message}", ex);
        }
    }

    public async Task<CloneResult> CloneConfigAsync(
        string namespaceId,
        List<ConfigCloneInfo> cloneInfos,
        SameConfigPolicy policy,
        string? srcUser = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["namespaceId"] = namespaceId,
                ["policy"] = policy.ToString()
            };

            if (!string.IsNullOrEmpty(srcUser))
                queryParams["srcUser"] = srcUser;

            var url = BuildUrl($"{ConfigApiPath}/clone", queryParams);
            var content = JsonContent.Create(cloneInfos);
            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<CloneResult>>(cancellationToken: cancellationToken);
                return result?.Data ?? new CloneResult();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to clone config");
            throw new NacosException(NacosException.ServerError, $"Failed to clone config: {ex.Message}", ex);
        }
    }

    #endregion

    #region IConfigHistoryMaintainer

    public async Task<Page<ConfigHistoryBasicInfo>> ListConfigHistoryAsync(
        string dataId,
        string group,
        string namespaceId,
        int pageNo = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId,
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString()
            };

            var url = BuildUrl($"{ConfigHistoryApiPath}/list", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<Page<ConfigHistoryBasicInfo>>>(cancellationToken: cancellationToken);
                return result?.Data ?? Page<ConfigHistoryBasicInfo>.Empty();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to list config history");
            throw new NacosException(NacosException.ServerError, $"Failed to list config history: {ex.Message}", ex);
        }
    }

    public async Task<ConfigHistoryDetailInfo?> GetConfigHistoryInfoAsync(
        string dataId,
        string group,
        string namespaceId,
        long nid,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId,
                ["nid"] = nid.ToString()
            };

            var url = BuildUrl(ConfigHistoryApiPath, queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ConfigHistoryDetailInfo>>(cancellationToken: cancellationToken);
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
            _logger?.LogError(ex, "Failed to get config history info");
            throw new NacosException(NacosException.ServerError, $"Failed to get config history: {ex.Message}", ex);
        }
    }

    public async Task<ConfigHistoryDetailInfo?> GetPreviousConfigHistoryInfoAsync(
        string dataId,
        string group,
        string namespaceId,
        long id,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId,
                ["id"] = id.ToString()
            };

            var url = BuildUrl($"{ConfigHistoryApiPath}/previous", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ConfigHistoryDetailInfo>>(cancellationToken: cancellationToken);
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
            _logger?.LogError(ex, "Failed to get previous config history");
            throw new NacosException(NacosException.ServerError, $"Failed to get previous config history: {ex.Message}", ex);
        }
    }

    #endregion

    #region IBetaConfigMaintainer

    public async Task<BetaConfigInfo?> GetBetaConfigAsync(
        string dataId,
        string group,
        CancellationToken cancellationToken = default)
    {
        return await GetBetaConfigAsync(dataId, group, _options.Namespace ?? "", cancellationToken);
    }

    public async Task<BetaConfigInfo?> GetBetaConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId
            };

            var url = BuildUrl($"{ConfigApiPath}/beta", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<BetaConfigInfo>>(cancellationToken: cancellationToken);
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
            _logger?.LogError(ex, "Failed to get beta config");
            throw new NacosException(NacosException.ServerError, $"Failed to get beta config: {ex.Message}", ex);
        }
    }

    public async Task<bool> PublishBetaConfigAsync(
        string dataId,
        string group,
        string content,
        string betaIps,
        CancellationToken cancellationToken = default)
    {
        return await PublishBetaConfigAsync(dataId, group, _options.Namespace ?? "", content, betaIps, cancellationToken: cancellationToken);
    }

    public async Task<bool> PublishBetaConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        string content,
        string betaIps,
        string? description = null,
        string? type = null,
        string? appName = null,
        string? srcUser = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId,
                ["content"] = content,
                ["betaIps"] = betaIps
            };

            if (!string.IsNullOrEmpty(description))
                queryParams["desc"] = description;
            if (!string.IsNullOrEmpty(type))
                queryParams["type"] = type;
            if (!string.IsNullOrEmpty(appName))
                queryParams["appName"] = appName;
            if (!string.IsNullOrEmpty(srcUser))
                queryParams["srcUser"] = srcUser;

            var url = BuildUrl($"{ConfigApiPath}/beta", queryParams);
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
            _logger?.LogError(ex, "Failed to publish beta config");
            throw new NacosException(NacosException.ServerError, $"Failed to publish beta config: {ex.Message}", ex);
        }
    }

    public async Task<bool> StopBetaConfigAsync(
        string dataId,
        string group,
        CancellationToken cancellationToken = default)
    {
        return await StopBetaConfigAsync(dataId, group, _options.Namespace ?? "", cancellationToken);
    }

    public async Task<bool> StopBetaConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId
            };

            var url = BuildUrl($"{ConfigApiPath}/beta", queryParams);
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
            _logger?.LogError(ex, "Failed to stop beta config");
            throw new NacosException(NacosException.ServerError, $"Failed to stop beta config: {ex.Message}", ex);
        }
    }

    public async Task<GrayConfigInfo?> GetGrayConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        string grayName,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId,
                ["grayName"] = grayName
            };

            var url = BuildUrl($"{ConfigApiPath}/gray", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<GrayConfigInfo>>(cancellationToken: cancellationToken);
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
            _logger?.LogError(ex, "Failed to get gray config");
            throw new NacosException(NacosException.ServerError, $"Failed to get gray config: {ex.Message}", ex);
        }
    }

    public async Task<bool> PublishGrayConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        string content,
        GrayConfigRule grayRule,
        string? description = null,
        string? type = null,
        string? srcUser = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId,
                ["content"] = content,
                ["grayName"] = grayRule.GrayName ?? "gray",
                ["grayRule"] = JsonSerializer.Serialize(grayRule)
            };

            if (!string.IsNullOrEmpty(description))
                queryParams["desc"] = description;
            if (!string.IsNullOrEmpty(type))
                queryParams["type"] = type;
            if (!string.IsNullOrEmpty(srcUser))
                queryParams["srcUser"] = srcUser;

            var url = BuildUrl($"{ConfigApiPath}/gray", queryParams);
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
            _logger?.LogError(ex, "Failed to publish gray config");
            throw new NacosException(NacosException.ServerError, $"Failed to publish gray config: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteGrayConfigAsync(
        string dataId,
        string group,
        string namespaceId,
        string grayName,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["group"] = group,
                ["namespaceId"] = namespaceId,
                ["grayName"] = grayName
            };

            var url = BuildUrl($"{ConfigApiPath}/gray", queryParams);
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
            _logger?.LogError(ex, "Failed to delete gray config");
            throw new NacosException(NacosException.ServerError, $"Failed to delete gray config: {ex.Message}", ex);
        }
    }

    #endregion

    #region IConfigOpsMaintainer

    public async Task<ConfigImportResult> ImportConfigAsync(
        string namespaceId,
        SameConfigPolicy policy,
        byte[] fileContent,
        string fileName,
        string? srcUser = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var url = $"{ConfigApiPath}?import=true&namespace={HttpUtility.UrlEncode(namespaceId)}&policy={policy}";
            if (!string.IsNullOrEmpty(srcUser))
                url += $"&srcUser={HttpUtility.UrlEncode(srcUser)}";

            using var content = new MultipartFormDataContent();
            var fileContentBytes = new ByteArrayContent(fileContent);
            fileContentBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
            content.Add(fileContentBytes, "file", fileName);

            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<ConfigImportResult>>(cancellationToken: cancellationToken);
                return result?.Data ?? new ConfigImportResult();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to import config");
            throw new NacosException(NacosException.ServerError, $"Failed to import config: {ex.Message}", ex);
        }
    }

    public async Task<byte[]> ExportConfigAsync(
        ConfigExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["namespaceId"] = request.NamespaceId ?? "",
                ["export"] = "true"
            };

            if (!string.IsNullOrEmpty(request.DataId))
                queryParams["dataId"] = request.DataId;
            if (!string.IsNullOrEmpty(request.Group))
                queryParams["group"] = request.Group;
            if (!string.IsNullOrEmpty(request.AppName))
                queryParams["appName"] = request.AppName;
            if (request.ConfigIds != null && request.ConfigIds.Any())
                queryParams["ids"] = string.Join(",", request.ConfigIds);

            var url = BuildUrl(ConfigApiPath, queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to export config");
            throw new NacosException(NacosException.ServerError, $"Failed to export config: {ex.Message}", ex);
        }
    }

    public async Task<byte[]> ExportConfigByIdsAsync(
        string namespaceId,
        IEnumerable<long> ids,
        CancellationToken cancellationToken = default)
    {
        return await ExportConfigAsync(new ConfigExportRequest
        {
            NamespaceId = namespaceId,
            ConfigIds = ids.ToList()
        }, cancellationToken);
    }

    public async Task<byte[]> ExportAllConfigAsync(
        string namespaceId,
        string? dataId = null,
        string? group = null,
        string? appName = null,
        CancellationToken cancellationToken = default)
    {
        return await ExportConfigAsync(new ConfigExportRequest
        {
            NamespaceId = namespaceId,
            DataId = dataId,
            Group = group,
            AppName = appName
        }, cancellationToken);
    }

    public async Task<CloneResult> CloneConfigAsync(
        string sourceNamespaceId,
        string targetNamespaceId,
        IEnumerable<long> ids,
        SameConfigPolicy policy = SameConfigPolicy.Abort,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["srcNamespaceId"] = sourceNamespaceId,
                ["dstNamespaceId"] = targetNamespaceId,
                ["policy"] = policy.ToString(),
                ["ids"] = string.Join(",", ids)
            };

            var url = BuildUrl($"{ConfigApiPath}/clone", queryParams);
            var response = await _httpClient.PostAsync(url, null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<CloneResult>>(cancellationToken: cancellationToken);
                return result?.Data ?? new CloneResult();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to clone config");
            throw new NacosException(NacosException.ServerError, $"Failed to clone config: {ex.Message}", ex);
        }
    }

    public async Task<CloneResult> CloneAllConfigAsync(
        string sourceNamespaceId,
        string targetNamespaceId,
        SameConfigPolicy policy = SameConfigPolicy.Abort,
        string? dataId = null,
        string? group = null,
        string? appName = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["srcNamespaceId"] = sourceNamespaceId,
                ["dstNamespaceId"] = targetNamespaceId,
                ["policy"] = policy.ToString()
            };

            if (!string.IsNullOrEmpty(dataId))
                queryParams["dataId"] = dataId;
            if (!string.IsNullOrEmpty(group))
                queryParams["group"] = group;
            if (!string.IsNullOrEmpty(appName))
                queryParams["appName"] = appName;

            var url = BuildUrl($"{ConfigApiPath}/cloneAll", queryParams);
            var response = await _httpClient.PostAsync(url, null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NacosApiResponse<CloneResult>>(cancellationToken: cancellationToken);
                return result?.Data ?? new CloneResult();
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NacosException((int)response.StatusCode, error);
        }
        catch (Exception ex) when (ex is not NacosException)
        {
            _logger?.LogError(ex, "Failed to clone all config");
            throw new NacosException(NacosException.ServerError, $"Failed to clone all config: {ex.Message}", ex);
        }
    }

    #endregion

    #region Helper Classes

    private class NacosApiResponse<T>
    {
        public int Code { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    #endregion
}
