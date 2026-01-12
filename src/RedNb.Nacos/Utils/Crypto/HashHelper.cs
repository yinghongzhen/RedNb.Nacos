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
        return Convert.ToHexStringLower(hashBytes);
    }
}
