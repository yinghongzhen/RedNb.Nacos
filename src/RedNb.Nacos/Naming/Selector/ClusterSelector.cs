namespace RedNb.Nacos.Core.Naming.Selector;

/// <summary>
/// A selector that filters instances based on cluster name.
/// </summary>
public class ClusterSelector : INamingSelector
{
    private readonly HashSet<string> _clusters;

    /// <summary>
    /// Gets the type of this selector.
    /// </summary>
    public string Type => "cluster";

    /// <summary>
    /// Gets the expression for this selector.
    /// </summary>
    public string Expression { get; }

    /// <summary>
    /// Creates a new ClusterSelector with the specified clusters.
    /// </summary>
    /// <param name="clusters">Cluster names to match.</param>
    public ClusterSelector(IEnumerable<string> clusters)
    {
        _clusters = new HashSet<string>(clusters ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        Expression = string.Join(",", _clusters);
    }

    /// <summary>
    /// Creates a new ClusterSelector from an expression.
    /// Expression format: "cluster1,cluster2,cluster3"
    /// </summary>
    /// <param name="expression">Comma-separated cluster names.</param>
    public ClusterSelector(string expression)
    {
        Expression = expression ?? string.Empty;
        _clusters = new HashSet<string>(
            (expression ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public NamingResult Select(NamingContext context)
    {
        if (_clusters.Count == 0)
        {
            return NamingResult.Of(context.Instances);
        }

        var filtered = context.Instances
            .Where(instance => _clusters.Contains(instance.ClusterName ?? NacosConstants.DefaultClusterName))
            .ToList();

        return NamingResult.Of(filtered);
    }

    /// <summary>
    /// Creates a ClusterSelector for the specified clusters.
    /// </summary>
    public static ClusterSelector Of(params string[] clusters) => new(clusters);
}
