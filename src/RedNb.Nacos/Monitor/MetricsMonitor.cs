using System.Collections.Concurrent;
using System.Diagnostics;

namespace RedNb.Nacos.Monitor;

/// <summary>
/// 指标监控器
/// 提供 Nacos 客户端的指标收集和导出功能
/// </summary>
public class MetricsMonitor
{
    private static readonly Lazy<MetricsMonitor> _instance = new(() => new MetricsMonitor());

    /// <summary>
    /// 默认实例
    /// </summary>
    public static MetricsMonitor Default => _instance.Value;

    // Gauge 指标存储
    private readonly ConcurrentDictionary<string, GaugeMetric> _gauges = new();

    // Counter 指标存储
    private readonly ConcurrentDictionary<string, CounterMetric> _counters = new();

    // Histogram 指标存储
    private readonly ConcurrentDictionary<string, HistogramMetric> _histograms = new();

    /// <summary>
    /// 是否启用指标收集
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 私有构造函数
    /// </summary>
    private MetricsMonitor()
    {
        InitializeMetrics();
    }

    /// <summary>
    /// 初始化默认指标
    /// </summary>
    private void InitializeMetrics()
    {
        // 初始化 Gauge 指标
        RegisterGauge(MetricNames.ServiceInfoMapSize, "Number of cached service information");
        RegisterGauge(MetricNames.ListenConfigCount, "Number of listened configs");
        RegisterGauge(MetricNames.ConnectionStatus, "Connection status (1=connected, 0=disconnected)");
        RegisterGauge(MetricNames.FailoverEnabled, "Failover enabled status (1=enabled, 0=disabled)");

        // 初始化 Counter 指标
        RegisterCounter(MetricNames.ConfigRequestSuccessTotal, "Total successful config requests");
        RegisterCounter(MetricNames.ConfigRequestFailedTotal, "Total failed config requests");
        RegisterCounter(MetricNames.NamingRequestSuccessTotal, "Total successful naming requests");
        RegisterCounter(MetricNames.NamingRequestFailedTotal, "Total failed naming requests");
        RegisterCounter(MetricNames.ServiceChangePushTotal, "Total service change pushes");
        RegisterCounter(MetricNames.ConfigChangePushTotal, "Total config change pushes");
        RegisterCounter(MetricNames.RedoOperationTotal, "Total redo operations");
        RegisterCounter(MetricNames.FailoverUsedTotal, "Total failover uses");

        // 初始化 Histogram 指标
        RegisterHistogram(MetricNames.RequestLatency, "Request latency in milliseconds",
            new[] { 5.0, 10.0, 25.0, 50.0, 100.0, 250.0, 500.0, 1000.0, 2500.0, 5000.0 });
        RegisterHistogram(MetricNames.ConfigRequestLatency, "Config request latency in milliseconds",
            new[] { 5.0, 10.0, 25.0, 50.0, 100.0, 250.0, 500.0, 1000.0, 2500.0, 5000.0 });
        RegisterHistogram(MetricNames.NamingRequestLatency, "Naming request latency in milliseconds",
            new[] { 5.0, 10.0, 25.0, 50.0, 100.0, 250.0, 500.0, 1000.0, 2500.0, 5000.0 });
    }

    #region Gauge 操作

    /// <summary>
    /// 注册 Gauge 指标
    /// </summary>
    public GaugeMetric RegisterGauge(string name, string description, IDictionary<string, string>? labels = null)
    {
        return _gauges.GetOrAdd(name, _ => new GaugeMetric(name, description, labels));
    }

    /// <summary>
    /// 设置 Gauge 值
    /// </summary>
    public void SetGauge(string name, double value)
    {
        if (!Enabled) return;

        if (_gauges.TryGetValue(name, out var gauge))
        {
            gauge.Set(value);
        }
    }

    /// <summary>
    /// 增加 Gauge 值
    /// </summary>
    public void IncreaseGauge(string name, double value = 1)
    {
        if (!Enabled) return;

        if (_gauges.TryGetValue(name, out var gauge))
        {
            gauge.Increase(value);
        }
    }

    /// <summary>
    /// 减少 Gauge 值
    /// </summary>
    public void DecreaseGauge(string name, double value = 1)
    {
        if (!Enabled) return;

        if (_gauges.TryGetValue(name, out var gauge))
        {
            gauge.Decrease(value);
        }
    }

    /// <summary>
    /// 获取 Gauge 值
    /// </summary>
    public double GetGaugeValue(string name)
    {
        return _gauges.TryGetValue(name, out var gauge) ? gauge.Value : 0;
    }

    #endregion

    #region Counter 操作

    /// <summary>
    /// 注册 Counter 指标
    /// </summary>
    public CounterMetric RegisterCounter(string name, string description, IDictionary<string, string>? labels = null)
    {
        return _counters.GetOrAdd(name, _ => new CounterMetric(name, description, labels));
    }

    /// <summary>
    /// 增加 Counter 值
    /// </summary>
    public void IncreaseCounter(string name, double value = 1)
    {
        if (!Enabled) return;

        if (_counters.TryGetValue(name, out var counter))
        {
            counter.Increase(value);
        }
    }

    /// <summary>
    /// 获取 Counter 值
    /// </summary>
    public double GetCounterValue(string name)
    {
        return _counters.TryGetValue(name, out var counter) ? counter.Value : 0;
    }

    #endregion

    #region Histogram 操作

    /// <summary>
    /// 注册 Histogram 指标
    /// </summary>
    public HistogramMetric RegisterHistogram(string name, string description, double[] buckets, IDictionary<string, string>? labels = null)
    {
        return _histograms.GetOrAdd(name, _ => new HistogramMetric(name, description, buckets, labels));
    }

    /// <summary>
    /// 观察 Histogram 值
    /// </summary>
    public void ObserveHistogram(string name, double value)
    {
        if (!Enabled) return;

        if (_histograms.TryGetValue(name, out var histogram))
        {
            histogram.Observe(value);
        }
    }

    /// <summary>
    /// 开始计时并返回用于记录的 Timer
    /// </summary>
    public IDisposable StartTimer(string histogramName)
    {
        return new HistogramTimer(this, histogramName);
    }

    #endregion

    #region 便捷方法

    /// <summary>
    /// 记录配置请求成功
    /// </summary>
    public void RecordConfigRequestSuccess()
    {
        IncreaseCounter(MetricNames.ConfigRequestSuccessTotal);
    }

    /// <summary>
    /// 记录配置请求失败
    /// </summary>
    public void RecordConfigRequestFailed()
    {
        IncreaseCounter(MetricNames.ConfigRequestFailedTotal);
    }

    /// <summary>
    /// 记录命名请求成功
    /// </summary>
    public void RecordNamingRequestSuccess()
    {
        IncreaseCounter(MetricNames.NamingRequestSuccessTotal);
    }

    /// <summary>
    /// 记录命名请求失败
    /// </summary>
    public void RecordNamingRequestFailed()
    {
        IncreaseCounter(MetricNames.NamingRequestFailedTotal);
    }

    /// <summary>
    /// 记录服务变更推送
    /// </summary>
    public void RecordServiceChangePush()
    {
        IncreaseCounter(MetricNames.ServiceChangePushTotal);
    }

    /// <summary>
    /// 记录配置变更推送
    /// </summary>
    public void RecordConfigChangePush()
    {
        IncreaseCounter(MetricNames.ConfigChangePushTotal);
    }

    /// <summary>
    /// 记录重做操作
    /// </summary>
    public void RecordRedoOperation()
    {
        IncreaseCounter(MetricNames.RedoOperationTotal);
    }

    /// <summary>
    /// 记录故障转移使用
    /// </summary>
    public void RecordFailoverUsed()
    {
        IncreaseCounter(MetricNames.FailoverUsedTotal);
    }

    /// <summary>
    /// 设置连接状态
    /// </summary>
    public void SetConnectionStatus(bool connected)
    {
        SetGauge(MetricNames.ConnectionStatus, connected ? 1 : 0);
    }

    /// <summary>
    /// 设置故障转移状态
    /// </summary>
    public void SetFailoverEnabled(bool enabled)
    {
        SetGauge(MetricNames.FailoverEnabled, enabled ? 1 : 0);
    }

    /// <summary>
    /// 设置服务信息缓存数量
    /// </summary>
    public void SetServiceInfoMapSize(int size)
    {
        SetGauge(MetricNames.ServiceInfoMapSize, size);
    }

    /// <summary>
    /// 设置监听配置数量
    /// </summary>
    public void SetListenConfigCount(int count)
    {
        SetGauge(MetricNames.ListenConfigCount, count);
    }

    #endregion

    #region 导出

    /// <summary>
    /// 获取所有指标快照
    /// </summary>
    public MetricsSnapshot GetSnapshot()
    {
        return new MetricsSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Gauges = _gauges.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetSnapshot()),
            Counters = _counters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetSnapshot()),
            Histograms = _histograms.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetSnapshot())
        };
    }

    /// <summary>
    /// 重置所有指标
    /// </summary>
    public void Reset()
    {
        foreach (var gauge in _gauges.Values) gauge.Set(0);
        foreach (var counter in _counters.Values) counter.Reset();
        foreach (var histogram in _histograms.Values) histogram.Reset();
    }

    #endregion

    /// <summary>
    /// Histogram 计时器
    /// </summary>
    private class HistogramTimer : IDisposable
    {
        private readonly MetricsMonitor _monitor;
        private readonly string _histogramName;
        private readonly Stopwatch _stopwatch;

        public HistogramTimer(MetricsMonitor monitor, string histogramName)
        {
            _monitor = monitor;
            _histogramName = histogramName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _monitor.ObserveHistogram(_histogramName, _stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
