namespace RedNb.Nacos.Core.Naming;

/// <summary>
/// Preserved metadata keys for instance configuration.
/// </summary>
public static class PreservedMetadataKeys
{
    /// <summary>
    /// Heart beat interval key.
    /// </summary>
    public const string HeartBeatInterval = "preserved.heart.beat.interval";

    /// <summary>
    /// Heart beat timeout key.
    /// </summary>
    public const string HeartBeatTimeout = "preserved.heart.beat.timeout";

    /// <summary>
    /// IP delete timeout key.
    /// </summary>
    public const string IpDeleteTimeout = "preserved.ip.delete.timeout";

    /// <summary>
    /// Instance ID generator key.
    /// </summary>
    public const string InstanceIdGenerator = "preserved.instance.id.generator";

    /// <summary>
    /// Register source key.
    /// </summary>
    public const string RegisterSource = "preserved.register.source";
}
