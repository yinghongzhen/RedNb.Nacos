namespace RedNb.Nacos.Common.Utils;

/// <summary>
/// 加密工具类
/// </summary>
public static class EncryptionUtils
{
    /// <summary>
    /// 计算 HMAC-SHA1 签名
    /// </summary>
    public static string HmacSha1(string data, string key)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// 计算 HMAC-SHA256 签名
    /// </summary>
    public static string HmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// 生成阿里云签名
    /// </summary>
    /// <param name="resource">资源路径</param>
    /// <param name="accessKey">AccessKey</param>
    /// <param name="secretKey">SecretKey</param>
    /// <returns>签名字符串</returns>
    public static (string signature, string timestamp) GenerateAliyunSignature(
        string resource,
        string accessKey,
        string secretKey)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var signData = $"{resource}+{accessKey}+{timestamp}";
        var signature = HmacSha1(signData, secretKey);
        return (signature, timestamp);
    }
}
