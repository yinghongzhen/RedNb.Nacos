using System.Text.Json;

namespace RedNb.Nacos.Config.Parser;

/// <summary>
/// JSON 格式配置变更解析器
/// </summary>
public class JsonChangeParser : AbstractConfigChangeParser
{
    private static readonly string[] SupportedTypes = { "json" };

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
            using var document = JsonDocument.Parse(content);
            FlattenJson(string.Empty, document.RootElement, result);
        }
        catch
        {
            // 解析失败时返回空字典
        }

        return result;
    }

    /// <summary>
    /// 将 JSON 元素扁平化为点分隔的键值对
    /// </summary>
    /// <param name="prefix">当前前缀</param>
    /// <param name="element">JSON 元素</param>
    /// <param name="result">结果字典</param>
    private static void FlattenJson(string prefix, JsonElement element, Dictionary<string, string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var newPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenJson(newPrefix, property.Value, result);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var newPrefix = $"{prefix}[{index}]";
                    FlattenJson(newPrefix, item, result);
                    index++;
                }
                break;

            case JsonValueKind.String:
                result[prefix] = element.GetString() ?? string.Empty;
                break;

            case JsonValueKind.Number:
                result[prefix] = element.GetRawText();
                break;

            case JsonValueKind.True:
                result[prefix] = "true";
                break;

            case JsonValueKind.False:
                result[prefix] = "false";
                break;

            case JsonValueKind.Null:
                result[prefix] = string.Empty;
                break;
        }
    }
}
