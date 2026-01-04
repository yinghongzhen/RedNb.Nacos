using RedNb.Nacos.Common.Failover;
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
    private readonly INacosGrpcClient? _grpcClient;
    private readonly IConfigSnapshot _configSnapshot;
    private readonly ILogger<NacosConfigService> _logger;

    private readonly ConcurrentDictionary<string, CacheData> _cacheMap = new();
    private readonly ConcurrentDictionary<string, List<IConfigListener>> _fuzzyWatchers = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _longPollingTask;
    private bool _disposed;
    private bool _useGrpc;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public NacosConfigService(
        IOptions<NacosOptions> options,
        INacosHttpClient httpClient,
        IConfigSnapshot configSnapshot,
        ILogger<NacosConfigService> logger,
        INacosGrpcClient? grpcClient = null)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _grpcClient = grpcClient;
        _configSnapshot = configSnapshot;
        _logger = logger;
        _useGrpc = _options.UseGrpc && grpcClient != null;

        // 注册 gRPC 推送处理器
        if (_useGrpc && _grpcClient != null)
        {
            _grpcClient.RegisterPushHandler<ConfigChangeNotifyRequest>(HandleConfigChangeNotifyAsync);
        }

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
            string? content = null;

            if (_useGrpc && _grpcClient != null)
            {
                content = await GetConfigByGrpcAsync(dataId, group, cancellationToken);
            }
            else
            {
                content = await GetConfigByHttpAsync(dataId, group, cancellationToken);
            }

            // 更新缓存和快照
            if (content != null)
            {
                UpdateCache(dataId, group, content);
                await _configSnapshot.SaveSnapshotAsync(dataId, group, _options.Namespace, content, cancellationToken);
            }

            return content;
        }
        catch (NacosException ex) when (ex.ErrorCode == ErrorCodes.NotFound)
        {
            _logger.LogWarning("配置不存在: dataId={DataId}, group={Group}", dataId, group);
            return null;
        }
        catch (NacosConnectionException ex)
        {
            _logger.LogWarning(ex, "连接失败，尝试从快照获取配置: dataId={DataId}, group={Group}", dataId, group);

            // 尝试从快照获取
            var snapshotContent = await _configSnapshot.GetSnapshotAsync(dataId, group, _options.Namespace, cancellationToken);
            if (snapshotContent != null)
            {
                _logger.LogInformation("从快照获取配置成功: dataId={DataId}, group={Group}", dataId, group);
                UpdateCache(dataId, group, snapshotContent);
                return snapshotContent;
            }

            throw;
        }
    }

    private async Task<string?> GetConfigByGrpcAsync(string dataId, string group, CancellationToken cancellationToken)
    {
        var request = new ConfigQueryRequest
        {
            DataId = dataId,
            Group = group,
            Tenant = _options.Namespace
        };

        var response = await _grpcClient!.RequestAsync<ConfigQueryRequest, ConfigQueryResponse>(
            request, cancellationToken);

        if (response == null || !response.IsSuccess)
        {
            if (response?.ErrorCode == ErrorCodes.NotFound)
            {
                throw new NacosException(ErrorCodes.NotFound, "配置不存在");
            }
            throw new NacosException(response?.ErrorCode ?? 500, response?.Message ?? "获取配置失败");
        }

        return response.Content;
    }

    private async Task<string?> GetConfigByHttpAsync(string dataId, string group, CancellationToken cancellationToken)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["dataId"] = dataId,
            ["groupName"] = group,
            ["namespaceId"] = _options.Namespace
        };

        return await _httpClient.GetStringAsync(
            EndpointConstants.Config_Get,
            queryParams,
            requireAuth: false,
            cancellationToken);
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

    /// <inheritdoc />
    public async Task<string?> GetConfigAndSignListenerAsync(
        string dataId,
        string group,
        IConfigListener listener,
        long timeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        // 先添加监听器
        await AddListenerAsync(dataId, group, listener, cancellationToken);

        // 再获取配置
        return await GetConfigAsync(dataId, group, timeoutMs, cancellationToken);
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

        bool success;

        if (_useGrpc && _grpcClient != null)
        {
            success = await PublishConfigByGrpcAsync(dataId, group, content, configType, null, cancellationToken);
        }
        else
        {
            success = await PublishConfigByHttpAsync(dataId, group, content, configType, cancellationToken);
        }

        if (success)
        {
            _logger.LogInformation("配置发布成功: dataId={DataId}, group={Group}", dataId, group);
            UpdateCache(dataId, group, content);
            await _configSnapshot.SaveSnapshotAsync(dataId, group, _options.Namespace, content, cancellationToken);
        }

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> PublishConfigCasAsync(
        string dataId,
        string group,
        string content,
        string casMd5,
        string? configType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataId);
        ArgumentException.ThrowIfNullOrEmpty(content);
        ArgumentException.ThrowIfNullOrEmpty(casMd5);
        group = string.IsNullOrEmpty(group) ? NacosConstants.DefaultGroup : group;

        bool success;

        if (_useGrpc && _grpcClient != null)
        {
            success = await PublishConfigByGrpcAsync(dataId, group, content, configType, casMd5, cancellationToken);
        }
        else
        {
            // HTTP 不支持 CAS，回退到普通发布
            _logger.LogWarning("HTTP 模式不支持 CAS 发布，回退到普通发布");
            success = await PublishConfigByHttpAsync(dataId, group, content, configType, cancellationToken);
        }

        if (success)
        {
            _logger.LogInformation("配置CAS发布成功: dataId={DataId}, group={Group}", dataId, group);
            UpdateCache(dataId, group, content);
            await _configSnapshot.SaveSnapshotAsync(dataId, group, _options.Namespace, content, cancellationToken);
        }

        return success;
    }

    private async Task<bool> PublishConfigByGrpcAsync(
        string dataId,
        string group,
        string content,
        string? configType,
        string? casMd5,
        CancellationToken cancellationToken)
    {
        var request = new ConfigPublishRequest
        {
            DataId = dataId,
            Group = group,
            Tenant = _options.Namespace,
            Content = content,
            Type = configType,
            CasMd5 = casMd5
        };

        var response = await _grpcClient!.RequestAsync<ConfigPublishRequest, ConfigPublishResponse>(
            request, cancellationToken);

        return response?.IsSuccess ?? false;
    }

    private async Task<bool> PublishConfigByHttpAsync(
        string dataId,
        string group,
        string content,
        string? configType,
        CancellationToken cancellationToken)
    {
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

        return result?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveConfigAsync(
        string dataId,
        string group,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataId);
        group = string.IsNullOrEmpty(group) ? NacosConstants.DefaultGroup : group;

        bool success;

        if (_useGrpc && _grpcClient != null)
        {
            var request = new ConfigRemoveRequest
            {
                DataId = dataId,
                Group = group,
                Tenant = _options.Namespace
            };

            var response = await _grpcClient.RequestAsync<ConfigRemoveRequest, ConfigRemoveResponse>(
                request, cancellationToken);
            success = response?.IsSuccess ?? false;
        }
        else
        {
            var queryParams = new Dictionary<string, string>
            {
                ["dataId"] = dataId,
                ["groupName"] = group,
                ["namespaceId"] = _options.Namespace
            };

            success = await _httpClient.DeleteAsync(
                EndpointConstants.Config_Delete,
                queryParams,
                requireAuth: true,
                cancellationToken);
        }

        if (success)
        {
            _logger.LogInformation("配置删除成功: dataId={DataId}, group={Group}", dataId, group);
            var cacheKey = GetCacheKey(dataId, group);
            _cacheMap.TryRemove(cacheKey, out _);
            await _configSnapshot.DeleteSnapshotAsync(dataId, group, _options.Namespace, cancellationToken);
        }

        return success;
    }

    #endregion

    #region 配置监听

    /// <inheritdoc />
    public async Task AddListenerAsync(
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

        // 如果使用 gRPC，发送监听请求
        if (_useGrpc && _grpcClient != null)
        {
            await SendConfigListenRequestAsync(cacheData, true, cancellationToken);
        }

        _logger.LogDebug("添加配置监听器: dataId={DataId}, group={Group}", dataId, group);
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
    public async Task RemoveListenerAsync(
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

            // 如果没有监听器了，取消监听
            if (cacheData.Listeners.Count == 0 && _useGrpc && _grpcClient != null)
            {
                await SendConfigListenRequestAsync(cacheData, false, cancellationToken);
            }

            _logger.LogDebug("移除配置监听器: dataId={DataId}, group={Group}", dataId, group);
        }
    }

    private async Task SendConfigListenRequestAsync(CacheData cacheData, bool listen, CancellationToken cancellationToken)
    {
        var request = new ConfigBatchListenRequest
        {
            Tenant = _options.Namespace,
            Listen = listen,
            ConfigListenContexts = new List<ConfigListenContext>
            {
                new ConfigListenContext
                {
                    DataId = cacheData.DataId,
                    Group = cacheData.Group,
                    Tenant = _options.Namespace,
                    Md5 = cacheData.Md5
                }
            }
        };

        await _grpcClient!.RequestAsync<ConfigBatchListenRequest, ConfigChangeBatchListenResponse>(
            request, cancellationToken);
    }

    #endregion

    #region 模糊订阅

    /// <inheritdoc />
    public Task FuzzyWatchAsync(
        string dataIdPattern,
        string groupPattern,
        IConfigListener listener,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataIdPattern);
        ArgumentException.ThrowIfNullOrEmpty(groupPattern);
        ArgumentNullException.ThrowIfNull(listener);

        var key = $"{dataIdPattern}@@{groupPattern}";
        var listeners = _fuzzyWatchers.GetOrAdd(key, _ => new List<IConfigListener>());

        lock (listeners)
        {
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        _logger.LogDebug("添加模糊配置订阅: dataIdPattern={DataIdPattern}, groupPattern={GroupPattern}",
            dataIdPattern, groupPattern);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelFuzzyWatchAsync(
        string dataIdPattern,
        string groupPattern,
        IConfigListener listener,
        CancellationToken cancellationToken = default)
    {
        var key = $"{dataIdPattern}@@{groupPattern}";

        if (_fuzzyWatchers.TryGetValue(key, out var listeners))
        {
            lock (listeners)
            {
                listeners.Remove(listener);
            }
        }

        _logger.LogDebug("取消模糊配置订阅: dataIdPattern={DataIdPattern}, groupPattern={GroupPattern}",
            dataIdPattern, groupPattern);

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

    #region gRPC 推送处理

    private async Task<NacosResponse?> HandleConfigChangeNotifyAsync(ConfigChangeNotifyRequest request)
    {
        _logger.LogInformation("收到配置变更通知: dataId={DataId}, group={Group}",
            request.DataId, request.Group);

        var cacheKey = GetCacheKey(request.DataId, request.Group);

        if (!_cacheMap.TryGetValue(cacheKey, out var cacheData))
        {
            return new ConfigQueryResponse { ResultCode = 200 };
        }

        // 重新获取配置
        try
        {
            var newContent = await GetConfigAsync(request.DataId, request.Group);
            var oldContent = cacheData.Content;

            if (cacheData.UpdateContent(newContent))
            {
                // 通知监听器
                var eventArgs = new ConfigChangedEventArgs
                {
                    DataId = request.DataId,
                    Group = request.Group,
                    Namespace = _options.Namespace,
                    Content = newContent,
                    OldContent = oldContent,
                    ChangeType = string.IsNullOrEmpty(oldContent)
                        ? ConfigChangeType.Added
                        : ConfigChangeType.Modified
                };

                await NotifyListenersAsync(cacheData, eventArgs);

                // 检查模糊订阅
                await NotifyFuzzyWatchersAsync(eventArgs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理配置变更通知失败: dataId={DataId}", request.DataId);
        }

        return new ConfigQueryResponse { ResultCode = 200 };
    }

    private async Task NotifyListenersAsync(CacheData cacheData, ConfigChangedEventArgs eventArgs)
    {
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

    private async Task NotifyFuzzyWatchersAsync(ConfigChangedEventArgs eventArgs)
    {
        foreach (var (key, listeners) in _fuzzyWatchers)
        {
            var parts = key.Split("@@");
            if (parts.Length != 2) continue;

            var dataIdPattern = parts[0];
            var groupPattern = parts[1];

            if (MatchPattern(eventArgs.DataId, dataIdPattern) &&
                MatchPattern(eventArgs.Group, groupPattern))
            {
                foreach (var listener in listeners.ToList())
                {
                    try
                    {
                        await listener.ReceiveConfigInfoAsync(eventArgs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "模糊订阅监听器执行异常");
                    }
                }
            }
        }
    }

    private static bool MatchPattern(string value, string pattern)
    {
        if (pattern == "*") return true;
        if (!pattern.Contains('*')) return value == pattern;

        var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(value, regex);
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

                    // 如果使用 gRPC，不需要轮询
                    if (_useGrpc)
                    {
                        continue;
                    }

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

                    // 保存快照
                    if (newContent != null)
                    {
                        await _configSnapshot.SaveSnapshotAsync(
                            cacheData.DataId, cacheData.Group, cacheData.Namespace,
                            newContent, cancellationToken);
                    }

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

                    await NotifyListenersAsync(cacheData, eventArgs);
                    await NotifyFuzzyWatchersAsync(eventArgs);
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
