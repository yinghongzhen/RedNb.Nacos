using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RedNb.Nacos.Failover;

/// <summary>
/// 故障转移反应器
/// 负责定期刷新故障转移数据并在需要时提供本地缓存
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class FailoverReactor<T> : IDisposable where T : class
{
    private readonly ILogger _logger;
    private readonly IFailoverDataSource<T>? _dataSource;
    private readonly ConcurrentDictionary<string, FailoverData<T>> _failoverDataCache = new();
    private readonly int _refreshIntervalMs;
    private readonly object _lockObj = new();

    private CancellationTokenSource? _cts;
    private Task? _refreshTask;
    private FailoverSwitch _failoverSwitch = FailoverSwitch.CreateDisabled();

    /// <summary>
    /// 当故障转移数据变更时触发
    /// </summary>
    public event EventHandler<FailoverDataChangedEventArgs<T>>? DataChanged;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="dataSource">数据源，可为null则仅使用手动设置的数据</param>
    /// <param name="refreshIntervalMs">刷新间隔（毫秒）</param>
    public FailoverReactor(ILogger logger, IFailoverDataSource<T>? dataSource = null, int refreshIntervalMs = 5000)
    {
        _logger = logger;
        _dataSource = dataSource;
        _refreshIntervalMs = refreshIntervalMs;
    }

    /// <summary>
    /// 获取故障转移是否启用
    /// </summary>
    public bool IsFailoverEnabled => _failoverSwitch.Enabled;

    /// <summary>
    /// 启动故障转移反应器
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_dataSource == null)
        {
            _logger.LogInformation("Failover reactor started without data source, manual failover data management only");
            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _refreshTask = Task.Run(() => RefreshLoopAsync(_cts.Token), _cts.Token);
        _logger.LogInformation("Failover reactor started with {Interval}ms refresh interval", _refreshIntervalMs);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止故障转移反应器
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();
        if (_refreshTask != null)
        {
            try
            {
                await _refreshTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 预期的取消
            }
        }
        _logger.LogInformation("Failover reactor stopped");
    }

    /// <summary>
    /// 获取故障转移数据
    /// </summary>
    public T? GetFailoverData(string key)
    {
        if (!_failoverSwitch.Enabled)
        {
            return null;
        }

        if (_failoverDataCache.TryGetValue(key, out var data))
        {
            _logger.LogDebug("Using failover data for key: {Key}", key);
            return data.Data;
        }

        return null;
    }

    /// <summary>
    /// 尝试获取故障转移数据
    /// </summary>
    public bool TryGetFailoverData(string key, out T? data)
    {
        data = null;

        if (!_failoverSwitch.Enabled)
        {
            return false;
        }

        if (_failoverDataCache.TryGetValue(key, out var failoverData))
        {
            data = failoverData.Data;
            _logger.LogDebug("Using failover data for key: {Key}", key);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 手动设置故障转移数据
    /// </summary>
    public void SetFailoverData(string key, FailoverData<T> data)
    {
        var oldData = _failoverDataCache.AddOrUpdate(key, data, (_, _) => data);
        _logger.LogDebug("Set failover data for key: {Key}", key);

        if (oldData.Data != data.Data)
        {
            OnDataChanged(new FailoverDataChangedEventArgs<T>(key, oldData, data));
        }
    }

    /// <summary>
    /// 移除故障转移数据
    /// </summary>
    public void RemoveFailoverData(string key)
    {
        if (_failoverDataCache.TryRemove(key, out var removed))
        {
            _logger.LogDebug("Removed failover data for key: {Key}", key);
            OnDataChanged(new FailoverDataChangedEventArgs<T>(key, removed, null));
        }
    }

    /// <summary>
    /// 设置故障转移开关
    /// </summary>
    public void SetFailoverSwitch(bool enabled)
    {
        lock (_lockObj)
        {
            var oldEnabled = _failoverSwitch.Enabled;
            _failoverSwitch.Enabled = enabled;

            if (oldEnabled != enabled)
            {
                _logger.LogInformation("Failover switch changed: {OldState} -> {NewState}", oldEnabled, enabled);
            }
        }
    }

    /// <summary>
    /// 刷新循环
    /// </summary>
    private async Task RefreshLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_refreshIntervalMs, cancellationToken);
                RefreshFailoverData();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing failover data");
            }
        }
    }

    /// <summary>
    /// 刷新故障转移数据
    /// </summary>
    private void RefreshFailoverData()
    {
        if (_dataSource == null)
        {
            return;
        }

        try
        {
            // 刷新开关状态
            var switchData = _dataSource.GetSwitch();
            lock (_lockObj)
            {
                if (_failoverSwitch.Enabled != switchData.Enabled)
                {
                    _logger.LogInformation("Failover switch changed from data source: {OldState} -> {NewState}",
                        _failoverSwitch.Enabled, switchData.Enabled);
                    _failoverSwitch = switchData;
                }
            }

            // 刷新故障转移数据
            if (_failoverSwitch.Enabled)
            {
                var newData = _dataSource.GetFailoverData();
                foreach (var kvp in newData)
                {
                    var oldData = _failoverDataCache.AddOrUpdate(kvp.Key, kvp.Value, (_, _) => kvp.Value);
                    if (oldData.Data != kvp.Value.Data)
                    {
                        OnDataChanged(new FailoverDataChangedEventArgs<T>(kvp.Key, oldData, kvp.Value));
                    }
                }

                // 移除不再存在的数据
                var keysToRemove = _failoverDataCache.Keys.Except(newData.Keys).ToList();
                foreach (var key in keysToRemove)
                {
                    if (_failoverDataCache.TryRemove(key, out var removed))
                    {
                        OnDataChanged(new FailoverDataChangedEventArgs<T>(key, removed, null));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing failover data from data source");
        }
    }

    /// <summary>
    /// 触发数据变更事件
    /// </summary>
    protected virtual void OnDataChanged(FailoverDataChangedEventArgs<T> e)
    {
        DataChanged?.Invoke(this, e);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 故障转移数据变更事件参数
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class FailoverDataChangedEventArgs<T> : EventArgs where T : class
{
    /// <summary>
    /// 数据键
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// 旧数据
    /// </summary>
    public FailoverData<T>? OldData { get; }

    /// <summary>
    /// 新数据
    /// </summary>
    public FailoverData<T>? NewData { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public FailoverDataChangedEventArgs(string key, FailoverData<T>? oldData, FailoverData<T>? newData)
    {
        Key = key;
        OldData = oldData;
        NewData = newData;
    }
}
