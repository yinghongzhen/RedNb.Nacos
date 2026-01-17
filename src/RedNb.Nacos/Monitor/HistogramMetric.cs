using System.Collections.Concurrent;

namespace RedNb.Nacos.Monitor;

/// <summary>
/// Histogram 指标（分布直方图）
/// </summary>
public class HistogramMetric
{
    private readonly object _lockObj = new();
    private readonly double[] _buckets;
    private readonly long[] _bucketCounts;
    private double _sum;
    private long _count;

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
    /// 桶边界
    /// </summary>
    public IReadOnlyList<double> Buckets => _buckets;

    /// <summary>
    /// 总和
    /// </summary>
    public double Sum
    {
        get
        {
            lock (_lockObj) return _sum;
        }
    }

    /// <summary>
    /// 计数
    /// </summary>
    public long Count
    {
        get
        {
            lock (_lockObj) return _count;
        }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public HistogramMetric(string name, string description, double[] buckets, IDictionary<string, string>? labels = null)
    {
        Name = name;
        Description = description;
        Labels = labels?.AsReadOnly();

        // 确保桶边界有序
        _buckets = buckets.OrderBy(b => b).ToArray();
        _bucketCounts = new long[_buckets.Length + 1]; // +1 用于 +Inf 桶
    }

    /// <summary>
    /// 观察一个值
    /// </summary>
    public void Observe(double value)
    {
        lock (_lockObj)
        {
            _sum += value;
            _count++;

            // 找到对应的桶并增加计数
            for (var i = 0; i < _buckets.Length; i++)
            {
                if (value <= _buckets[i])
                {
                    _bucketCounts[i]++;
                    return;
                }
            }

            // 大于所有桶边界，放入 +Inf 桶
            _bucketCounts[_buckets.Length]++;
        }
    }

    /// <summary>
    /// 获取桶计数
    /// </summary>
    public long[] GetBucketCounts()
    {
        lock (_lockObj)
        {
            return (long[])_bucketCounts.Clone();
        }
    }

    /// <summary>
    /// 获取平均值
    /// </summary>
    public double GetMean()
    {
        lock (_lockObj)
        {
            return _count == 0 ? 0 : _sum / _count;
        }
    }

    /// <summary>
    /// 重置直方图
    /// </summary>
    public void Reset()
    {
        lock (_lockObj)
        {
            _sum = 0;
            _count = 0;
            Array.Clear(_bucketCounts, 0, _bucketCounts.Length);
        }
    }

    /// <summary>
    /// 获取快照
    /// </summary>
    public HistogramSnapshot GetSnapshot()
    {
        lock (_lockObj)
        {
            return new HistogramSnapshot
            {
                Name = Name,
                Description = Description,
                Buckets = _buckets.ToArray(),
                BucketCounts = (long[])_bucketCounts.Clone(),
                Sum = _sum,
                Count = _count,
                Mean = _count == 0 ? 0 : _sum / _count,
                Labels = Labels
            };
        }
    }
}

/// <summary>
/// Histogram 快照
/// </summary>
public class HistogramSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double[] Buckets { get; set; } = Array.Empty<double>();
    public long[] BucketCounts { get; set; } = Array.Empty<long>();
    public double Sum { get; set; }
    public long Count { get; set; }
    public double Mean { get; set; }
    public IReadOnlyDictionary<string, string>? Labels { get; set; }
}
