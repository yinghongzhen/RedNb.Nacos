using System.Security.Cryptography;
using System.Text;

namespace RedNb.Nacos.Utils;

internal static class CacheIdentity
{
    public static string ForNaming(NacosClientOptions options)
    {
        var servers = options.GetServerAddressList()
            .Select(address => new Uri(options.GetBaseUrl(address)).AbsoluteUri.TrimEnd('/'))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        return HashParts(string.Join(",", servers), options.Endpoint ?? "",
            string.IsNullOrEmpty(options.Namespace) ? "public" : options.Namespace, options.Username ?? "");
    }

    public static string HashParts(params string[] parts)
    {
        var value = new StringBuilder();
        foreach (var part in parts) value.Append(part.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(':').Append(part);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.ToString())));
    }

    public static string For(NacosClientOptions options)
    {
        var servers = string.Join(",", options.GetServerAddressList().Order(StringComparer.Ordinal));
        var identity = $"{servers}\n{options.ContextPath}\n{options.Namespace ?? "public"}\n{options.Username}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }
}
