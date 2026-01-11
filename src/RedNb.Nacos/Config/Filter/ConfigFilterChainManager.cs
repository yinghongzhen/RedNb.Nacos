namespace RedNb.Nacos.Core.Config.Filter;

/// <summary>
/// Manages the config filter chain.
/// </summary>
public class ConfigFilterChainManager : IConfigFilterChain
{
    private readonly List<IConfigFilter> _filters = new();
    private readonly IDictionary<string, string>? _initProperties;
    private readonly object _lock = new();

    public ConfigFilterChainManager(IDictionary<string, string>? properties = null)
    {
        _initProperties = properties;
    }

    /// <summary>
    /// Adds a filter to the chain.
    /// </summary>
    /// <param name="filter">Filter to add</param>
    /// <returns>This manager for chaining</returns>
    public ConfigFilterChainManager AddFilter(IConfigFilter filter)
    {
        lock (_lock)
        {
            // Initialize the filter
            filter.Init(_initProperties);

            // Check if filter with same name exists
            var existingIndex = _filters.FindIndex(f => f.FilterName == filter.FilterName);
            if (existingIndex >= 0)
            {
                // Replace existing filter
                _filters[existingIndex] = filter;
                return this;
            }

            // Insert in order (lower order first)
            var insertIndex = 0;
            while (insertIndex < _filters.Count && _filters[insertIndex].Order <= filter.Order)
            {
                insertIndex++;
            }

            _filters.Insert(insertIndex, filter);
            return this;
        }
    }

    /// <summary>
    /// Removes a filter from the chain by name.
    /// </summary>
    /// <param name="filterName">Name of filter to remove</param>
    /// <returns>True if removed</returns>
    public bool RemoveFilter(string filterName)
    {
        lock (_lock)
        {
            var index = _filters.FindIndex(f => f.FilterName == filterName);
            if (index >= 0)
            {
                _filters.RemoveAt(index);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Gets all registered filters.
    /// </summary>
    public IReadOnlyList<IConfigFilter> Filters
    {
        get
        {
            lock (_lock)
            {
                return _filters.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets whether this chain has any filters.
    /// </summary>
    public bool HasFilters
    {
        get
        {
            lock (_lock)
            {
                return _filters.Count > 0;
            }
        }
    }

    /// <summary>
    /// Removes a filter from the chain.
    /// </summary>
    /// <param name="filter">The filter to remove</param>
    /// <returns>True if the filter was removed</returns>
    public bool RemoveFilter(IConfigFilter filter)
    {
        return RemoveFilter(filter.FilterName);
    }

    /// <inheritdoc />
    public async Task DoFilterAsync(IConfigRequest request, IConfigResponse response, CancellationToken cancellationToken = default)
    {
        var chain = new VirtualFilterChain(Filters);
        await chain.DoFilterAsync(request, response, cancellationToken);
    }

    /// <summary>
    /// Internal filter chain that executes filters in sequence.
    /// </summary>
    private class VirtualFilterChain : IConfigFilterChain
    {
        private readonly IReadOnlyList<IConfigFilter> _filters;
        private int _currentPosition;

        public VirtualFilterChain(IReadOnlyList<IConfigFilter> filters)
        {
            _filters = filters;
            _currentPosition = 0;
        }

        public async Task DoFilterAsync(IConfigRequest request, IConfigResponse response, CancellationToken cancellationToken = default)
        {
            if (_currentPosition < _filters.Count)
            {
                var filter = _filters[_currentPosition];
                _currentPosition++;
                await filter.DoFilterAsync(request, response, this, cancellationToken);
            }
        }
    }
}
