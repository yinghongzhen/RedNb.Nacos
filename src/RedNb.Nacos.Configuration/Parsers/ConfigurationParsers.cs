using System.Text.Json;

namespace RedNb.Nacos.Configuration.Parsers;

/// <summary>
/// 配置解析器接口
/// </summary>
public interface IConfigurationParser
{
    /// <summary>
    /// 解析配置内容
    /// </summary>
    IDictionary<string, string?> Parse(string content);
}

/// <summary>
/// JSON 配置解析器
/// </summary>
public class JsonConfigurationParser : IConfigurationParser
{
    public IDictionary<string, string?> Parse(string content)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(content))
            return data;

        try
        {
            using var doc = JsonDocument.Parse(content);
            ParseElement(doc.RootElement, string.Empty, data);
        }
        catch (JsonException)
        {
            // 如果不是有效的 JSON，尝试作为键值对解析
            ParseKeyValue(content, data);
        }

        return data;
    }

    private void ParseElement(JsonElement element, string prefix, Dictionary<string, string?> data)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}:{property.Name}";
                    ParseElement(property.Value, key, data);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}:{index}";
                    ParseElement(item, key, data);
                    index++;
                }
                break;

            case JsonValueKind.String:
                data[prefix] = element.GetString();
                break;

            case JsonValueKind.Number:
                data[prefix] = element.GetRawText();
                break;

            case JsonValueKind.True:
                data[prefix] = "true";
                break;

            case JsonValueKind.False:
                data[prefix] = "false";
                break;

            case JsonValueKind.Null:
                data[prefix] = null;
                break;
        }
    }

    private void ParseKeyValue(string content, Dictionary<string, string?> data)
    {
        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex > 0)
            {
                var key = trimmed[..separatorIndex].Trim();
                var value = separatorIndex < trimmed.Length - 1
                    ? trimmed[(separatorIndex + 1)..].Trim()
                    : string.Empty;
                data[key] = value;
            }
        }
    }
}

/// <summary>
/// Properties 配置解析器
/// </summary>
public class PropertiesConfigurationParser : IConfigurationParser
{
    public IDictionary<string, string?> Parse(string content)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(content))
            return data;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#') || trimmed.StartsWith('!'))
                continue;

            var separatorIndex = trimmed.IndexOfAny(['=', ':']);
            if (separatorIndex > 0)
            {
                var key = trimmed[..separatorIndex].Trim();
                var value = separatorIndex < trimmed.Length - 1
                    ? trimmed[(separatorIndex + 1)..].Trim()
                    : string.Empty;

                // 将点号转换为冒号（Microsoft.Extensions.Configuration 风格）
                key = key.Replace('.', ':');
                data[key] = value;
            }
        }

        return data;
    }
}

/// <summary>
/// YAML 配置解析器（简化版本，仅支持基本语法）
/// </summary>
public class YamlConfigurationParser : IConfigurationParser
{
    public IDictionary<string, string?> Parse(string content)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(content))
            return data;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var pathStack = new Stack<(int indent, string key)>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                continue;

            var indent = line.Length - line.TrimStart().Length;
            var trimmed = line.Trim();

            // 弹出缩进不匹配的路径
            while (pathStack.Count > 0 && pathStack.Peek().indent >= indent)
            {
                pathStack.Pop();
            }

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = trimmed[..colonIndex].Trim();
                var value = colonIndex < trimmed.Length - 1
                    ? trimmed[(colonIndex + 1)..].Trim()
                    : null;

                if (string.IsNullOrEmpty(value))
                {
                    // 这是一个对象节点
                    pathStack.Push((indent, key));
                }
                else
                {
                    // 这是一个值节点
                    var fullKey = string.Join(":", pathStack.Reverse().Select(p => p.key).Append(key));

                    // 移除引号
                    if ((value.StartsWith('"') && value.EndsWith('"')) ||
                        (value.StartsWith('\'') && value.EndsWith('\'')))
                    {
                        value = value[1..^1];
                    }

                    data[fullKey] = value;
                }
            }
        }

        return data;
    }
}

/// <summary>
/// 配置解析器工厂
/// </summary>
public static class ConfigurationParserFactory
{
    /// <summary>
    /// 根据 DataId 后缀获取解析器
    /// </summary>
    public static IConfigurationParser GetParser(string dataId)
    {
        if (dataId.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return new JsonConfigurationParser();
        }

        if (dataId.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
            dataId.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
        {
            return new YamlConfigurationParser();
        }

        if (dataId.EndsWith(".properties", StringComparison.OrdinalIgnoreCase) ||
            dataId.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
        {
            return new PropertiesConfigurationParser();
        }

        // 默认使用 JSON 解析器
        return new JsonConfigurationParser();
    }
}
