namespace RedNb.Nacos.Utils.Keys;

/// <summary>
/// Helper methods for generating and parsing group keys.
/// </summary>
public static class GroupKeyHelper
{
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
}
