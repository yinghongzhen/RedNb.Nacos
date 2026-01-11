namespace RedNb.Nacos.Core.Config;

/// <summary>
/// Property change type enumeration.
/// </summary>
public enum PropertyChangeType
{
    /// <summary>
    /// Property was added.
    /// </summary>
    Added,

    /// <summary>
    /// Property was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// Property was deleted.
    /// </summary>
    Deleted
}

/// <summary>
/// Configuration change event.
/// </summary>
public class ConfigChangeEvent
{
    /// <summary>
    /// Data ID.
    /// </summary>
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Group name.
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Tenant/Namespace.
    /// </summary>
    public string? Tenant { get; set; }

    /// <summary>
    /// Change items.
    /// </summary>
    public Dictionary<string, ConfigChangeItem> ChangeItems { get; set; } = new();
}

/// <summary>
/// Configuration change item.
/// </summary>
public class ConfigChangeItem
{
    /// <summary>
    /// Property key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Old value.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Change type.
    /// </summary>
    public PropertyChangeType Type { get; set; }

    public ConfigChangeItem()
    {
    }

    public ConfigChangeItem(string key, string? oldValue, string? newValue, PropertyChangeType type)
    {
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
        Type = type;
    }
}
