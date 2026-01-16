namespace RedNb.Nacos.Monitor;

/// <summary>
/// Counter 指标（累积计数器）
/// </summary>
public class CounterMetric
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
    public CounterMetric(string name, string description, IDictionary<string, string>? labels = null)
    {
        Name = name;
        Description = description;
        Labels = labels?.AsReadOnly();
    }

    /// <summary>
    /// 增加计数
    /// </summary>
    public void Increase(double value = 1)
    {
        if (value < 0)
        {
            throw new ArgumentException("Counter value cannot be negative", nameof(value));
        }

        lock (_lockObj) _value += value;
    }

    /// <summary>
    /// 重置计数器
    /// </summary>
    public void Reset()
    {
        lock (_lockObj) _value = 0;
    }

    /// <summary>
    /// 获取快照
    /// </summary>
    public CounterSnapshot GetSnapshot()
    {
        return new CounterSnapshot
        {
            Name = Name,
            Description = Description,
            Value = Value,
            Labels = Labels
        };
    }
}

/// <summary>
/// Counter 快照
/// </summary>
public class CounterSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Value { get; set; }
    public IReadOnlyDictionary<string, string>? Labels { get; set; }
}
