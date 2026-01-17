using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RedNb.Nacos.Redo;

/// <summary>
/// 抽象 Redo 服务基类
/// 参考 Java SDK: com.alibaba.nacos.client.redo.service.AbstractRedoService
/// </summary>
public abstract class AbstractRedoService : IDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> _redoDataMap = new();
    private readonly object _lockObj = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _redoTask;

    /// <summary>
    /// Redo 任务执行间隔（毫秒）
    /// </summary>
    protected long RedoDelayTime { get; set; } = 3000;

    /// <summary>
    /// Redo 线程数量
    /// </summary>
    protected int RedoThreadCount { get; set; } = 1;

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

    /// <summary>
    /// 启动 Redo 任务
    /// </summary>
    protected void StartRedoTask()
    {
        _redoTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay((int)RedoDelayTime, _cts.Token);

                    if (!IsConnected)
                    {
                        continue;
                    }

                    await ExecuteRedoTaskAsync(_cts.Token);
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
        }, _cts.Token);
    }

    /// <summary>
    /// 执行 Redo 任务（子类实现）
    /// </summary>
    protected abstract Task ExecuteRedoTaskAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 连接成功事件
    /// </summary>
    public virtual void OnConnected()
    {
        IsConnected = true;
        _logger.LogInformation("Grpc connection connect");
    }

    /// <summary>
    /// 连接断开事件
    /// </summary>
    public virtual void OnDisconnect()
    {
        IsConnected = false;
        _logger.LogWarning("Grpc connection disconnect, mark to redo");

        foreach (var typeKey in _redoDataMap.Keys)
        {
            if (_redoDataMap.TryGetValue(typeKey, out var actualRedoData))
            {
                lock (actualRedoData)
                {
                    foreach (var redoData in actualRedoData.Values)
                    {
                        // 使用反射设置 Registered = false
                        var prop = redoData.GetType().GetProperty("Registered");
                        prop?.SetValue(redoData, false);
                    }
                }
            }
        }

        _logger.LogWarning("mark to redo completed");
    }

    /// <summary>
    /// 关闭服务
    /// </summary>
    public virtual void Shutdown()
    {
        _logger.LogInformation("Shutdown grpc redo service executor");
        _redoDataMap.Clear();
        _cts.Cancel();
    }

    #region Redo Data Management

    /// <summary>
    /// 缓存 Redo 数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    /// <param name="redoData">Redo 数据</param>
    public void CachedRedoData<T>(string key, RedoData<T> redoData)
    {
        var actualRedoData = _redoDataMap.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<string, object>());
        lock (actualRedoData)
        {
            actualRedoData[key] = redoData;
        }
    }

    /// <summary>
    /// 移除 Redo 数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    public void RemoveRedoData<T>(string key)
    {
        var actualRedoData = _redoDataMap.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<string, object>());
        lock (actualRedoData)
        {
            if (actualRedoData.TryGetValue(key, out var obj) && obj is RedoData<T> redoData)
            {
                if (!redoData.ExpectedRegistered)
                {
                    actualRedoData.TryRemove(key, out _);
                }
            }
        }
    }

    /// <summary>
    /// 数据注册成功，标记为已注册
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    public void DataRegistered<T>(string key)
    {
        var actualRedoData = _redoDataMap.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<string, object>());
        lock (actualRedoData)
        {
            if (actualRedoData.TryGetValue(key, out var obj) && obj is RedoData<T> redoData)
            {
                redoData.SetRegistered();
            }
        }
    }

    /// <summary>
    /// 数据注销，标记为正在注销
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    public void DataDeregister<T>(string key)
    {
        var actualRedoData = _redoDataMap.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<string, object>());
        lock (actualRedoData)
        {
            if (actualRedoData.TryGetValue(key, out var obj) && obj is RedoData<T> redoData)
            {
                redoData.Unregistering = true;
                redoData.ExpectedRegistered = false;
            }
        }
    }

    /// <summary>
    /// 数据注销完成
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    public void DataDeregistered<T>(string key)
    {
        var actualRedoData = _redoDataMap.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<string, object>());
        lock (actualRedoData)
        {
            if (actualRedoData.TryGetValue(key, out var obj) && obj is RedoData<T> redoData)
            {
                redoData.SetUnregistered();
            }
        }
    }

    /// <summary>
    /// 判断数据是否已注册
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    /// <returns>是否已注册</returns>
    public bool IsDataRegistered<T>(string key)
    {
        var actualRedoData = _redoDataMap.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<string, object>());
        lock (actualRedoData)
        {
            if (actualRedoData.TryGetValue(key, out var obj) && obj is RedoData<T> redoData)
            {
                return redoData.Registered;
            }
        }
        return false;
    }

    /// <summary>
    /// 查找需要 Redo 的数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <returns>需要 Redo 的数据集合</returns>
    public HashSet<RedoData<T>> FindRedoData<T>()
    {
        var result = new HashSet<RedoData<T>>();
        var actualRedoData = _redoDataMap.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<string, object>());
        lock (actualRedoData)
        {
            foreach (var obj in actualRedoData.Values)
            {
                if (obj is RedoData<T> redoData && redoData.IsNeedRedo())
                {
                    result.Add(redoData);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 获取 Redo 数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    /// <returns>Redo 数据</returns>
    public RedoData<T>? GetRedoData<T>(string key)
    {
        var actualRedoData = _redoDataMap.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<string, object>());
        lock (actualRedoData)
        {
            if (actualRedoData.TryGetValue(key, out var obj) && obj is RedoData<T> redoData)
            {
                return redoData;
            }
        }
        return null;
    }

    #endregion

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Shutdown();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
