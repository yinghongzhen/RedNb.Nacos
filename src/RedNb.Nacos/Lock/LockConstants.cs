namespace RedNb.Nacos.Core.Lock;

/// <summary>
/// Constants for distributed lock service.
/// </summary>
public static class LockConstants
{
    /// <summary>
    /// Default lock type - Nacos internal lock implementation.
    /// </summary>
    public const string DefaultLockType = "nacos";

    /// <summary>
    /// Lock type for Redis-based distributed lock (requires SPI extension).
    /// </summary>
    public const string LockTypeRedis = "redis";

    /// <summary>
    /// Lock type for ZooKeeper-based distributed lock (requires SPI extension).
    /// </summary>
    public const string LockTypeZookeeper = "zookeeper";

    /// <summary>
    /// Lock type for etcd-based distributed lock (requires SPI extension).
    /// </summary>
    public const string LockTypeEtcd = "etcd";

    /// <summary>
    /// Default lock expiration time in milliseconds (30 seconds).
    /// </summary>
    public const long DefaultExpireTime = 30000;

    /// <summary>
    /// Default lock acquire timeout in milliseconds (10 seconds).
    /// </summary>
    public const long DefaultAcquireTimeout = 10000;

    /// <summary>
    /// Lock acquire retry interval in milliseconds.
    /// </summary>
    public const long RetryInterval = 100;

    /// <summary>
    /// Lock heartbeat interval in milliseconds (for lock renewal).
    /// </summary>
    public const long HeartbeatInterval = 10000;

    /// <summary>
    /// API path for lock operations.
    /// </summary>
    public const string LockApiPath = "/nacos/v3/lock";

    /// <summary>
    /// API path for acquiring lock.
    /// </summary>
    public const string LockAcquirePath = "/nacos/v3/lock/acquire";

    /// <summary>
    /// API path for releasing lock.
    /// </summary>
    public const string LockReleasePath = "/nacos/v3/lock/release";

    /// <summary>
    /// API path for querying lock.
    /// </summary>
    public const string LockQueryPath = "/nacos/v3/lock/query";

    /// <summary>
    /// gRPC request type for lock acquire.
    /// </summary>
    public const string GrpcLockAcquireType = "LockAcquireRequest";

    /// <summary>
    /// gRPC request type for lock release.
    /// </summary>
    public const string GrpcLockReleaseType = "LockReleaseRequest";

    /// <summary>
    /// gRPC request type for lock query.
    /// </summary>
    public const string GrpcLockQueryType = "LockQueryRequest";
}
