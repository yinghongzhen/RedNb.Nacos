using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RedNb.Nacos.Failover;

/// <summary>
/// ����ת�Ʒ�Ӧ��
/// ������ˢ�¹���ת�����ݲ�����Ҫʱ�ṩ���ػ���
/// </summary>
/// <typeparam name="T">��������</typeparam>
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
    /// ������ת�����ݱ��ʱ����
    /// </summary>
    public event EventHandler<FailoverDataChangedEventArgs<T>>? DataChanged;

    /// <summary>
    /// ���캯��
    /// </summary>
    /// <param name="logger">��־��¼��</param>
    /// <param name="dataSource">����Դ����Ϊnull���ʹ���ֶ����õ�����</param>
    /// <param name="refreshIntervalMs">ˢ�¼�������룩</param>
    public FailoverReactor(ILogger logger, IFailoverDataSource<T>? dataSource = null, int refreshIntervalMs = 5000)
    {
        _logger = logger;
        _dataSource = dataSource;
        _refreshIntervalMs = refreshIntervalMs;
    }

    /// <summary>
    /// ��ȡ����ת���Ƿ�����
    /// </summary>
    public bool IsFailoverEnabled => _failoverSwitch.Enabled;

    /// <summary>
    /// ��������ת�Ʒ�Ӧ��
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
    /// ֹͣ����ת�Ʒ�Ӧ��
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
                // Ԥ�ڵ�ȡ��
            }
        }
        _logger.LogInformation("Failover reactor stopped");
    }

    /// <summary>
    /// ��ȡ����ת������
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
    /// ���Ի�ȡ����ת������
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
        FailoverData<T>? oldData = null;
        _failoverDataCache.TryGetValue(key, out oldData);

        _failoverDataCache.AddOrUpdate(key, data, (_, _) => data);
        _logger.LogDebug("Set failover data for key: {Key}", key);

        // 始终触发事件，无论是新增还是更新
        OnDataChanged(new FailoverDataChangedEventArgs<T>(key, oldData, data));
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
    /// ���ù���ת�ƿ���
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
    /// ˢ��ѭ��
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
    /// ˢ�¹���ת������
    /// </summary>
    private void RefreshFailoverData()
    {
        if (_dataSource == null)
        {
            return;
        }

        try
        {
            // ˢ�¿���״̬
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

            // ˢ�¹���ת������
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

                // �Ƴ����ٴ��ڵ�����
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
    /// �������ݱ���¼�
    /// </summary>
    protected virtual void OnDataChanged(FailoverDataChangedEventArgs<T> e)
    {
        DataChanged?.Invoke(this, e);
    }

    /// <summary>
    /// �ͷ���Դ
    /// </summary>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// ����ת�����ݱ���¼�����
/// </summary>
/// <typeparam name="T">��������</typeparam>
public class FailoverDataChangedEventArgs<T> : EventArgs where T : class
{
    /// <summary>
    /// ���ݼ�
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// ������
    /// </summary>
    public FailoverData<T>? OldData { get; }

    /// <summary>
    /// ������
    /// </summary>
    public FailoverData<T>? NewData { get; }

    /// <summary>
    /// ���캯��
    /// </summary>
    public FailoverDataChangedEventArgs(string key, FailoverData<T>? oldData, FailoverData<T>? newData)
    {
        Key = key;
        OldData = oldData;
        NewData = newData;
    }
}
