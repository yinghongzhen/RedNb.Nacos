namespace RedNb.Nacos.Config.Parser;

/// <summary>
/// 配置变更项
/// </summary>
public class ConfigChangeItem
{
    /// <summary>
    /// 属性键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 旧值
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// 新值
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// 变更类型
    /// </summary>
    public PropertyChangeType Type { get; set; }

    /// <summary>
    /// 无参构造函数
    /// </summary>
    public ConfigChangeItem()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigChangeItem(string key, string? oldValue, string? newValue, PropertyChangeType type)
    {
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
        Type = type;
    }

    /// <summary>
    /// 创建新增项
    /// </summary>
    public static ConfigChangeItem CreateAdded(string key, string newValue)
    {
        return new ConfigChangeItem(key, null, newValue, PropertyChangeType.Added);
    }

    /// <summary>
    /// 创建修改项
    /// </summary>
    public static ConfigChangeItem CreateModified(string key, string oldValue, string newValue)
    {
        return new ConfigChangeItem(key, oldValue, newValue, PropertyChangeType.Modified);
    }

    /// <summary>
    /// 创建删除项
    /// </summary>
    public static ConfigChangeItem CreateDeleted(string key, string oldValue)
    {
        return new ConfigChangeItem(key, oldValue, null, PropertyChangeType.Deleted);
    }

    public override string ToString()
    {
        return $"[{Type}] {Key}: {OldValue} -> {NewValue}";
    }
}
