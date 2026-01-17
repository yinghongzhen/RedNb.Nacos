using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Config.Filter;
using RedNb.Nacos.Core.Config.FuzzyWatch;
using RedNb.Nacos.Utils;

namespace RedNb.Nacos.GrpcClient.Config;

/// <summary>
/// Nacos config service implementation using gRPC.
/// Provides configuration management with server push support.
/// </summary>
public class NacosGrpcConfigService : IConfigService
{
    private readonly NacosClientOptions _options;
    private readonly NacosGrpcClient _grpcClient;
    private readonly ConfigRpcTransportClient _transportClient;
    private readonly ILogger<NacosGrpcConfigService>? _logger;
    
    // Listener management
    private readonly ConcurrentDictionary<string, ConfigCacheData> _configCache = new();
    private readonly ConfigFilterChainManager _filterChainManager;
    
    // Fuzzy watch management
    private readonly ConcurrentDictionary<string, FuzzyWatchEntry> _fuzzyWatchers = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _knownConfigs = new();
    
    // Local cache for failover
    private readonly LocalConfigCache _localCache;
    
    // Background task management
    private readonly CancellationTokenSource _cts;
    private Task? _listenTask;
    private readonly SemaphoreSlim _listenLock = new(1, 1);
    
    private bool _disposed;
    private bool _isHealthy;
    private bool _initialized;

    /// <summary>
    /// Interval for checking config changes (milliseconds).
    /// </summary>
    private const int ListenCheckIntervalMs = 30000;

    public NacosGrpcConfigService(NacosClientOptions options, ILogger<NacosGrpcConfigService>? logger = null)
    {
        _options = options;
        _logger = logger;
        _grpcClient = new NacosGrpcClient(options, logger);
        _transportClient = new ConfigRpcTransportClient(_grpcClient, options, logger);
        _localCache = new LocalConfigCache(options);
        _filterChainManager = new ConfigFilterChainManager();
        _cts = new CancellationTokenSource();

        // Register for config change notifications
        _transportClient.OnConfigChanged += HandleConfigChangeNotify;
        _transportClient.OnFuzzyWatchChanged += HandleFuzzyWatchChangeNotify;
    }

    /// <summary>
    /// Initializes the gRPC connection.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;
        
        await _grpcClient.ConnectAsync(cancellationToken);
        _isHealthy = true;
        _initialized = true;

        // Start background listen task
        _listenTask = ListenConfigLoopAsync(_cts.Token);
        
        _logger?.LogInformation("NacosGrpcConfigService initialized");
    }

    #region IConfigService Implementation

    public async Task<string?> GetConfigAsync(string dataId, string group, long timeoutMs, 
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        group = GetGroupOrDefault(group);
        var tenant = GetTenant();

        try
        {
            // Check cache first
            var cacheKey = GetCacheKey(dataId, group, tenant);
            if (_configCache.TryGetValue(cacheKey, out var cached) && cached.Content != null)
            {
                return cached.Content;
            }

            // Query from server
            var response = await _transportClient.QueryConfigAsync(dataId, group, tenant, 
                cancellationToken: cancellationToken);

            if (response?.IsSuccess == true && response.Content != null)
            {
                var content = response.Content;
                
                // Apply filters (decrypt if needed)
                content = await ApplyFiltersOnGetAsync(dataId, group, tenant, content, 
                    response.EncryptedDataKey, cancellationToken);

                // Update cache
                UpdateCache(dataId, group, tenant, content, response.Md5, response.ContentType, 
                    response.EncryptedDataKey, response.LastModified);

                // Save to local cache for failover
                _localCache.SaveSnapshot(dataId, group, content);
                
                _isHealthy = true;
                return content;
            }

            // Try local cache as failover
            return _localCache.GetSnapshot(dataId, group);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get config {DataId}@{Group} from gRPC server", dataId, group);
            _isHealthy = false;

            // Try local cache as failover
            var cachedContent = _localCache.GetSnapshot(dataId, group);
            if (cachedContent != null)
            {
                _logger?.LogInformation("Using local cache for {DataId}@{Group}", dataId, group);
                return cachedContent;
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
        await EnsureInitializedAsync(cancellationToken);
        
        group = GetGroupOrDefault(group);
        var tenant = GetTenant();
        var cacheKey = GetCacheKey(dataId, group, tenant);

        // Get or create cache data
        var cacheData = _configCache.GetOrAdd(cacheKey, _ => new ConfigCacheData(dataId, group, tenant));
        cacheData.AddListener(listener);

        // Try to get current content and MD5
        if (string.IsNullOrEmpty(cacheData.Md5))
        {
            try
            {
                var content = await GetConfigAsync(dataId, group, _options.DefaultTimeout, cancellationToken);
                if (content != null)
                {
                    cacheData.Content = content;
                    cacheData.Md5 = NacosUtils.GetMd5(content);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to get initial config for listener {DataId}@{Group}", dataId, group);
            }
        }

        // Send listen request to server
        await SendListenRequestAsync(new List<ConfigCacheData> { cacheData }, true, cancellationToken);

        _logger?.LogDebug("Added gRPC listener for {DataId}@{Group}", dataId, group);
    }

    public void RemoveListener(string dataId, string group, IConfigChangeListener listener)
    {
        group = GetGroupOrDefault(group);
        var tenant = GetTenant();
        var cacheKey = GetCacheKey(dataId, group, tenant);

        if (_configCache.TryGetValue(cacheKey, out var cacheData))
        {
            cacheData.RemoveListener(listener);
            
            // If no more listeners, remove from cache and send unlisten
            if (!cacheData.HasListeners)
            {
                _configCache.TryRemove(cacheKey, out _);
                _ = SendUnlistenRequestAsync(cacheData);
            }
        }

        _logger?.LogDebug("Removed gRPC listener for {DataId}@{Group}", dataId, group);
    }

    public async Task<bool> PublishConfigAsync(string dataId, string group, string content, 
        CancellationToken cancellationToken = default)
    {
        return await PublishConfigAsync(dataId, group, content, ConfigType.Default, cancellationToken);
    }

    public async Task<bool> PublishConfigAsync(string dataId, string group, string content, string type, 
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        group = GetGroupOrDefault(group);
        var tenant = GetTenant();

        // Apply filters (encrypt if needed)
        var filterResult = await ApplyFiltersOnPublishAsync(dataId, group, tenant, content, type, cancellationToken);

        var result = await _transportClient.PublishConfigAsync(dataId, group, tenant,
            filterResult.Content, type, null, filterResult.EncryptedDataKey, null, cancellationToken);

        if (result)
        {
            _logger?.LogDebug("Published config {DataId}@{Group}", dataId, group);
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
        await EnsureInitializedAsync(cancellationToken);
        
        group = GetGroupOrDefault(group);
        var tenant = GetTenant();

        // Apply filters (encrypt if needed)
        var filterResult = await ApplyFiltersOnPublishAsync(dataId, group, tenant, content, type, cancellationToken);

        var result = await _transportClient.PublishConfigAsync(dataId, group, tenant,
            filterResult.Content, type, casMd5, filterResult.EncryptedDataKey, null, cancellationToken);

        if (result)
        {
            _logger?.LogDebug("Published config with CAS {DataId}@{Group}", dataId, group);
        }

        return result;
    }

    public async Task<bool> RemoveConfigAsync(string dataId, string group, 
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        group = GetGroupOrDefault(group);
        var tenant = GetTenant();

        var result = await _transportClient.RemoveConfigAsync(dataId, group, tenant, null, cancellationToken);

        if (result)
        {
            _localCache.RemoveSnapshot(dataId, group);
            
            var cacheKey = GetCacheKey(dataId, group, tenant);
            _configCache.TryRemove(cacheKey, out _);
            
            _logger?.LogDebug("Removed config {DataId}@{Group}", dataId, group);
        }

        return result;
    }

    public string GetServerStatus()
    {
        return _isHealthy && _grpcClient.IsConnected ? "UP" : "DOWN";
    }

    public void AddConfigFilter(IConfigFilter configFilter)
    {
        _filterChainManager.AddFilter(configFilter);
        _logger?.LogDebug("Added config filter: {FilterName}", configFilter.FilterName);
    }

    #endregion

    #region Fuzzy Watch Implementation

    public Task FuzzyWatchAsync(string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        return FuzzyWatchAsync("*", groupNamePattern, watcher, cancellationToken);
    }

    public async Task FuzzyWatchAsync(string dataIdPattern, string groupNamePattern, 
        IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        var tenant = GetTenant() ?? "";
        var watchKey = GetFuzzyWatchKey(dataIdPattern, groupNamePattern, tenant);

        // Add to local watchers
        var entry = _fuzzyWatchers.GetOrAdd(watchKey, _ => new FuzzyWatchEntry
        {
            DataIdPattern = dataIdPattern,
            GroupPattern = groupNamePattern,
            Tenant = tenant
        });
        entry.AddWatcher(watcher);

        // Send watch request to server
        var context = new ConfigFuzzyListenContext
        {
            DataIdPattern = dataIdPattern,
            GroupPattern = groupNamePattern
        };

        await _transportClient.SendFuzzyWatchAsync(new List<ConfigFuzzyListenContext> { context }, 
            true, cancellationToken);

        _logger?.LogDebug("Added fuzzy watch for dataId={DataIdPattern}, group={GroupPattern}", 
            dataIdPattern, groupNamePattern);
    }

    public Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string groupNamePattern, 
        IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        return FuzzyWatchWithGroupKeysAsync("*", groupNamePattern, watcher, cancellationToken);
    }

    public async Task<ISet<string>> FuzzyWatchWithGroupKeysAsync(string dataIdPattern, string groupNamePattern, 
        IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        var tenant = GetTenant() ?? "";
        var watchKey = GetFuzzyWatchKey(dataIdPattern, groupNamePattern, tenant);

        // Add to local watchers
        var entry = _fuzzyWatchers.GetOrAdd(watchKey, _ => new FuzzyWatchEntry
        {
            DataIdPattern = dataIdPattern,
            GroupPattern = groupNamePattern,
            Tenant = tenant
        });
        entry.AddWatcher(watcher);

        // Send watch request and get matching keys
        var context = new ConfigFuzzyListenContext
        {
            DataIdPattern = dataIdPattern,
            GroupPattern = groupNamePattern
        };

        var response = await _transportClient.FuzzyWatchAsync(new List<ConfigFuzzyListenContext> { context }, 
            true, cancellationToken);

        var matchedKeys = new HashSet<string>();
        if (response?.MatchedGroupKeys != null)
        {
            foreach (var key in response.MatchedGroupKeys)
            {
                matchedKeys.Add(key);
                _knownConfigs.TryAdd(key, new HashSet<string> { watchKey });
            }
        }

        _logger?.LogDebug("Added fuzzy watch with keys for dataId={DataIdPattern}, group={GroupPattern}, matched={Count}", 
            dataIdPattern, groupNamePattern, matchedKeys.Count);

        return matchedKeys;
    }

    public Task CancelFuzzyWatchAsync(string groupNamePattern, IConfigFuzzyWatchEventWatcher watcher, 
        CancellationToken cancellationToken = default)
    {
        return CancelFuzzyWatchAsync("*", groupNamePattern, watcher, cancellationToken);
    }

    public async Task CancelFuzzyWatchAsync(string dataIdPattern, string groupNamePattern, 
        IConfigFuzzyWatchEventWatcher watcher, CancellationToken cancellationToken = default)
    {
        var tenant = GetTenant() ?? "";
        var watchKey = GetFuzzyWatchKey(dataIdPattern, groupNamePattern, tenant);

        if (_fuzzyWatchers.TryGetValue(watchKey, out var entry))
        {
            entry.RemoveWatcher(watcher);
            
            if (!entry.HasWatchers)
            {
                _fuzzyWatchers.TryRemove(watchKey, out _);

                // Send unwatch request
                var context = new ConfigFuzzyListenContext
                {
                    DataIdPattern = dataIdPattern,
                    GroupPattern = groupNamePattern
                };

                await _transportClient.SendFuzzyWatchAsync(new List<ConfigFuzzyListenContext> { context }, 
                    false, cancellationToken);
            }
        }

        _logger?.LogDebug("Cancelled fuzzy watch for dataId={DataIdPattern}, group={GroupPattern}", 
            dataIdPattern, groupNamePattern);
    }

    #endregion

    #region Private Methods

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await InitializeAsync(cancellationToken);
        }
    }

    private async Task ListenConfigLoopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting config listen loop");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(ListenCheckIntervalMs, cancellationToken);

                if (!_grpcClient.IsConnected)
                {
                    continue;
                }

                // Re-register all listeners
                var allCacheData = _configCache.Values.Where(c => c.HasListeners).ToList();
                if (allCacheData.Count > 0)
                {
                    await SendListenRequestAsync(allCacheData, true, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error in config listen loop");
            }
        }

        _logger?.LogInformation("Config listen loop stopped");
    }

    private async Task SendListenRequestAsync(List<ConfigCacheData> cacheDataList, bool listen, 
        CancellationToken cancellationToken)
    {
        if (cacheDataList.Count == 0) return;

        await _listenLock.WaitAsync(cancellationToken);
        try
        {
            var contexts = cacheDataList.Select(c => new ConfigListenContext
            {
                DataId = c.DataId,
                Group = c.Group,
                Tenant = c.Tenant,
                Md5 = c.Md5 ?? ""
            }).ToList();

            await _transportClient.SendBatchListenAsync(contexts, listen, cancellationToken);
        }
        finally
        {
            _listenLock.Release();
        }
    }

    private async Task SendUnlistenRequestAsync(ConfigCacheData cacheData)
    {
        try
        {
            await SendListenRequestAsync(new List<ConfigCacheData> { cacheData }, false, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send unlisten request for {DataId}@{Group}", 
                cacheData.DataId, cacheData.Group);
        }
    }

    private void HandleConfigChangeNotify(ConfigChangeNotifyRequest request)
    {
        var cacheKey = GetCacheKey(request.DataId, request.Group, request.Tenant);

        _logger?.LogDebug("Received config change notify for {DataId}@{Group}", request.DataId, request.Group);

        if (_configCache.TryGetValue(cacheKey, out var cacheData))
        {
            // If server pushed the content directly, use it
            if (request.ContentPush && request.Content != null)
            {
                _ = ProcessContentPushAsync(cacheData, request);
            }
            else
            {
                // Need to fetch new content
                _ = RefreshConfigAsync(cacheData);
            }
        }
    }

    private async Task ProcessContentPushAsync(ConfigCacheData cacheData, ConfigChangeNotifyRequest request)
    {
        try
        {
            var content = request.Content!;
            
            // Apply filters (decrypt if needed)
            content = await ApplyFiltersOnGetAsync(cacheData.DataId, cacheData.Group, cacheData.Tenant, 
                content, request.EncryptedDataKey, CancellationToken.None);

            var oldMd5 = cacheData.Md5;
            var newMd5 = request.Md5 ?? NacosUtils.GetMd5(content);

            if (oldMd5 != newMd5)
            {
                cacheData.Content = content;
                cacheData.Md5 = newMd5;
                cacheData.Type = request.Type;
                cacheData.EncryptedDataKey = request.EncryptedDataKey;
                cacheData.LastModified = request.LastModified;

                // Save to local cache
                _localCache.SaveSnapshot(cacheData.DataId, cacheData.Group, content);

                // Notify listeners
                NotifyListeners(cacheData, content, newMd5);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing content push for {DataId}@{Group}", 
                cacheData.DataId, cacheData.Group);
        }
    }

    private async Task RefreshConfigAsync(ConfigCacheData cacheData)
    {
        try
        {
            var response = await _transportClient.QueryConfigAsync(
                cacheData.DataId, cacheData.Group, cacheData.Tenant, cancellationToken: CancellationToken.None);

            if (response?.IsSuccess == true && response.Content != null)
            {
                var content = response.Content;
                
                // Apply filters (decrypt if needed)
                content = await ApplyFiltersOnGetAsync(cacheData.DataId, cacheData.Group, cacheData.Tenant, 
                    content, response.EncryptedDataKey, CancellationToken.None);

                var oldMd5 = cacheData.Md5;
                var newMd5 = response.Md5 ?? NacosUtils.GetMd5(content);

                if (oldMd5 != newMd5)
                {
                    cacheData.Content = content;
                    cacheData.Md5 = newMd5;
                    cacheData.Type = response.ContentType;
                    cacheData.EncryptedDataKey = response.EncryptedDataKey;
                    cacheData.LastModified = response.LastModified;

                    // Save to local cache
                    _localCache.SaveSnapshot(cacheData.DataId, cacheData.Group, content);

                    // Notify listeners
                    NotifyListeners(cacheData, content, newMd5);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing config {DataId}@{Group}", 
                cacheData.DataId, cacheData.Group);
        }
    }

    private void NotifyListeners(ConfigCacheData cacheData, string content, string? md5)
    {
        var configInfo = new ConfigInfo
        {
            DataId = cacheData.DataId,
            Group = cacheData.Group,
            Tenant = cacheData.Tenant,
            Content = content,
            Md5 = md5,
            Type = cacheData.Type,
            EncryptedDataKey = cacheData.EncryptedDataKey,
            LastModified = cacheData.LastModified
        };

        foreach (var listener in cacheData.GetListeners())
        {
            try
            {
                listener.OnReceiveConfigInfo(configInfo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error notifying listener for {DataId}@{Group}", 
                    cacheData.DataId, cacheData.Group);
            }
        }
    }

    private void HandleFuzzyWatchChangeNotify(ConfigFuzzyWatchChangeNotifyRequest request)
    {
        _logger?.LogDebug("Received fuzzy watch change notify for {DataId}@{Group}, Type={ChangeType}", 
            request.DataId, request.Group, request.ChangedType);

        // Update known configs
        var configKey = GetCacheKey(request.DataId, request.Group, request.Tenant);
        if (request.ChangedType == ConfigChangedType.AddConfig)
        {
            _knownConfigs.TryAdd(configKey, new HashSet<string>());
        }
        else if (request.ChangedType == ConfigChangedType.DeleteConfig)
        {
            _knownConfigs.TryRemove(configKey, out _);
        }

        // Notify matching watchers
        foreach (var entry in _fuzzyWatchers.Values)
        {
            if (!string.IsNullOrEmpty(entry.Tenant) && entry.Tenant != request.Tenant)
            {
                continue;
            }

            if (MatchesPattern(request.DataId, entry.DataIdPattern) && 
                MatchesPattern(request.Group, entry.GroupPattern))
            {
                var changeEvent = ConfigFuzzyWatchChangeEvent.Build(
                    request.Tenant ?? "",
                    request.Group,
                    request.DataId,
                    request.ChangedType,
                    request.SyncType);

                foreach (var watcher in entry.GetWatchers())
                {
                    try
                    {
                        var scheduler = watcher.Scheduler;
                        if (scheduler != null)
                        {
                            Task.Factory.StartNew(() => watcher.OnEvent(changeEvent), 
                                CancellationToken.None, TaskCreationOptions.None, scheduler);
                        }
                        else
                        {
                            watcher.OnEvent(changeEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error notifying fuzzy watcher for {DataId}@{Group}", 
                            request.DataId, request.Group);
                    }
                }
            }
        }
    }

    private void UpdateCache(string dataId, string group, string? tenant, string content, 
        string? md5, string? type, string? encryptedDataKey, long lastModified)
    {
        var cacheKey = GetCacheKey(dataId, group, tenant);
        var cacheData = _configCache.GetOrAdd(cacheKey, _ => new ConfigCacheData(dataId, group, tenant));
        
        cacheData.Content = content;
        cacheData.Md5 = md5 ?? NacosUtils.GetMd5(content);
        cacheData.Type = type;
        cacheData.EncryptedDataKey = encryptedDataKey;
        cacheData.LastModified = lastModified;
    }

    private async Task<string> ApplyFiltersOnGetAsync(string dataId, string group, string? tenant,
        string content, string? encryptedDataKey, CancellationToken cancellationToken)
    {
        if (!_filterChainManager.HasFilters)
        {
            return content;
        }

        var request = new Core.Config.Filter.ConfigRequest(dataId, group, tenant, content);
        request.EncryptedDataKey = encryptedDataKey;
        var response = new Core.Config.Filter.ConfigResponse { Content = content };
        response.EncryptedDataKey = encryptedDataKey;

        await _filterChainManager.DoFilterAsync(request, response, cancellationToken);

        return response.Content ?? content;
    }

    private async Task<FilterResult> ApplyFiltersOnPublishAsync(string dataId, string group, string? tenant,
        string content, string? type, CancellationToken cancellationToken)
    {
        if (!_filterChainManager.HasFilters)
        {
            return new FilterResult(content, null);
        }

        var request = new Core.Config.Filter.ConfigRequest(dataId, group, tenant, content);
        request.PutParameter(ConfigRequestKeys.Type, type);
        var response = new Core.Config.Filter.ConfigResponse { Content = content };

        await _filterChainManager.DoFilterAsync(request, response, cancellationToken);

        return new FilterResult(response.Content ?? content, response.EncryptedDataKey);
    }

    private static bool MatchesPattern(string value, string pattern)
    {
        if (pattern == "*") return true;
        
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        
        return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
    }

    private string GetGroupOrDefault(string? group)
    {
        return string.IsNullOrWhiteSpace(group) ? NacosConstants.DefaultGroup : group.Trim();
    }

    private string? GetTenant()
    {
        return string.IsNullOrWhiteSpace(_options.Namespace) ? null : _options.Namespace;
    }

    private static string GetCacheKey(string dataId, string group, string? tenant)
    {
        return NacosUtils.GetGroupKey(dataId, group, tenant);
    }

    private static string GetFuzzyWatchKey(string dataIdPattern, string groupPattern, string tenant)
    {
        return $"{dataIdPattern}@@{groupPattern}@@{tenant}";
    }

    #endregion

    #region Lifecycle

    public async Task ShutdownAsync()
    {
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _cts.CancelAsync();
        
        // Wait for listen task
        if (_listenTask != null)
        {
            try { await _listenTask.WaitAsync(TimeSpan.FromSeconds(2)); } catch { /* Ignore */ }
        }

        await _transportClient.DisposeAsync();
        await _grpcClient.DisposeAsync();
        
        _cts.Dispose();
        _listenLock.Dispose();
        
        _logger?.LogInformation("NacosGrpcConfigService disposed");
    }

    #endregion

    #region Internal Types

    private readonly record struct FilterResult(string Content, string? EncryptedDataKey);

    private class ConfigCacheData
    {
        private readonly List<IConfigChangeListener> _listeners = new();
        private readonly object _lock = new();

        public string DataId { get; }
        public string Group { get; }
        public string? Tenant { get; }
        public string? Content { get; set; }
        public string? Md5 { get; set; }
        public string? Type { get; set; }
        public string? EncryptedDataKey { get; set; }
        public long LastModified { get; set; }

        public bool HasListeners
        {
            get { lock (_lock) { return _listeners.Count > 0; } }
        }

        public ConfigCacheData(string dataId, string group, string? tenant)
        {
            DataId = dataId;
            Group = group;
            Tenant = tenant;
        }

        public void AddListener(IConfigChangeListener listener)
        {
            lock (_lock)
            {
                if (!_listeners.Contains(listener))
                {
                    _listeners.Add(listener);
                }
            }
        }

        public void RemoveListener(IConfigChangeListener listener)
        {
            lock (_lock)
            {
                _listeners.Remove(listener);
            }
        }

        public List<IConfigChangeListener> GetListeners()
        {
            lock (_lock)
            {
                return new List<IConfigChangeListener>(_listeners);
            }
        }
    }

    private class FuzzyWatchEntry
    {
        private readonly List<IConfigFuzzyWatchEventWatcher> _watchers = new();
        private readonly object _lock = new();

        public string DataIdPattern { get; init; } = "";
        public string GroupPattern { get; init; } = "";
        public string Tenant { get; init; } = "";

        public bool HasWatchers
        {
            get { lock (_lock) { return _watchers.Count > 0; } }
        }

        public void AddWatcher(IConfigFuzzyWatchEventWatcher watcher)
        {
            lock (_lock)
            {
                if (!_watchers.Contains(watcher))
                {
                    _watchers.Add(watcher);
                }
            }
        }

        public void RemoveWatcher(IConfigFuzzyWatchEventWatcher watcher)
        {
            lock (_lock)
            {
                _watchers.Remove(watcher);
            }
        }

        public List<IConfigFuzzyWatchEventWatcher> GetWatchers()
        {
            lock (_lock)
            {
                return new List<IConfigFuzzyWatchEventWatcher>(_watchers);
            }
        }
    }

    #endregion
}

/// <summary>
/// Local config cache for failover.
/// </summary>
internal class LocalConfigCache
{
    private readonly string _cacheDir;

    public LocalConfigCache(NacosClientOptions options)
    {
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "nacos", "config", options.Namespace ?? "public");
        
        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }
    }

    public void SaveSnapshot(string dataId, string group, string content)
    {
        try
        {
            var fileName = GetFileName(dataId, group);
            var filePath = Path.Combine(_cacheDir, fileName);
            File.WriteAllText(filePath, content);
        }
        catch
        {
            // Ignore cache save errors
        }
    }

    public string? GetSnapshot(string dataId, string group)
    {
        try
        {
            var fileName = GetFileName(dataId, group);
            var filePath = Path.Combine(_cacheDir, fileName);
            
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
        }
        catch
        {
            // Ignore cache read errors
        }
        
        return null;
    }

    public void RemoveSnapshot(string dataId, string group)
    {
        try
        {
            var fileName = GetFileName(dataId, group);
            var filePath = Path.Combine(_cacheDir, fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore cache delete errors
        }
    }

    private static string GetFileName(string dataId, string group)
    {
        return $"{group}@@{dataId}".Replace("/", "_").Replace("\\", "_");
    }
}
