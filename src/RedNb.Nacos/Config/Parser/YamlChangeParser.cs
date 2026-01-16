using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RedNb.Nacos.Config.Parser;

/// <summary>
/// YAML 格式配置变更解析器
/// </summary>
public class YamlChangeParser : AbstractConfigChangeParser
{
    private static readonly string[] SupportedTypes = { "yaml", "yml" };
    private readonly IDeserializer _deserializer;

    public YamlChangeParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    /// <inheritdoc />
    public override bool IsSupport(string configType)
    {
        return SupportedTypes.Any(t => t.Equals(configType, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    protected override Dictionary<string, string> ParseToMap(string content)
    {
        var result = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        try
        {
            var yamlObject = _deserializer.Deserialize<object>(content);
            if (yamlObject != null)
            {
                FlattenYaml(string.Empty, yamlObject, result);
            }
        }
        catch
        {
            // 解析失败时返回空字典
        }

        return result;
    }

    /// <summary>
    /// 将 YAML 对象扁平化为点分隔的键值对
    /// </summary>
    /// <param name="prefix">当前前缀</param>
    /// <param name="obj">YAML 对象</param>
    /// <param name="result">结果字典</param>
    private static void FlattenYaml(string prefix, object obj, Dictionary<string, string> result)
    {
        if (obj is IDictionary<object, object> dict)
        {
            foreach (var kvp in dict)
            {
                var key = kvp.Key?.ToString() ?? string.Empty;
                var newPrefix = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";

                if (kvp.Value == null)
                {
                    result[newPrefix] = string.Empty;
                }
                else if (IsSimpleType(kvp.Value))
                {
                    result[newPrefix] = kvp.Value.ToString() ?? string.Empty;
                }
                else
                {
                    FlattenYaml(newPrefix, kvp.Value, result);
                }
            }
        }
        else if (obj is IList<object> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var newPrefix = $"{prefix}[{i}]";
                var item = list[i];

                if (item == null)
                {
                    result[newPrefix] = string.Empty;
                }
                else if (IsSimpleType(item))
                {
                    result[newPrefix] = item.ToString() ?? string.Empty;
                }
                else
                {
                    FlattenYaml(newPrefix, item, result);
                }
            }
        }
        else
        {
            result[prefix] = obj.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// 判断是否为简单类型
    /// </summary>
    private static bool IsSimpleType(object obj)
    {
        var type = obj.GetType();
        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
               type == typeof(DateTime) || type == typeof(DateTimeOffset) || type.IsEnum;
    }
}
