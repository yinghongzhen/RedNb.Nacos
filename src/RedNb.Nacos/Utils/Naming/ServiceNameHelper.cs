using RedNb.Nacos.Core;

namespace RedNb.Nacos.Utils.Naming;

/// <summary>
/// Helper methods for service name operations.
/// </summary>
public static class ServiceNameHelper
{
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
