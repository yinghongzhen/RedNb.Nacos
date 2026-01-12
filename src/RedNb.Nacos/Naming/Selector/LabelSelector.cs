namespace RedNb.Nacos.Core.Naming.Selector;

/// <summary>
/// A selector that filters instances based on metadata labels.
/// </summary>
public class LabelSelector : INamingSelector
{
    private readonly Dictionary<string, string> _labels;

    /// <summary>
    /// Gets the type of this selector.
    /// </summary>
    public string Type => "label";

    /// <summary>
    /// Gets the expression for this selector.
    /// </summary>
    public string Expression { get; }

    /// <summary>
    /// Creates a new LabelSelector with the specified labels.
    /// </summary>
    /// <param name="labels">Labels to match against instance metadata.</param>
    public LabelSelector(Dictionary<string, string> labels)
    {
        _labels = labels ?? new Dictionary<string, string>();
        Expression = string.Join(",", _labels.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    /// <summary>
    /// Creates a new LabelSelector with the specified expression.
    /// Expression format: "key1=value1,key2=value2"
    /// </summary>
    /// <param name="expression">Label expression string.</param>
    public LabelSelector(string expression)
    {
        Expression = expression ?? string.Empty;
        _labels = ParseExpression(expression);
    }

    /// <inheritdoc />
    public NamingResult Select(NamingContext context)
    {
        if (_labels.Count == 0)
        {
            return NamingResult.Of(context.Instances);
        }

        var filtered = context.Instances.Where(instance =>
        {
            foreach (var label in _labels)
            {
                if (!instance.Metadata.TryGetValue(label.Key, out var value) || value != label.Value)
                {
                    return false;
                }
            }
            return true;
        }).ToList();

        return NamingResult.Of(filtered);
    }

    private static Dictionary<string, string> ParseExpression(string? expression)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(expression))
        {
            return result;
        }

        var pairs = expression.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                result[keyValue[0].Trim()] = keyValue[1].Trim();
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a LabelSelector builder.
    /// </summary>
    public static LabelSelectorBuilder Builder() => new();
}

/// <summary>
/// Builder for creating LabelSelector instances.
/// </summary>
public class LabelSelectorBuilder
{
    private readonly Dictionary<string, string> _labels = new();

    /// <summary>
    /// Adds a label to match.
    /// </summary>
    public LabelSelectorBuilder AddLabel(string key, string value)
    {
        _labels[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple labels to match.
    /// </summary>
    public LabelSelectorBuilder AddLabels(Dictionary<string, string> labels)
    {
        foreach (var label in labels)
        {
            _labels[label.Key] = label.Value;
        }
        return this;
    }

    /// <summary>
    /// Builds the LabelSelector.
    /// </summary>
    public LabelSelector Build() => new(_labels);
}
