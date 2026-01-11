using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Naming;

/// <summary>
/// Service information with instances, used in data pushing and cached for nacos-client.
/// </summary>
public class ServiceInfo
{
    /// <summary>
    /// Service name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Group name.
    /// </summary>
    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = NacosConstants.DefaultGroup;

    /// <summary>
    /// Clusters.
    /// </summary>
    [JsonPropertyName("clusters")]
    public string? Clusters { get; set; }

    /// <summary>
    /// Cache milliseconds.
    /// </summary>
    [JsonPropertyName("cacheMillis")]
    public long CacheMillis { get; set; } = 1000L;

    /// <summary>
    /// Service instances (hosts).
    /// </summary>
    [JsonPropertyName("hosts")]
    public List<Instance> Hosts { get; set; } = new();

    /// <summary>
    /// Last refresh time.
    /// </summary>
    [JsonPropertyName("lastRefTime")]
    public long LastRefTime { get; set; }

    /// <summary>
    /// Checksum.
    /// </summary>
    [JsonPropertyName("checksum")]
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Whether all IPs are returned.
    /// </summary>
    [JsonPropertyName("allIps")]
    public bool AllIps { get; set; }

    /// <summary>
    /// Whether protection threshold is reached.
    /// </summary>
    [JsonPropertyName("reachProtectionThreshold")]
    public bool ReachProtectionThreshold { get; set; }

    /// <summary>
    /// JSON from server (for caching purposes).
    /// </summary>
    [JsonIgnore]
    public string? JsonFromServer { get; set; }

    public ServiceInfo()
    {
    }

    public ServiceInfo(string name, string? clusters = null)
    {
        Name = name;
        Clusters = clusters;
    }

    /// <summary>
    /// Constructs ServiceInfo from a key string.
    /// </summary>
    /// <param name="key">Key in format: groupName@@serviceName@@clusters</param>
    public ServiceInfo(string key, bool parseFromKey)
    {
        if (!parseFromKey)
        {
            Name = key;
            return;
        }

        var parts = key.Split(NacosConstants.ServiceInfoSplitter);
        if (parts.Length >= 3)
        {
            GroupName = parts[0];
            Name = parts[1];
            Clusters = parts[2];
        }
        else if (parts.Length == 2)
        {
            GroupName = parts[0];
            Name = parts[1];
        }
        else
        {
            throw new ArgumentException($"Can't parse out 'groupName' from key: {key}");
        }
    }

    /// <summary>
    /// Gets the count of instances.
    /// </summary>
    public int IpCount() => Hosts.Count;

    /// <summary>
    /// Checks if the service info has expired.
    /// </summary>
    public bool Expired() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - LastRefTime > CacheMillis;

    /// <summary>
    /// Checks if the service info is valid.
    /// </summary>
    public bool IsValid() => Hosts.Count > 0;

    /// <summary>
    /// Validates if service info has valid healthy instances.
    /// </summary>
    public bool Validate()
    {
        if (AllIps) return true;
        return Hosts.Any(h => h.Healthy && h.Weight > 0);
    }

    /// <summary>
    /// Gets the unique key for this service info.
    /// </summary>
    [JsonIgnore]
    public string Key
    {
        get
        {
            var serviceName = GetGroupedServiceName();
            return GetKey(serviceName, Clusters);
        }
    }

    /// <summary>
    /// Gets the grouped service name.
    /// </summary>
    public string GetGroupedServiceName()
    {
        if (!string.IsNullOrEmpty(GroupName) && !Name.Contains(NacosConstants.ServiceInfoSplitter))
        {
            return $"{GroupName}{NacosConstants.ServiceInfoSplitter}{Name}";
        }
        return Name;
    }

    /// <summary>
    /// Gets a key from service name and clusters.
    /// </summary>
    public static string GetKey(string name, string? clusters)
    {
        if (!string.IsNullOrEmpty(clusters))
        {
            return $"{name}{NacosConstants.ServiceInfoSplitter}{clusters}";
        }
        return name;
    }

    /// <summary>
    /// Creates ServiceInfo from a key.
    /// </summary>
    public static ServiceInfo FromKey(string key)
    {
        return new ServiceInfo(key, parseFromKey: true);
    }

    public override string ToString() => Key;

    /// <summary>
    /// Adds an instance to the hosts list.
    /// </summary>
    public void AddHost(Instance host)
    {
        Hosts.Add(host);
    }

    /// <summary>
    /// Adds multiple instances to the hosts list.
    /// </summary>
    public void AddAllHosts(IEnumerable<Instance> hosts)
    {
        Hosts.AddRange(hosts);
    }
}
