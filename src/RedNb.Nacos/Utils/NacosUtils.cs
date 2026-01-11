using System.Security.Cryptography;
using System.Text;

namespace RedNb.Nacos.Core.Utils;

/// <summary>
/// Common utility methods.
/// </summary>
public static class NacosUtils
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

    /// <summary>
    /// Gets current timestamp in milliseconds.
    /// </summary>
    public static long GetCurrentTimeMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Generates a unique group key.
    /// </summary>
    public static string GetGroupKey(string dataId, string group)
    {
        return $"{dataId}+{group}";
    }

    /// <summary>
    /// Generates a unique group key with tenant.
    /// </summary>
    public static string GetGroupKey(string dataId, string group, string? tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant))
        {
            return GetGroupKey(dataId, group);
        }
        return $"{dataId}+{group}+{tenant}";
    }

    /// <summary>
    /// Parses group key into components.
    /// </summary>
    public static (string DataId, string Group, string? Tenant) ParseGroupKey(string groupKey)
    {
        var parts = groupKey.Split('+');
        return parts.Length switch
        {
            3 => (parts[0], parts[1], parts[2]),
            2 => (parts[0], parts[1], null),
            _ => throw new ArgumentException($"Invalid group key: {groupKey}")
        };
    }

    /// <summary>
    /// Gets grouped service name.
    /// </summary>
    public static string GetGroupedServiceName(string serviceName, string groupName)
    {
        if (serviceName.Contains(NacosConstants.ServiceInfoSplitter))
        {
            return serviceName;
        }
        return $"{groupName}{NacosConstants.ServiceInfoSplitter}{serviceName}";
    }

    /// <summary>
    /// Parses grouped service name.
    /// </summary>
    public static (string ServiceName, string GroupName) ParseGroupedServiceName(string groupedName)
    {
        var idx = groupedName.LastIndexOf(NacosConstants.ServiceInfoSplitter, StringComparison.Ordinal);
        if (idx > 0)
        {
            var groupName = groupedName[..idx];
            var serviceName = groupedName[(idx + NacosConstants.ServiceInfoSplitter.Length)..];
            return (serviceName, groupName);
        }
        return (groupedName, NacosConstants.DefaultGroup);
    }

    /// <summary>
    /// Checks if a string is a number.
    /// </summary>
    public static bool IsNumber(string? value)
    {
        return !string.IsNullOrEmpty(value) && long.TryParse(value, out _);
    }

    /// <summary>
    /// URL encodes a string.
    /// </summary>
    public static string UrlEncode(string value)
    {
        return Uri.EscapeDataString(value);
    }

    /// <summary>
    /// URL decodes a string.
    /// </summary>
    public static string UrlDecode(string value)
    {
        return Uri.UnescapeDataString(value);
    }

    /// <summary>
    /// Checks if a string is null, empty, or whitespace.
    /// </summary>
    public static bool IsBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Checks if a string is not null, empty, or whitespace.
    /// </summary>
    public static bool IsNotBlank(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Checks if a string is a valid IPv4 address.
    /// </summary>
    public static bool IsIpv4(string? ip)
    {
        if (string.IsNullOrEmpty(ip))
        {
            return false;
        }

        if (System.Net.IPAddress.TryParse(ip, out var address))
        {
            return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }

        return false;
    }

    /// <summary>
    /// Builds query string from dictionary.
    /// </summary>
    public static string BuildQueryString(Dictionary<string, string?> parameters)
    {
        var pairs = parameters
            .Where(kvp => kvp.Value != null)
            .Select(kvp => $"{UrlEncode(kvp.Key)}={UrlEncode(kvp.Value!)}");
        return string.Join("&", pairs);
    }

    /// <summary>
    /// Gets cluster string from list.
    /// </summary>
    public static string GetClusterString(List<string>? clusters)
    {
        if (clusters == null || clusters.Count == 0)
        {
            return string.Empty;
        }
        return string.Join(",", clusters.Where(c => !string.IsNullOrWhiteSpace(c)));
    }

    /// <summary>
    /// Parses cluster string to list.
    /// </summary>
    public static List<string> ParseClusterString(string? clusters)
    {
        if (string.IsNullOrWhiteSpace(clusters))
        {
            return new List<string>();
        }
        return clusters.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList();
    }
}
