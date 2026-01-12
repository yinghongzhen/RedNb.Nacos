using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Lock;

/// <summary>
/// Result of a lock operation.
/// </summary>
public class LockResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the lock instance returned by the server.
    /// </summary>
    [JsonPropertyName("lockInstance")]
    public LockInstance? LockInstance { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the error code if operation failed.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the lock owner if the lock is held by another client.
    /// </summary>
    [JsonPropertyName("currentOwner")]
    public string? CurrentOwner { get; set; }

    /// <summary>
    /// Gets or sets the remaining TTL in milliseconds if lock is held.
    /// </summary>
    [JsonPropertyName("remainingTtl")]
    public long RemainingTtl { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static LockResult Succeeded(LockInstance? instance = null)
    {
        return new LockResult { Success = true, LockInstance = instance };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static LockResult Failed(string message, int errorCode = -1)
    {
        return new LockResult { Success = false, Message = message, ErrorCode = errorCode };
    }

    /// <summary>
    /// Creates a result indicating lock is already held.
    /// </summary>
    public static LockResult LockHeld(string currentOwner, long remainingTtl)
    {
        return new LockResult
        {
            Success = false,
            CurrentOwner = currentOwner,
            RemainingTtl = remainingTtl,
            Message = $"Lock is held by {currentOwner}, remaining TTL: {remainingTtl}ms"
        };
    }
}

/// <summary>
/// Lock state enumeration.
/// </summary>
public enum LockState
{
    /// <summary>
    /// Lock is free and can be acquired.
    /// </summary>
    Free,

    /// <summary>
    /// Lock is held by the current client.
    /// </summary>
    Held,

    /// <summary>
    /// Lock is held by another client.
    /// </summary>
    HeldByOther,

    /// <summary>
    /// Lock state is unknown (e.g., server unreachable).
    /// </summary>
    Unknown
}
