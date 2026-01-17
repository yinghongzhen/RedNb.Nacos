namespace RedNb.Nacos.Monitor;

/// <summary>
/// Gauge 指标（当前值指标）
/// </summary>
public class GaugeMetric
{
    private double _value;
    private readonly object _lockObj = new();

    /// <summary>
    /// 指标名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 指标描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 标签
    /// </summary>
    public IReadOnlyDictionary<string, string>? Labels { get; }

    /// <summary>
    /// 当前值
    /// </summary>
    public double Value
    {
        get
        {
            lock (_lockObj) return _value;
        }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public GaugeMetric(string name, string description, IDictionary<string, string>? labels = null)
    {
        Name = name;
        Description = description;
        Labels = labels?.AsReadOnly();
    }

    /// <summary>
    /// 设置值
    /// </summary>
    public void Set(double value)
    {
        lock (_lockObj) _value = value;
    }

    /// <summary>
    /// 增加值
    /// </summary>
    public void Increase(double value = 1)
    {
        lock (_lockObj) _value += value;
    }

    /// <summary>
    /// 减少值
    /// </summary>
    public void Decrease(double value = 1)
    {
        lock (_lockObj) _value -= value;
    }

    /// <summary>
    /// 获取快照
    /// </summary>
    public GaugeSnapshot GetSnapshot()
    {
        return new GaugeSnapshot
        {
            Name = Name,
            Description = Description,
            Value = Value,
            Labels = Labels
        };
    }
}

/// <summary>
/// Gauge 快照
/// </summary>
public class GaugeSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Value { get; set; }
    public IReadOnlyDictionary<string, string>? Labels { get; set; }
}

/// <summary>
/// 扩展方法
/// </summary>
internal static class DictionaryExtensions
{
    public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        return new Dictionary<TKey, TValue>(dictionary);
    }
}
