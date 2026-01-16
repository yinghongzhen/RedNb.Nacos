namespace RedNb.Nacos.Config.Parser;

/// <summary>
/// 配置变更解析器接口
/// </summary>
public interface IConfigChangeParser
{
    /// <summary>
    /// 判断是否为该解析器的目标类型
    /// </summary>
    /// <param name="configType">配置类型（如 yaml, properties, json 等）</param>
    /// <returns>是否支持</returns>
    bool IsSupport(string configType);

    /// <summary>
    /// 解析配置变更
    /// </summary>
    /// <param name="oldContent">旧配置内容</param>
    /// <param name="newContent">新配置内容</param>
    /// <param name="configType">配置类型</param>
    /// <returns>变更项字典，key为属性路径</returns>
    Dictionary<string, ConfigChangeItem> Parse(string? oldContent, string? newContent, string configType);
}
