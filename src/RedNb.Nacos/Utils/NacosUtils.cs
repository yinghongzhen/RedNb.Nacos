using RedNb.Nacos.Utils.Crypto;
using RedNb.Nacos.Utils.Keys;
using RedNb.Nacos.Utils.Naming;
using RedNb.Nacos.Utils.Network;
using RedNb.Nacos.Utils.Strings;
using RedNb.Nacos.Utils.Time;

namespace RedNb.Nacos.Utils;

/// <summary>
/// Facade class for common utility methods.
/// Provides backward compatibility by delegating to specialized helper classes.
/// </summary>
/// <remarks>
/// Consider using the specialized helper classes directly for better clarity:
/// <list type="bullet">
///   <item><see cref="HashHelper"/> - Cryptography operations</item>
///   <item><see cref="TimeHelper"/> - Time operations</item>
///   <item><see cref="GroupKeyHelper"/> - Config group key operations</item>
///   <item><see cref="ServiceNameHelper"/> - Naming service operations</item>
///   <item><see cref="UrlHelper"/> - URL encoding/query string</item>
///   <item><see cref="IpHelper"/> - IP address validation</item>
///   <item><see cref="StringHelper"/> - String validation</item>
/// </list>
/// </remarks>
public static class NacosUtils
{
    /// <summary>
    /// Calculates MD5 hash of a string.
    /// </summary>
    public static string GetMd5(string input) 
        => HashHelper.GetMd5(input);

    /// <summary>
    /// Gets current timestamp in milliseconds.
    /// </summary>
    public static long GetCurrentTimeMillis() 
        => TimeHelper.GetCurrentTimeMillis();

    /// <summary>
    /// Generates a unique group key.
    /// </summary>
    public static string GetGroupKey(string dataId, string group) 
        => GroupKeyHelper.GetGroupKey(dataId, group);

    /// <summary>
    /// Generates a unique group key with tenant.
    /// </summary>
    public static string GetGroupKey(string dataId, string group, string? tenant) 
        => GroupKeyHelper.GetGroupKey(dataId, group, tenant);

    /// <summary>
    /// Parses group key into components.
    /// </summary>
    public static (string DataId, string Group, string? Tenant) ParseGroupKey(string groupKey) 
        => GroupKeyHelper.ParseGroupKey(groupKey);

    /// <summary>
    /// Gets grouped service name.
    /// </summary>
    public static string GetGroupedServiceName(string serviceName, string groupName) 
        => ServiceNameHelper.GetGroupedServiceName(serviceName, groupName);

    /// <summary>
    /// Parses grouped service name.
    /// </summary>
    public static (string ServiceName, string GroupName) ParseGroupedServiceName(string groupedName) 
        => ServiceNameHelper.ParseGroupedServiceName(groupedName);

    /// <summary>
    /// Checks if a string is a number.
    /// </summary>
    public static bool IsNumber(string? value) 
        => StringHelper.IsNumber(value);

    /// <summary>
    /// URL encodes a string.
    /// </summary>
    public static string UrlEncode(string value) 
        => UrlHelper.UrlEncode(value);

    /// <summary>
    /// URL decodes a string.
    /// </summary>
    public static string UrlDecode(string value) 
        => UrlHelper.UrlDecode(value);

    /// <summary>
    /// Checks if a string is null, empty, or whitespace.
    /// </summary>
    public static bool IsBlank(string? value) 
        => StringHelper.IsBlank(value);

    /// <summary>
    /// Checks if a string is not null, empty, or whitespace.
    /// </summary>
    public static bool IsNotBlank(string? value) 
        => StringHelper.IsNotBlank(value);

    /// <summary>
    /// Checks if a string is a valid IPv4 address.
    /// </summary>
    public static bool IsIpv4(string? ip) 
        => IpHelper.IsIpv4(ip);

    /// <summary>
    /// Builds query string from dictionary.
    /// </summary>
    public static string BuildQueryString(Dictionary<string, string?> parameters) 
        => UrlHelper.BuildQueryString(parameters);

    /// <summary>
    /// Gets cluster string from list.
    /// </summary>
    public static string GetClusterString(List<string>? clusters) 
        => ServiceNameHelper.GetClusterString(clusters);

    /// <summary>
    /// Parses cluster string to list.
    /// </summary>
    public static List<string> ParseClusterString(string? clusters) 
        => ServiceNameHelper.ParseClusterString(clusters);
}
