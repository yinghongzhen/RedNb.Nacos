namespace RedNb.Nacos.Config.Parser;

/// <summary>
/// Properties 格式配置变更解析器
/// </summary>
public class PropertiesChangeParser : AbstractConfigChangeParser
{
    private static readonly string[] SupportedTypes = { "properties", "props" };

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

        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 跳过注释和空行
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#') || trimmedLine.StartsWith('!'))
            {
                continue;
            }

            // 查找分隔符
            var separatorIndex = FindSeparator(trimmedLine);
            if (separatorIndex < 0)
            {
                // 没有分隔符，整行作为 key，值为空
                result[trimmedLine] = string.Empty;
            }
            else
            {
                var key = trimmedLine.Substring(0, separatorIndex).Trim();
                var value = trimmedLine.Substring(separatorIndex + 1).TrimStart();

                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 查找分隔符位置
    /// </summary>
    private static int FindSeparator(string line)
    {
        var escapeMode = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (escapeMode)
            {
                escapeMode = false;
                continue;
            }

            if (c == '\\')
            {
                escapeMode = true;
                continue;
            }

            if (c == '=' || c == ':')
            {
                return i;
            }
        }

        return -1;
    }
}
