using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Naming;

/// <summary>
/// Represents a service instance in Nacos.
/// </summary>
public class Instance
{
    /// <summary>
    /// Unique id of this instance.
    /// </summary>
    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    /// <summary>
    /// Instance IP address.
    /// </summary>
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// Instance port.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }

    /// <summary>
    /// Instance weight for load balancing.
    /// </summary>
    [JsonPropertyName("weight")]
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Instance health status.
    /// </summary>
    [JsonPropertyName("healthy")]
    public bool Healthy { get; set; } = true;

    /// <summary>
    /// If instance is enabled to accept request.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// If instance is ephemeral.
    /// </summary>
    [JsonPropertyName("ephemeral")]
    public bool Ephemeral { get; set; } = true;

    /// <summary>
    /// Cluster name of instance.
    /// </summary>
    [JsonPropertyName("clusterName")]
    public string ClusterName { get; set; } = NacosConstants.DefaultClusterName;

    /// <summary>
    /// Service name of instance.
    /// </summary>
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    /// <summary>
    /// User extended attributes.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Adds metadata key-value pair.
    /// </summary>
    public void AddMetadata(string key, string value)
    {
        Metadata[key] = value;
    }

    /// <summary>
    /// Returns true if metadata contains the specified key.
    /// </summary>
    public bool ContainsMetadata(string key)
    {
        return Metadata.ContainsKey(key);
    }

    /// <summary>
    /// Gets metadata value by key with default value.
    /// </summary>
    public string GetMetadata(string key, string defaultValue = "")
    {
        return Metadata.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Gets metadata value by key as long with default value.
    /// </summary>
    public long GetMetadataAsLong(string key, long defaultValue = 0)
    {
        if (Metadata.TryGetValue(key, out var value) && long.TryParse(value, out var result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Gets the instance heart beat interval.
    /// </summary>
    public long GetInstanceHeartBeatInterval()
    {
        return GetMetadataAsLong(PreservedMetadataKeys.HeartBeatInterval, NacosConstants.DefaultHeartBeatInterval);
    }

    /// <summary>
    /// Gets the instance heart beat timeout.
    /// </summary>
    public long GetInstanceHeartBeatTimeout()
    {
        return GetMetadataAsLong(PreservedMetadataKeys.HeartBeatTimeout, NacosConstants.DefaultHeartBeatTimeout);
    }

    /// <summary>
    /// Gets the IP delete timeout.
    /// </summary>
    public long GetIpDeleteTimeout()
    {
        return GetMetadataAsLong(PreservedMetadataKeys.IpDeleteTimeout, NacosConstants.DefaultIpDeleteTimeout);
    }

    /// <summary>
    /// Gets the instance address in format ip:port.
    /// </summary>
    public string ToInetAddr()
    {
        return $"{Ip}:{Port}";
    }

    /// <summary>
    /// Validates the instance.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Ip))
        {
            throw new NacosException(NacosException.InvalidParam, "Required parameter 'ip' is not present");
        }

        if (Port < 0 || Port > 65535)
        {
            throw new NacosException(NacosException.InvalidParam, "Required parameter 'port' is require 0 ~ 65535");
        }

        if (string.IsNullOrWhiteSpace(ClusterName))
        {
            ClusterName = NacosConstants.DefaultClusterName;
        }
    }

    public override string ToString()
    {
        return $"Instance{{instanceId='{InstanceId}', ip='{Ip}', port={Port}, weight={Weight}, healthy={Healthy}, enabled={Enabled}, ephemeral={Ephemeral}, clusterName='{ClusterName}', serviceName='{ServiceName}', metadata={string.Join(", ", Metadata.Select(kv => $"{kv.Key}={kv.Value}"))}}}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Instance other) return false;
        return ToString() == other.ToString();
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
}
