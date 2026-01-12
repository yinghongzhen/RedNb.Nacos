namespace RedNb.Nacos.Core.Naming.Selector;

/// <summary>
/// Interface for custom instance selection logic.
/// Used to filter instances based on custom criteria.
/// </summary>
public interface INamingSelector
{
    /// <summary>
    /// Gets the type of this selector.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Gets the selector expression (e.g., label selectors).
    /// </summary>
    string Expression { get; }

    /// <summary>
    /// Selects instances based on custom criteria.
    /// </summary>
    /// <param name="context">The naming context containing service info and instances.</param>
    /// <returns>The selection result containing filtered instances.</returns>
    NamingResult Select(NamingContext context);
}

/// <summary>
/// Context for naming selection operations.
/// </summary>
public class NamingContext
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cluster names.
    /// </summary>
    public List<string> Clusters { get; set; } = new();

    /// <summary>
    /// Gets or sets the service info.
    /// </summary>
    public ServiceInfo? ServiceInfo { get; set; }

    /// <summary>
    /// Gets or sets all available instances.
    /// </summary>
    public List<Instance> Instances { get; set; } = new();

    /// <summary>
    /// Gets or sets whether only healthy instances should be selected.
    /// </summary>
    public bool HealthyOnly { get; set; } = true;
}

/// <summary>
/// Result of naming selection operations.
/// </summary>
public class NamingResult
{
    /// <summary>
    /// Gets or sets the selected instances.
    /// </summary>
    public List<Instance> Instances { get; set; } = new();

    /// <summary>
    /// Gets the count of selected instances.
    /// </summary>
    public int Count => Instances.Count;

    /// <summary>
    /// Gets whether the selection was successful (has instances).
    /// </summary>
    public bool Success => Instances.Count > 0;

    /// <summary>
    /// Creates a successful result with the given instances.
    /// </summary>
    public static NamingResult Of(List<Instance> instances) => new() { Instances = instances };

    /// <summary>
    /// Creates an empty result.
    /// </summary>
    public static NamingResult Empty() => new() { Instances = new List<Instance>() };
}
