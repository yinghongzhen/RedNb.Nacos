namespace RedNb.Nacos.Common.Utils;

/// <summary>
/// MD5 工具类
/// </summary>
public static class Md5Utils
{
    /// <summary>
    /// 计算 MD5 哈希值
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>MD5 哈希值（小写）</returns>
    public static string ComputeMd5(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// 计算 MD5 哈希值（大写）
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>MD5 哈希值（大写）</returns>
    public static string ComputeMd5Upper(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// 计算配置内容的 MD5
    /// </summary>
    /// <param name="content">配置内容</param>
    /// <returns>MD5 值</returns>
    public static string GetConfigMd5(string? content)
    {
        return string.IsNullOrEmpty(content) ? string.Empty : ComputeMd5(content);
    }
}
