using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Client.Http;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Config.Filter;
using RedNb.Nacos.Core.Config.FuzzyWatch;
using RedNb.Nacos.Utils;

namespace RedNb.Nacos.Client.Config;

/// <summary>
/// Nacos config service implementation using HTTP.
/// </summary>
public class NacosConfigService : IConfigService
{
    private readonly NacosClientOptions _options;
    private readonly NacosHttpClient _httpClient;
    private readonly ILogger<NacosConfigService>? _logger;
    private readonly ConfigListenerManager _listenerManager;
    private readonly LocalConfigCache _localCache;
    private readonly ConfigFilterChainManager _filterChainManager;
    private readonly FuzzyWatchManager _fuzzyWatchManager;
    private readonly CancellationTokenSource _cts;
    private bool _disposed;
    private bool _isHealthy = true;

    private const string ConfigApiPath = "v1/cs/configs";
    private const string ListenerApiPath = "v1/cs/configs/listener";

    public NacosConfigService(NacosClientOptions options, ILogger<NacosConfigService>? logger = null)
    {
        _options = options;
        _logger = logger;
        _httpClient = new NacosHttpClient(options, logger);
        _listenerManager = new ConfigListenerManager();
        _localCache = new LocalConfigCache(options);
        _filterChainManager = new ConfigFilterChainManager();
        _fuzzyWatchManager = new FuzzyWatchManager(logger);
        _cts = new CancellationTokenSource();

        // Start long polling for config changes
        _ = StartLongPollingAsync(_cts.Token);
    }

    public async Task<string?> GetConfigAsync(string dataId, string group, long timeoutMs, 
        CancellationToken cancellationToken = default)
    {
        group = GetGroupOrDefault(group);
        ValidateParams(dataId, group);

        try
        {
            var parameters = new Dictionary<string, string?>
            {
                { "dataId", dataId },
                { "group", group },
                { "tenant", GetTenant() }
            };

            var content = await _httpClient.GetAsync(ConfigApiPath, parameters, timeoutMs, cancellationToken);
            
            if (content != null)
            {
                _localCache.SaveSnapshot(dataId, group, content);
                _isHealthy = true;
                
                // Apply filter chain for decryption
                content = await ApplyGetFilterAsync(dataId, group, content, cancellationToken);
            }

            return content;
        }
        catch (NacosException ex) when (ex.ErrorCode == NacosException.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get config from server, trying local cache");
            _isHealthy = false;
            
            // Try to get from local cache
            var cached = _localCache.GetSnapshot(dataId, group);
            if (cached != null)
            {
                _logger?.LogInformation("Using cached config for {DataId}@{Group}", dataId, group);
                // Apply filter chain for decryption
                cached = await ApplyGetFilterAsync(dataId, group, cached, cancellationToken);
                return cached;
            }

            throw;
        }
    }

    public async Task<string?> GetConfigAndSignListenerAsync(string dataId, string group, long timeoutMs, 
        IConfigChangeListener listener, CancellationToken cancellationToken = default)
    {
        var content = await GetConfigAsync(dataId, group, timeoutMs, cancellationToken);
        await AddListenerAsync(dataId, group, listener, cancellationToken);
        return content;
    }

    public async Task AddListenerAsync(string dataId, string group, IConfigChangeListener listener, 
        CancellationToken cancellationToken = default)
    {
        group = GetGroupOrDefault(group);
        ValidateParams(dataId, group);
        
        _listenerManager.AddListener(dataId, group, GetTenant(), listener);
        _logger?.LogDebug("Added listener for {DataId}@{Group}", dataId, group);
        
        // Initialize MD5 for the listener to enable proper change detection
        try
        {
            var content = await GetConfigAsync(dataId, group, _options.DefaultTimeout, cancellationToken);
            var md5 = content != null ? NacosUtils.GetMd5(content) : null;
            _listenerManager.UpdateMd5(dataId, group, GetTenant(), md5);
            _logger?.LogDebug("Initialized MD5 for {DataId}@{Group}: {Md5}", dataId, group, md5);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to initialize MD5 for {DataId}@{Group}, listener may not detect first change", dataId, group);
        }
    }

    public void RemoveListener(string dataId, string group, IConfigChangeListener listener)
    {
        group = GetGroupOrDefault(group);
        _listenerManager.RemoveListener(dataId, group, GetTenant(), listener);
        _logger?.LogDebug("Removed listener for {DataId}@{Group}", dataId, group);
    }

    public async Task<bool> PublishConfigAsync(string dataId, string group, string content, 
        CancellationToken cancellationToken = default)
    {
        return await PublishConfigAsync(dataId, group, content, ConfigType.Default, cancellationToken);
    }

    public async Task<bool> PublishConfigAsync(string dataId, string group, string content, string type, 
        CancellationToken cancellationToken = default)
    {
        group = GetGroupOrDefault(group);
        ValidateParams(dataId, group, content);

        // Apply filter chain for encryption
        var (processedContent, encryptedDataKey) = await ApplyPublishFilterAsync(dataId, group, content, cancellationToken);

        var parameters = new Dictionary<string, string?>
        {
            { "dataId", dataId },
            { "group", group },
            { "tenant", GetTenant() },
            { "content", processedContent },
            { "type", type }
        };

        if (!string.IsNullOrEmpty(encryptedDataKey))
        {
            parameters["encryptedDataKey"] = encryptedDataKey;
        }

        var body = NacosUtils.BuildQueryString(parameters);
        var response = await _httpClient.PostAsync(ConfigApiPath, null, body, 
            _options.DefaultTimeout, cancellationToken);

        var result = response?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        
        if (result)
        {
            _logger?.LogInformation("Published config {DataId}@{Group}", dataId, group);
        }

        return result;
    }

    public async Task<bool> PublishConfigCasAsync(string dataId, string group, string content, string casMd5, 
        CancellationToken cancellationToken = default)
    {
        return await PublishConfigCasAsync(dataId, group, content, casMd5, ConfigType.Default, cancellationToken);
    }

    public async Task<bool> PublishConfigCasAsync(string dataId, string group, string content, string casMd5, 
        string type, CancellationToken cancellationToken = default)
    {
        group = GetGroupOrDefault(group);
        ValidateParams(dataId, group, content);

        var parameters = new Dictionary<string, string?>
        {
            { "dataId", dataId },
            { "group", group },
            { "tenant", GetTenant() },
            { "content", content },
            { "type", type },
            { "casMd5", casMd5 }
        };

        var body = NacosUtils.BuildQueryString(parameters);
        var response = await _httpClient.PostAsync(ConfigApiPath, null, body, 
            _options.DefaultTimeout, cancellationToken);

        return response?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task<bool> RemoveConfigAsync(string dataId, string group, 
        CancellationToken cancellationToken = default)
    {
        group = GetGroupOrDefault(group);
        ValidateParams(dataId, group);

        var parameters = new Dictionary<string, string?>
        {
            { "dataId", dataId },
            { "group", group },
            { "tenant", GetTenant() }
        };

        var response = await _httpClient.DeleteAsync(ConfigApiPath, parameters, 
            _options.DefaultTimeout, cancellationToken);

        var result = response?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        
        if (result)
        {
            _localCache.RemoveSnapshot(dataId, group);
            _logger?.LogInformation("Removed config {DataId}@{Group}", dataId, group);
        }

        return result;
    }

    public string GetServerStatus()
    {
        return _isHealthy ? "UP" : "DOWN";
    }

    public void AddConfigFilter(IConfigFilter configFilter)
    {
        _filterChainManager.AddFilter(configFilter);
        _logger?.LogDebug("Added config filter: {FilterName}", configFilter.FilterName);
    }

    #region Fuzzy Watch

    public Task FuzzyWatchAsync(string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        return FuzzyWatchAsync("*", groupNamePattern, watcher, cancellationToken);
    }

    public Task FuzzyWatchAsync(string dataIdPattern, string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        _fuzzyWatchManager.AddWatcher(dataIdPattern, groupNamePattern, GetTenant() ?? "", watcher);
        _logger?.LogDebug("Added fuzzy watch for dataId={DataIdPattern}, group={GroupPattern}", 
            dataIdPattern, groupNamePattern);
        return Task.CompletedTask;
    }

    public Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string groupNamePattern, 
        IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        return FuzzyWatchWithGroupKeysAsync("*", groupNamePattern, watcher, cancellationToken);
    }

    public async Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string dataIdPattern, string groupNamePattern, 
        IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        await FuzzyWatchAsync(dataIdPattern, groupNamePattern, watcher, cancellationToken);
        
        // Return current matching keys
        var matchingKeys = _fuzzyWatchManager.GetMatchingKeys(dataIdPattern, groupNamePattern, GetTenant() ?? "");
        return matchingKeys;
    }

    public Task CancelFuzzyWatchAsync(string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        return CancelFuzzyWatchAsync("*", groupNamePattern, watcher, cancellationToken);
    }

    public Task CancelFuzzyWatchAsync(string dataIdPattern, string groupNamePattern, 
        IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        _fuzzyWatchManager.RemoveWatcher(dataIdPattern, groupNamePattern, GetTenant() ?? "", watcher);
        _logger?.LogDebug("Cancelled fuzzy watch for dataId={DataIdPattern}, group={GroupPattern}", 
            dataIdPattern, groupNamePattern);
        return Task.CompletedTask;
    }

    #endregion

    public async Task ShutdownAsync()
    {
        await DisposeAsync();
    }

    private async Task StartLongPollingAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting config long polling");

        // Wait a bit for initial setup
        await Task.Delay(100, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var listeningConfigs = _listenerManager.GetListeningConfigs();
                if (listeningConfigs.Count == 0)
                {
                    _logger?.LogDebug("No configs to listen, waiting...");
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                _logger?.LogDebug("Long polling for {Count} configs: {Configs}", 
                    listeningConfigs.Count,
                    string.Join(", ", listeningConfigs.Select(c => $"{c.DataId}@{c.Group}(md5={c.Md5})")));

                var changedConfigs = await CheckConfigChangesAsync(listeningConfigs, cancellationToken);
                
                if (changedConfigs.Count > 0)
                {
                    _logger?.LogInformation("Detected {Count} config changes", changedConfigs.Count);
                    foreach (var config in changedConfigs)
                    {
                        await NotifyListenersAsync(config.DataId, config.Group, config.Tenant, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in config long polling");
                await Task.Delay(5000, cancellationToken);
            }
        }

        _logger?.LogInformation("Config long polling stopped");
    }

    private async Task<List<ListeningConfig>> CheckConfigChangesAsync(
        List<ListeningConfig> listeningConfigs, CancellationToken cancellationToken)
    {
        var changedConfigs = new List<ListeningConfig>();

        // Build listening configs string
        // Format (without tenant): dataId\x02group\x02md5\x01
        // Format (with tenant): dataId\x02group\x02md5\x02tenant\x01
        var listeningConfigsStr = string.Join("", listeningConfigs.Select(c =>
        {
            if (string.IsNullOrEmpty(c.Tenant))
            {
                // Without tenant
                return $"{c.DataId}\x02{c.Group}\x02{c.Md5 ?? ""}\x01";
            }
            else
            {
                // With tenant
                return $"{c.DataId}\x02{c.Group}\x02{c.Md5 ?? ""}\x02{c.Tenant}\x01";
            }
        }));

        // Long polling requires the Listening-Configs parameter in the request body
        var body = $"Listening-Configs={Uri.EscapeDataString(listeningConfigsStr)}";

        // Set the Long-Pulling-Timeout header (required by Nacos server for long polling)
        var headers = new Dictionary<string, string>
        {
            { "Long-Pulling-Timeout", _options.LongPollTimeout.ToString() }
        };

        try
        {
            _logger?.LogDebug("Checking config changes, body: {Body}", body);
            
            var response = await _httpClient.PostWithHeadersAsync(
                ListenerApiPath, 
                null, 
                body, 
                headers,
                _options.LongPollTimeout + 5000, // Add buffer to HTTP timeout
                cancellationToken);

            _logger?.LogDebug("Long polling response: '{Response}'", response ?? "(empty)");

            if (!string.IsNullOrEmpty(response))
            {
                _logger?.LogInformation("Config change detected in response");
                
                // Parse changed config keys
                // Response format (without tenant): dataId\x02group\x01
                // Response format (with tenant): dataId\x02group\x02tenant\x01
                var parts = response.Split('\x01', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var fields = part.Split('\x02');
                    if (fields.Length >= 2)
                    {
                        changedConfigs.Add(new ListeningConfig
                        {
                            DataId = fields[0],
                            Group = fields[1],
                            Tenant = fields.Length > 2 && !string.IsNullOrEmpty(fields[2]) ? fields[2] : null
                        });
                        _logger?.LogInformation("Config changed: {DataId}@{Group}", fields[0], fields[1]);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested - propagate
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to check config changes");
        }

        return changedConfigs;
    }

    private async Task NotifyListenersAsync(string dataId, string group, string? tenant, 
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await GetConfigAsync(dataId, group, _options.DefaultTimeout, cancellationToken);
            var md5 = content != null ? NacosUtils.GetMd5(content) : null;

            var configInfo = new ConfigInfo
            {
                DataId = dataId,
                Group = group,
                Tenant = tenant,
                Content = content,
                Md5 = md5
            };

            var listeners = _listenerManager.GetListeners(dataId, group, tenant);
            foreach (var listener in listeners)
            {
                try
                {
                    listener.OnReceiveConfigInfo(configInfo);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error notifying listener for {DataId}@{Group}", dataId, group);
                }
            }

            // Update cached MD5
            _listenerManager.UpdateMd5(dataId, group, tenant, md5);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to notify listeners for {DataId}@{Group}", dataId, group);
        }
    }

    private string GetGroupOrDefault(string? group)
    {
        return string.IsNullOrWhiteSpace(group) ? NacosConstants.DefaultGroup : group.Trim();
    }

    private string? GetTenant()
    {
        return string.IsNullOrWhiteSpace(_options.Namespace) ? null : _options.Namespace;
    }

    private static void ValidateParams(string dataId, string group, string? content = null)
    {
        if (string.IsNullOrWhiteSpace(dataId))
        {
            throw new NacosException(NacosException.InvalidParam, "dataId is required");
        }

        if (string.IsNullOrWhiteSpace(group))
        {
            throw new NacosException(NacosException.InvalidParam, "group is required");
        }

        if (content != null && string.IsNullOrEmpty(content))
        {
            throw new NacosException(NacosException.InvalidParam, "content cannot be empty");
        }
    }

    #region Filter Chain Methods

    private async Task<string?> ApplyGetFilterAsync(string dataId, string group, string? content, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(content) || !_filterChainManager.HasFilters)
        {
            return content;
        }

        try
        {
            var request = new ConfigRequest();
            request.SetParameter(ConfigRequestKeys.DataId, dataId);
            request.SetParameter(ConfigRequestKeys.Group, group);
            request.SetParameter(ConfigRequestKeys.Tenant, GetTenant());
            request.SetParameter(ConfigRequestKeys.Content, content);

            var response = new ConfigResponse();

            await _filterChainManager.DoFilterAsync(request, response, cancellationToken);

            // Get decrypted content from response
            var processedContent = response.GetParameter<string>(ConfigResponseKeys.Content);
            return processedContent ?? content;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error applying get filter for {DataId}@{Group}", dataId, group);
            return content;
        }
    }

    private async Task<(string Content, string? EncryptedDataKey)> ApplyPublishFilterAsync(
        string dataId, string group, string content, CancellationToken cancellationToken)
    {
        if (!_filterChainManager.HasFilters)
        {
            return (content, null);
        }

        try
        {
            var request = new ConfigRequest();
            request.SetParameter(ConfigRequestKeys.DataId, dataId);
            request.SetParameter(ConfigRequestKeys.Group, group);
            request.SetParameter(ConfigRequestKeys.Tenant, GetTenant());
            request.SetParameter(ConfigRequestKeys.Content, content);

            var response = new ConfigResponse();

            await _filterChainManager.DoFilterAsync(request, response, cancellationToken);

            // Get encrypted content and data key from response
            var processedContent = response.GetParameter<string>(ConfigResponseKeys.Content) ?? content;
            var encryptedDataKey = response.GetParameter<string>(ConfigResponseKeys.EncryptedDataKey);

            return (processedContent, encryptedDataKey);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error applying publish filter for {DataId}@{Group}", dataId, group);
            return (content, null);
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        await _cts.CancelAsync();
        _cts.Dispose();
        _httpClient.Dispose();
        _disposed = true;
    }
}
