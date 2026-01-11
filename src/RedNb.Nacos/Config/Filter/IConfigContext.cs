namespace RedNb.Nacos.Core.Config.Filter;

/// <summary>
/// Config context interface for passing data between filters.
/// </summary>
public interface IConfigContext
{
    /// <summary>
    /// Gets a context parameter by key.
    /// </summary>
    /// <param name="key">Parameter key</param>
    /// <returns>Parameter value</returns>
    object? GetParameter(string key);

    /// <summary>
    /// Sets a context parameter.
    /// </summary>
    /// <param name="key">Parameter key</param>
    /// <param name="value">Parameter value</param>
    void SetParameter(string key, object? value);
}

/// <summary>
/// Default implementation of IConfigContext.
/// </summary>
public class ConfigContext : IConfigContext
{
    private readonly Dictionary<string, object?> _parameters = new();

    public object? GetParameter(string key)
    {
        return _parameters.TryGetValue(key, out var value) ? value : null;
    }

    public void SetParameter(string key, object? value)
    {
        _parameters[key] = value;
    }
}
