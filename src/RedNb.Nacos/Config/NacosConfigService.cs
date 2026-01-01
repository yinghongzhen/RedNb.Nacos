using RedNb.Nacos.Config.Models;
using RedNb.Nacos.Remote.Http;

namespace RedNb.Nacos.Config;

/// <summary>
/// Nacos 配置服务实现
/// </summary>
public sealed class NacosConfigService : INacosConfigService
{
    private readonly NacosOptions _options;
    private readonly INacosHttpClient _httpClient;
    private readonly ILogger<NacosConfigService> _logger;

    private readonly ConcurrentDictionary<string, CacheData> _cacheMap = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _longPollingTask;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public NacosConfigService(
        IOptions<NacosOptions> options,
        INacosHttpClient httpClient,
        ILogger<NacosConfigService> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;

        // 启动长轮询任务
        StartLongPolling();
    }

    #region 配置读取

    /// <inheritdoc />
    public async Task<string?> GetConfigAsync(
        string dataId,
        string group,
        long timeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataId);
        group = string.IsNullOrEmpty(group) ? NacosConstants.DefaultGroup : group;

        var cacheKey = GetCacheKey(dataId, group);

        // 先从缓存获取
        if (_cacheMap.TryGetValue(cacheKey, out var cacheData) && !string.IsNullOrEmpty(cacheData.Content))
        {
            return cacheData.Content;
        }

        // 从服务端获取
        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["groupName"] = group,
                ["namespaceId"] = _options.Namespace
            };

            var content = await _httpClient.GetStringAsync(
                EndpointConstants.Config_Get,
                queryParams,
                requireAuth: false,
                cancellationToken);

            // 更新缓存
            if (content != null)
            {
                UpdateCache(dataId, group, content);
            }

            return content;
        }
        catch (NacosException ex) when (ex.ErrorCode == ErrorCodes.NotFound)
        {
            _logger.LogWarning("配置不存在: dataId={DataId}, group={Group}", dataId, group);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetConfigAsync<T>(
        string dataId,
        string group,
        long timeoutMs = 3000,
        CancellationToken cancellationToken = default) where T : class
    {
        var content = await GetConfigAsync(dataId, group, timeoutMs, cancellationToken);

        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "配置反序列化失败: dataId={DataId}, group={Group}", dataId, group);
            throw new NacosConfigException($"配置反序列化失败: {ex.Message}", dataId, group);
        }
    }

    #endregion

    #region 配置发布

    /// <inheritdoc />
    public async Task<bool> PublishConfigAsync(
        string dataId,
        string group,
        string content,
        string? configType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataId);
        ArgumentException.ThrowIfNullOrEmpty(content);
        group = string.IsNullOrEmpty(group) ? NacosConstants.DefaultGroup : group;

        var formParams = new Dictionary<string, string>
        {
            ["dataId"] = dataId,
            ["groupName"] = group,
            ["namespaceId"] = _options.Namespace,
            ["content"] = content
        };

        if (!string.IsNullOrEmpty(configType))
        {
            formParams["type"] = configType;
        }

        var result = await _httpClient.PostStringAsync(
            EndpointConstants.Config_Publish,
            formParams,
            requireAuth: true,
            cancellationToken);

        var success = result?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (success)
        {
            _logger.LogInformation("配置发布成功: dataId={DataId}, group={Group}", dataId, group);
            UpdateCache(dataId, group, content);
        }

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveConfigAsync(
        string dataId,
        string group,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataId);
        group = string.IsNullOrEmpty(group) ? NacosConstants.DefaultGroup : group;

        var queryParams = new Dictionary<string, string>
        {
            ["dataId"] = dataId,
            ["groupName"] = group,
            ["namespaceId"] = _options.Namespace
        };

        var success = await _httpClient.DeleteAsync(
            EndpointConstants.Config_Delete,
            queryParams,
            requireAuth: true,
            cancellationToken);

        if (success)
        {
            _logger.LogInformation("配置删除成功: dataId={DataId}, group={Group}", dataId, group);
            var cacheKey = GetCacheKey(dataId, group);
            _cacheMap.TryRemove(cacheKey, out _);
        }

        return success;
    }

    #endregion

    #region 配置监听

    /// <inheritdoc />
    public Task AddListenerAsync(
        string dataId,
        string group,
        IConfigListener listener,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataId);
        ArgumentNullException.ThrowIfNull(listener);
        group = string.IsNullOrEmpty(group) ? NacosConstants.DefaultGroup : group;

        var cacheKey = GetCacheKey(dataId, group);
        var cacheData = _cacheMap.GetOrAdd(cacheKey, _ => new CacheData
        {
            DataId = dataId,
            Group = group,
            Namespace = _options.Namespace
        });

        lock (cacheData.Listeners)
        {
            if (!cacheData.Listeners.Contains(listener))
            {
                cacheData.Listeners.Add(listener);
            }
        }

        _logger.LogDebug("添加配置监听器: dataId={DataId}, group={Group}", dataId, group);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddListenerAsync(
        string dataId,
        string group,
        Action<string?> callback,
        CancellationToken cancellationToken = default)
    {
        var listener = new ActionConfigListener(callback);
        return AddListenerAsync(dataId, group, listener, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveListenerAsync(
        string dataId,
        string group,
        IConfigListener listener,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataId);
        ArgumentNullException.ThrowIfNull(listener);
        group = string.IsNullOrEmpty(group) ? NacosConstants.DefaultGroup : group;

        var cacheKey = GetCacheKey(dataId, group);
        if (_cacheMap.TryGetValue(cacheKey, out var cacheData))
        {
            lock (cacheData.Listeners)
            {
                cacheData.Listeners.Remove(listener);
            }

            _logger.LogDebug("移除配置监听器: dataId={DataId}, group={Group}", dataId, group);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region 健康检查

    /// <inheritdoc />
    public async Task<string> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetStringAsync(
                EndpointConstants.Health_Ready,
                cancellationToken: cancellationToken);

            return result ?? "UNKNOWN";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取服务状态失败");
            return "DOWN";
        }
    }

    #endregion

    #region 私有方法

    private string GetCacheKey(string dataId, string group)
    {
        return $"{_options.Namespace}+{group}+{dataId}";
    }

    private void UpdateCache(string dataId, string group, string? content)
    {
        var cacheKey = GetCacheKey(dataId, group);
        var cacheData = _cacheMap.GetOrAdd(cacheKey, _ => new CacheData
        {
            DataId = dataId,
            Group = group,
            Namespace = _options.Namespace
        });

        cacheData.UpdateContent(content);
    }

    private void StartLongPolling()
    {
        _longPollingTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.Config.RefreshIntervalMs, _cts.Token);
                    await CheckConfigChangesAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "配置长轮询异常");
                    await Task.Delay(5000, _cts.Token);
                }
            }
        }, _cts.Token);
    }

    private async Task CheckConfigChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var kvp in _cacheMap)
        {
            var cacheData = kvp.Value;

            if (cacheData.Listeners.Count == 0)
            {
                continue;
            }

            try
            {
                var queryParams = new Dictionary<string, string>
                {
                    ["dataId"] = cacheData.DataId,
                    ["groupName"] = cacheData.Group,
                    ["namespaceId"] = cacheData.Namespace
                };

                var newContent = await _httpClient.GetStringAsync(
                    EndpointConstants.Config_Get,
                    queryParams,
                    requireAuth: false,
                    cancellationToken);

                var oldContent = cacheData.Content;
                if (cacheData.UpdateContent(newContent))
                {
                    _logger.LogInformation(
                        "检测到配置变更: dataId={DataId}, group={Group}",
                        cacheData.DataId,
                        cacheData.Group);

                    // 通知监听器
                    var eventArgs = new ConfigChangedEventArgs
                    {
                        DataId = cacheData.DataId,
                        Group = cacheData.Group,
                        Namespace = cacheData.Namespace,
                        Content = newContent,
                        OldContent = oldContent,
                        ChangeType = string.IsNullOrEmpty(oldContent)
                            ? ConfigChangeType.Added
                            : ConfigChangeType.Modified
                    };

                    foreach (var listener in cacheData.Listeners.ToList())
                    {
                        try
                        {
                            await listener.ReceiveConfigInfoAsync(eventArgs);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "配置监听器执行异常");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查配置变更失败: dataId={DataId}", cacheData.DataId);
            }
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _cts.CancelAsync();

        if (_longPollingTask != null)
        {
            try
            {
                await _longPollingTask;
            }
            catch (OperationCanceledException)
            {
                // 忽略取消异常
            }
        }

        _cts.Dispose();
    }
}
