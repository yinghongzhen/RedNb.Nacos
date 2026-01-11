namespace RedNb.Nacos.Core.Config.Filter;

/// <summary>
/// Config request interface for filter chain.
/// </summary>
public interface IConfigRequest
{
    /// <summary>
    /// Puts a parameter.
    /// </summary>
    /// <param name="key">Parameter key</param>
    /// <param name="value">Parameter value</param>
    void PutParameter(string key, object? value);

    /// <summary>
    /// Gets a parameter.
    /// </summary>
    /// <param name="key">Parameter key</param>
    /// <returns>Parameter value</returns>
    object? GetParameter(string key);

    /// <summary>
    /// Gets the config context.
    /// </summary>
    IConfigContext ConfigContext { get; }
}

/// <summary>
/// Config request parameter keys.
/// </summary>
public static class ConfigRequestKeys
{
    public const string DataId = "dataId";
    public const string Group = "group";
    public const string Tenant = "tenant";
    public const string Content = "content";
    public const string EncryptedDataKey = "encryptedDataKey";
    public const string Type = "type";
}

/// <summary>
/// Default implementation of IConfigRequest.
/// </summary>
public class ConfigRequest : IConfigRequest
{
    private readonly Dictionary<string, object?> _parameters = new();
    private readonly IConfigContext _configContext;

    public ConfigRequest()
    {
        _configContext = new ConfigContext();
    }

    public ConfigRequest(string dataId, string group, string? tenant, string? content)
        : this()
    {
        PutParameter(ConfigRequestKeys.DataId, dataId);
        PutParameter(ConfigRequestKeys.Group, group);
        PutParameter(ConfigRequestKeys.Tenant, tenant);
        PutParameter(ConfigRequestKeys.Content, content);
    }

    public void PutParameter(string key, object? value)
    {
        _parameters[key] = value;
    }

    /// <summary>
    /// Sets a parameter (alias for PutParameter).
    /// </summary>
    public void SetParameter(string key, object? value) => PutParameter(key, value);

    public object? GetParameter(string key)
    {
        return _parameters.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Gets a typed parameter value.
    /// </summary>
    public T? GetParameter<T>(string key) where T : class
    {
        return GetParameter(key) as T;
    }

    public IConfigContext ConfigContext => _configContext;

    /// <summary>
    /// Gets the DataId.
    /// </summary>
    public string? DataId => GetParameter(ConfigRequestKeys.DataId) as string;

    /// <summary>
    /// Gets the Group.
    /// </summary>
    public string? Group => GetParameter(ConfigRequestKeys.Group) as string;

    /// <summary>
    /// Gets the Tenant.
    /// </summary>
    public string? Tenant => GetParameter(ConfigRequestKeys.Tenant) as string;

    /// <summary>
    /// Gets or sets the Content.
    /// </summary>
    public string? Content
    {
        get => GetParameter(ConfigRequestKeys.Content) as string;
        set => PutParameter(ConfigRequestKeys.Content, value);
    }

    /// <summary>
    /// Gets or sets the encrypted data key.
    /// </summary>
    public string? EncryptedDataKey
    {
        get => GetParameter(ConfigRequestKeys.EncryptedDataKey) as string;
        set => PutParameter(ConfigRequestKeys.EncryptedDataKey, value);
    }
}
