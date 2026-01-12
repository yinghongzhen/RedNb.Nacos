using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Lock;

/// <summary>
/// Represents a distributed lock instance.
/// </summary>
public class LockInstance
{
    /// <summary>
    /// Gets or sets the lock key (unique identifier).
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lock expiration time in milliseconds.
    /// After this time, the lock will be automatically released.
    /// </summary>
    [JsonPropertyName("expireTime")]
    public long ExpireTime { get; set; }

    /// <summary>
    /// Gets or sets the lock type for SPI extension.
    /// Default lock types: "nacos" (Nacos internal lock).
    /// </summary>
    [JsonPropertyName("lockType")]
    public string LockType { get; set; } = LockConstants.DefaultLockType;

    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    [JsonPropertyName("namespaceId")]
    public string? NamespaceId { get; set; }

    /// <summary>
    /// Gets or sets the lock owner (client ID).
    /// </summary>
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets additional parameters for lock extensions.
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, object>? Params { get; set; }

    /// <summary>
    /// Gets or sets the lock acquire timestamp.
    /// </summary>
    [JsonPropertyName("acquireTime")]
    public long AcquireTime { get; set; }

    /// <summary>
    /// Gets or sets whether this is a reentrant lock request.
    /// </summary>
    [JsonPropertyName("reentrant")]
    public bool Reentrant { get; set; }

    /// <summary>
    /// Creates a new lock instance with the specified key.
    /// </summary>
    /// <param name="key">The lock key.</param>
    /// <returns>A new LockInstance.</returns>
    public static LockInstance Create(string key)
    {
        return new LockInstance { Key = key };
    }

    /// <summary>
    /// Creates a new lock instance with key and expiration.
    /// </summary>
    /// <param name="key">The lock key.</param>
    /// <param name="expireTime">Expiration time in milliseconds.</param>
    /// <returns>A new LockInstance.</returns>
    public static LockInstance Create(string key, long expireTime)
    {
        return new LockInstance { Key = key, ExpireTime = expireTime };
    }

    /// <summary>
    /// Creates a new lock instance with key, expiration and lock type.
    /// </summary>
    /// <param name="key">The lock key.</param>
    /// <param name="expireTime">Expiration time in milliseconds.</param>
    /// <param name="lockType">The lock type.</param>
    /// <returns>A new LockInstance.</returns>
    public static LockInstance Create(string key, long expireTime, string lockType)
    {
        return new LockInstance { Key = key, ExpireTime = expireTime, LockType = lockType };
    }

    /// <summary>
    /// Sets the expiration time and returns this instance (fluent API).
    /// </summary>
    public LockInstance WithExpireTime(long expireTimeMs)
    {
        ExpireTime = expireTimeMs;
        return this;
    }

    /// <summary>
    /// Sets the expiration time from TimeSpan and returns this instance (fluent API).
    /// </summary>
    public LockInstance WithExpireTime(TimeSpan expireTime)
    {
        ExpireTime = (long)expireTime.TotalMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets the lock type and returns this instance (fluent API).
    /// </summary>
    public LockInstance WithLockType(string lockType)
    {
        LockType = lockType;
        return this;
    }

    /// <summary>
    /// Sets the namespace and returns this instance (fluent API).
    /// </summary>
    public LockInstance WithNamespace(string namespaceId)
    {
        NamespaceId = namespaceId;
        return this;
    }

    /// <summary>
    /// Sets the owner and returns this instance (fluent API).
    /// </summary>
    public LockInstance WithOwner(string owner)
    {
        Owner = owner;
        return this;
    }

    /// <summary>
    /// Adds a parameter and returns this instance (fluent API).
    /// </summary>
    public LockInstance WithParam(string key, object value)
    {
        Params ??= new Dictionary<string, object>();
        Params[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the reentrant flag and returns this instance (fluent API).
    /// </summary>
    public LockInstance WithReentrant(bool reentrant = true)
    {
        Reentrant = reentrant;
        return this;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"LockInstance[key={Key}, expireTime={ExpireTime}, lockType={LockType}, owner={Owner}]";
    }
}
