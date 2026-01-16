using System.Security.Cryptography;
using System.Text;

namespace RedNb.Nacos.Utils.Crypto;

/// <summary>
/// Hash and cryptography helper methods.
/// </summary>
public static class HashHelper
{
    /// <summary>
    /// Calculates MD5 hash of a string.
    /// </summary>
    public static string GetMd5(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = MD5.HashData(inputBytes);
        return ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// Converts bytes to lowercase hex string.
    /// Compatible with .NET 8 and earlier (Convert.ToHexStringLower is .NET 9+).
    /// </summary>
    private static string ToHexStringLower(byte[] bytes)
    {
#if NET9_0_OR_GREATER
        return Convert.ToHexStringLower(bytes);
#else
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
#endif
    }
}
