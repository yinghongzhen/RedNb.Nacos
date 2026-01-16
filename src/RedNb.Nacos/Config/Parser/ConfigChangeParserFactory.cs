namespace RedNb.Nacos.Config.Parser;

/// <summary>
/// 配置变更解析器工厂
/// </summary>
public class ConfigChangeParserFactory
{
    private readonly List<IConfigChangeParser> _parsers;

    /// <summary>
    /// 默认实例
    /// </summary>
    public static ConfigChangeParserFactory Default { get; } = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigChangeParserFactory()
    {
        _parsers = new List<IConfigChangeParser>
        {
            new PropertiesChangeParser(),
            new YamlChangeParser(),
            new JsonChangeParser()
        };
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="parsers">解析器列表</param>
    public ConfigChangeParserFactory(IEnumerable<IConfigChangeParser> parsers)
    {
        _parsers = parsers.ToList();
    }

    /// <summary>
    /// 注册解析器
    /// </summary>
    /// <param name="parser">解析器实例</param>
    public void RegisterParser(IConfigChangeParser parser)
    {
        _parsers.Add(parser);
    }

    /// <summary>
    /// 获取指定配置类型的解析器
    /// </summary>
    /// <param name="configType">配置类型</param>
    /// <returns>解析器实例，如果不支持则返回null</returns>
    public IConfigChangeParser? GetParser(string configType)
    {
        return _parsers.FirstOrDefault(p => p.IsSupport(configType));
    }

    /// <summary>
    /// 解析配置变更
    /// </summary>
    /// <param name="oldContent">旧配置内容</param>
    /// <param name="newContent">新配置内容</param>
    /// <param name="configType">配置类型</param>
    /// <returns>变更项字典，如果不支持该类型则返回空字典</returns>
    public Dictionary<string, ConfigChangeItem> Parse(string? oldContent, string? newContent, string configType)
    {
        var parser = GetParser(configType);
        if (parser == null)
        {
            return new Dictionary<string, ConfigChangeItem>();
        }

        return parser.Parse(oldContent, newContent, configType);
    }

    /// <summary>
    /// 判断是否支持指定的配置类型
    /// </summary>
    /// <param name="configType">配置类型</param>
    /// <returns>是否支持</returns>
    public bool IsSupported(string configType)
    {
        return _parsers.Any(p => p.IsSupport(configType));
    }
}
