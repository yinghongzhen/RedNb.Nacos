namespace RedNb.Nacos.Core.Config.Filter;

/// <summary>
/// Config filter interface for intercepting config operations.
/// DO NOT implement this interface directly, extend AbstractConfigFilter instead.
/// </summary>
public interface IConfigFilter
{
    /// <summary>
    /// Initializes the filter with properties.
    /// </summary>
    /// <param name="properties">Filter configuration properties</param>
    void Init(IDictionary<string, string>? properties);

    /// <summary>
    /// Executes the filter.
    /// </summary>
    /// <param name="request">Config request</param>
    /// <param name="response">Config response</param>
    /// <param name="filterChain">Filter chain to continue execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DoFilterAsync(IConfigRequest request, IConfigResponse response, IConfigFilterChain filterChain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the filter order. Lower values execute first.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Gets the filter name.
    /// </summary>
    string FilterName { get; }
}

/// <summary>
/// Abstract base class for config filters.
/// Extend this class to implement custom config filters.
/// </summary>
public abstract class AbstractConfigFilter : IConfigFilter
{
    /// <summary>
    /// Filter properties.
    /// </summary>
    protected IDictionary<string, string>? Properties { get; private set; }

    /// <inheritdoc />
    public virtual void Init(IDictionary<string, string>? properties)
    {
        Properties = properties;
    }

    /// <inheritdoc />
    public abstract Task DoFilterAsync(IConfigRequest request, IConfigResponse response, IConfigFilterChain filterChain, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract int Order { get; }

    /// <inheritdoc />
    public abstract string FilterName { get; }
}
