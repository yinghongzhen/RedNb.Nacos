using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RedNb.Nacos.Redo;

/// <summary>
/// Redo 服务抽象基类
/// </summary>
public abstract class AbstractRedoService : IRedoService, IDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> _redoDataMap = new();
    private readonly object _lockObj = new();
    private CancellationTokenSource? _cts;
    private Task? _redoTask;

    /// <summary>
    /// Redo 任务执行间隔（毫秒）
    /// </summary>
    protected virtual int RedoDelayMs => 3000;

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    protected AbstractRedoService(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void RegisterRedoData<T>(RedoData<T> redoData) where T : class
    {
        var typeMap = _redoDataMap.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<string, object>());
        typeMap[redoData.RedoKey] = redoData;
        _logger.LogDebug("Registered redo data: Type={Type}, Key={Key}", typeof(T).Name, redoData.RedoKey);
    }

    /// <inheritdoc />
    public IEnumerable<RedoData<T>> GetRedoData<T>() where T : class
    {
        if (_redoDataMap.TryGetValue(typeof(T), out var typeMap))
        {
            return typeMap.Values.Cast<RedoData<T>>().ToList();
        }
        return Enumerable.Empty<RedoData<T>>();
    }

    /// <inheritdoc />
    public void RemoveRedoData<T>(string key) where T : class
    {
        if (_redoDataMap.TryGetValue(typeof(T), out var typeMap))
        {
            typeMap.TryRemove(key, out _);
            _logger.LogDebug("Removed redo data: Type={Type}, Key={Key}", typeof(T).Name, key);
        }
    }

    /// <inheritdoc />
    public void OnConnected()
    {
        lock (_lockObj)
        {
            IsConnected = true;
            _logger.LogInformation("Connection established, marking all redo data as unregistered");

            // 连接建立后，将所有已注册的数据标记为未注册，以便重新注册
            foreach (var typeMap in _redoDataMap.Values)
            {
                foreach (var redoData in typeMap.Values)
                {
                    var method = redoData.GetType().GetMethod("SetUnregistered");
                    method?.Invoke(redoData, null);
                }
            }
        }
    }

    /// <inheritdoc />
    public void OnDisconnected()
    {
        lock (_lockObj)
        {
            IsConnected = false;
            _logger.LogWarning("Connection lost, redo service will retry operations when reconnected");
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _redoTask = Task.Run(() => RunRedoLoopAsync(_cts.Token), _cts.Token);
        _logger.LogInformation("Redo service started");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();
        if (_redoTask != null)
        {
            try
            {
                await _redoTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 预期的取消
            }
        }
        _logger.LogInformation("Redo service stopped");
    }

    /// <summary>
    /// Redo 循环
    /// </summary>
    private async Task RunRedoLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(RedoDelayMs, cancellationToken);

                if (!IsConnected)
                {
                    continue;
                }

                await ExecuteRedoAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in redo loop");
            }
        }
    }

    /// <summary>
    /// 执行 redo 操作
    /// </summary>
    protected abstract Task ExecuteRedoAsync(CancellationToken cancellationToken);

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
