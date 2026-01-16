namespace RedNb.Nacos.Config.Parser;

/// <summary>
/// 配置变更解析器抽象基类
/// </summary>
public abstract class AbstractConfigChangeParser : IConfigChangeParser
{
    /// <inheritdoc />
    public abstract bool IsSupport(string configType);

    /// <inheritdoc />
    public Dictionary<string, ConfigChangeItem> Parse(string? oldContent, string? newContent, string configType)
    {
        var oldMap = string.IsNullOrEmpty(oldContent) 
            ? new Dictionary<string, string>() 
            : ParseToMap(oldContent);
            
        var newMap = string.IsNullOrEmpty(newContent) 
            ? new Dictionary<string, string>() 
            : ParseToMap(newContent);

        return FilterChangeData(oldMap, newMap);
    }

    /// <summary>
    /// 解析配置内容为字典
    /// </summary>
    /// <param name="content">配置内容</param>
    /// <returns>配置字典</returns>
    protected abstract Dictionary<string, string> ParseToMap(string content);

    /// <summary>
    /// 过滤并生成变更数据
    /// </summary>
    /// <param name="oldMap">旧配置字典</param>
    /// <param name="newMap">新配置字典</param>
    /// <returns>变更项字典</returns>
    protected virtual Dictionary<string, ConfigChangeItem> FilterChangeData(
        Dictionary<string, string> oldMap,
        Dictionary<string, string> newMap)
    {
        var result = new Dictionary<string, ConfigChangeItem>();

        // 检查删除和修改的项
        foreach (var kvp in oldMap)
        {
            var key = kvp.Key;
            var oldValue = kvp.Value;

            if (!newMap.TryGetValue(key, out var newValue))
            {
                // 键在新配置中不存在，标记为删除
                result[key] = ConfigChangeItem.CreateDeleted(key, oldValue);
            }
            else if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                // 值发生变化，标记为修改
                result[key] = ConfigChangeItem.CreateModified(key, oldValue, newValue);
            }
        }

        // 检查新增的项
        foreach (var kvp in newMap)
        {
            var key = kvp.Key;
            if (!oldMap.ContainsKey(key))
            {
                result[key] = ConfigChangeItem.CreateAdded(key, kvp.Value);
            }
        }

        return result;
    }
}
