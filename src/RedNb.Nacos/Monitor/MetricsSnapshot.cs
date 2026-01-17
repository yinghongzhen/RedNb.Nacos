namespace RedNb.Nacos.Monitor;

/// <summary>
/// 指标快照
/// </summary>
public class MetricsSnapshot
{
    /// <summary>
    /// 快照时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gauge 指标
    /// </summary>
    public Dictionary<string, GaugeSnapshot> Gauges { get; set; } = new();

    /// <summary>
    /// Counter 指标
    /// </summary>
    public Dictionary<string, CounterSnapshot> Counters { get; set; } = new();

    /// <summary>
    /// Histogram 指标
    /// </summary>
    public Dictionary<string, HistogramSnapshot> Histograms { get; set; } = new();

    /// <summary>
    /// 导出为 Prometheus 文本格式
    /// </summary>
    public string ToPrometheusFormat()
    {
        var lines = new List<string>();

        // 导出 Gauge
        foreach (var gauge in Gauges.Values)
        {
            lines.Add($"# HELP {gauge.Name} {gauge.Description}");
            lines.Add($"# TYPE {gauge.Name} gauge");
            lines.Add($"{gauge.Name}{FormatLabels(gauge.Labels)} {gauge.Value}");
        }

        // 导出 Counter
        foreach (var counter in Counters.Values)
        {
            lines.Add($"# HELP {counter.Name} {counter.Description}");
            lines.Add($"# TYPE {counter.Name} counter");
            lines.Add($"{counter.Name}{FormatLabels(counter.Labels)} {counter.Value}");
        }

        // 导出 Histogram
        foreach (var histogram in Histograms.Values)
        {
            lines.Add($"# HELP {histogram.Name} {histogram.Description}");
            lines.Add($"# TYPE {histogram.Name} histogram");

            var cumulativeCount = 0L;
            for (var i = 0; i < histogram.Buckets.Length; i++)
            {
                cumulativeCount += histogram.BucketCounts[i];
                var bucketLabels = AddLabel(histogram.Labels, "le", histogram.Buckets[i].ToString());
                lines.Add($"{histogram.Name}_bucket{FormatLabels(bucketLabels)} {cumulativeCount}");
            }

            // +Inf bucket
            cumulativeCount += histogram.BucketCounts[histogram.Buckets.Length];
            var infLabels = AddLabel(histogram.Labels, "le", "+Inf");
            lines.Add($"{histogram.Name}_bucket{FormatLabels(infLabels)} {cumulativeCount}");

            lines.Add($"{histogram.Name}_sum{FormatLabels(histogram.Labels)} {histogram.Sum}");
            lines.Add($"{histogram.Name}_count{FormatLabels(histogram.Labels)} {histogram.Count}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 格式化标签
    /// </summary>
    private static string FormatLabels(IReadOnlyDictionary<string, string>? labels)
    {
        if (labels == null || labels.Count == 0)
        {
            return string.Empty;
        }

        var labelPairs = labels.Select(kvp => $"{kvp.Key}=\"{EscapeLabelValue(kvp.Value)}\"");
        return "{" + string.Join(",", labelPairs) + "}";
    }

    /// <summary>
    /// 添加标签
    /// </summary>
    private static Dictionary<string, string> AddLabel(IReadOnlyDictionary<string, string>? labels, string key, string value)
    {
        var result = labels != null ? new Dictionary<string, string>(labels) : new Dictionary<string, string>();
        result[key] = value;
        return result;
    }

    /// <summary>
    /// 转义标签值
    /// </summary>
    private static string EscapeLabelValue(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
