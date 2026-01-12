namespace RedNb.Nacos.Utils.Network;

/// <summary>
/// URL encoding/decoding and query string helper methods.
/// </summary>
public static class UrlHelper
{
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
    /// Builds query string from dictionary.
    /// </summary>
    public static string BuildQueryString(Dictionary<string, string?> parameters)
    {
        var pairs = parameters
            .Where(kvp => kvp.Value != null)
            .Select(kvp => $"{UrlEncode(kvp.Key)}={UrlEncode(kvp.Value!)}");
        return string.Join("&", pairs);
    }
}
