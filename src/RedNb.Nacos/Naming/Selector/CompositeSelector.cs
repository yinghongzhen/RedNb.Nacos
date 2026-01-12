namespace RedNb.Nacos.Core.Naming.Selector;

/// <summary>
/// A selector that combines multiple selectors with AND logic.
/// </summary>
public class CompositeSelector : INamingSelector
{
    private readonly List<INamingSelector> _selectors;

    /// <summary>
    /// Gets the type of this selector.
    /// </summary>
    public string Type => "composite";

    /// <summary>
    /// Gets the expression for this selector.
    /// </summary>
    public string Expression => string.Join(" AND ", _selectors.Select(s => $"({s.Type}:{s.Expression})"));

    /// <summary>
    /// Creates a new CompositeSelector with the specified selectors.
    /// </summary>
    /// <param name="selectors">Selectors to combine.</param>
    public CompositeSelector(params INamingSelector[] selectors)
    {
        _selectors = new List<INamingSelector>(selectors ?? Array.Empty<INamingSelector>());
    }

    /// <summary>
    /// Creates a new CompositeSelector with the specified selectors.
    /// </summary>
    /// <param name="selectors">Selectors to combine.</param>
    public CompositeSelector(IEnumerable<INamingSelector> selectors)
    {
        _selectors = new List<INamingSelector>(selectors ?? Enumerable.Empty<INamingSelector>());
    }

    /// <inheritdoc />
    public NamingResult Select(NamingContext context)
    {
        if (_selectors.Count == 0)
        {
            return NamingResult.Of(context.Instances);
        }

        var currentInstances = context.Instances;

        foreach (var selector in _selectors)
        {
            var selectorContext = new NamingContext
            {
                ServiceName = context.ServiceName,
                GroupName = context.GroupName,
                Clusters = context.Clusters,
                ServiceInfo = context.ServiceInfo,
                Instances = currentInstances,
                HealthyOnly = context.HealthyOnly
            };

            var result = selector.Select(selectorContext);
            currentInstances = result.Instances;

            if (currentInstances.Count == 0)
            {
                break;
            }
        }

        return NamingResult.Of(currentInstances);
    }

    /// <summary>
    /// Adds a selector to the composite.
    /// </summary>
    public CompositeSelector And(INamingSelector selector)
    {
        _selectors.Add(selector);
        return this;
    }

    /// <summary>
    /// Creates a new CompositeSelector builder.
    /// </summary>
    public static CompositeSelectorBuilder Builder() => new();
}

/// <summary>
/// Builder for CompositeSelector.
/// </summary>
public class CompositeSelectorBuilder
{
    private readonly List<INamingSelector> _selectors = new();

    /// <summary>
    /// Adds a selector to the composite.
    /// </summary>
    public CompositeSelectorBuilder Add(INamingSelector selector)
    {
        _selectors.Add(selector);
        return this;
    }

    /// <summary>
    /// Adds a label selector.
    /// </summary>
    public CompositeSelectorBuilder WithLabels(Dictionary<string, string> labels)
    {
        _selectors.Add(new LabelSelector(labels));
        return this;
    }

    /// <summary>
    /// Adds a cluster selector.
    /// </summary>
    public CompositeSelectorBuilder WithClusters(params string[] clusters)
    {
        _selectors.Add(ClusterSelector.Of(clusters));
        return this;
    }

    /// <summary>
    /// Builds the CompositeSelector.
    /// </summary>
    public CompositeSelector Build() => new(_selectors);
}
