using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Lock;
using RedNb.Nacos.GrpcClient;

namespace RedNb.Nacos.Grpc.Lock;

/// <summary>
/// gRPC implementation of the Nacos lock service.
/// </summary>
public class NacosGrpcLockService : ILockService
{
    private readonly NacosClientOptions _options;
    private readonly NacosGrpcClient _grpcClient;
    private readonly ILogger<NacosGrpcLockService>? _logger;
    private readonly string _clientId;
    private volatile bool _disposed;
    private volatile string _serverStatus = "UP";

    // Local lock tracking for reentrant support
    private readonly Dictionary<string, int> _localLockCounts = new();
    private readonly object _lockCountsLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NacosGrpcLockService"/> class.
    /// </summary>
    /// <param name="options">The Nacos client options.</param>
    /// <param name="logger">The logger.</param>
    public NacosGrpcLockService(NacosClientOptions options, ILogger<NacosGrpcLockService>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _clientId = Guid.NewGuid().ToString("N");
        _grpcClient = new NacosGrpcClient(options, logger);
    }

    /// <summary>
    /// Initializes the gRPC connection.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _grpcClient.ConnectAsync(cancellationToken);
        _logger?.LogInformation("Lock service gRPC connection established");
    }

    /// <inheritdoc/>
    public async Task<bool> LockAsync(LockInstance instance, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateLockInstance(instance);

        // Handle reentrant lock
        if (instance.Reentrant)
        {
            lock (_lockCountsLock)
            {
                if (_localLockCounts.TryGetValue(instance.Key, out var count) && count > 0)
                {
                    _localLockCounts[instance.Key] = count + 1;
                    _logger?.LogDebug("Reentrant lock acquired for key {Key}, count: {Count}", instance.Key, count + 1);
                    return true;
                }
            }
        }

        return await RemoteTryLockAsync(instance, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> UnlockAsync(LockInstance instance, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateLockInstance(instance);

        // Handle reentrant unlock
        if (instance.Reentrant)
        {
            lock (_lockCountsLock)
            {
                if (_localLockCounts.TryGetValue(instance.Key, out var count))
                {
                    if (count > 1)
                    {
                        _localLockCounts[instance.Key] = count - 1;
                        _logger?.LogDebug("Reentrant lock count decreased for key {Key}, count: {Count}", instance.Key, count - 1);
                        return true;
                    }
                }
            }
        }

        return await RemoteReleaseLockAsync(instance, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> TryLockAsync(LockInstance instance, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateLockInstance(instance);

        var deadline = DateTime.UtcNow.Add(timeout);
        var retryInterval = TimeSpan.FromMilliseconds(LockConstants.RetryInterval);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await LockAsync(instance, cancellationToken))
            {
                return true;
            }

            // Wait before retrying
            var remainingTime = deadline - DateTime.UtcNow;
            if (remainingTime <= TimeSpan.Zero)
            {
                break;
            }

            var waitTime = remainingTime < retryInterval ? remainingTime : retryInterval;
            await Task.Delay(waitTime, cancellationToken);
        }

        _logger?.LogWarning("Failed to acquire lock for key {Key} within timeout {Timeout}", instance.Key, timeout);
        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoteTryLockAsync(LockInstance instance, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateLockInstance(instance);

        try
        {
            instance.Owner ??= _clientId;
            instance.NamespaceId ??= _options.Namespace;
            instance.AcquireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (instance.ExpireTime <= 0)
            {
                instance.ExpireTime = LockConstants.DefaultExpireTime;
            }

            var request = new LockAcquireRequest
            {
                Key = instance.Key,
                ExpireTime = instance.ExpireTime,
                LockType = instance.LockType,
                NamespaceId = instance.NamespaceId,
                Owner = instance.Owner,
                Params = instance.Params
            };

            var response = await _grpcClient.RequestAsync<LockAcquireResponse>(
                LockConstants.GrpcLockAcquireType, request, cancellationToken);

            if (response?.Success == true)
            {
                lock (_lockCountsLock)
                {
                    _localLockCounts[instance.Key] = 1;
                }
                _logger?.LogInformation("Lock acquired successfully for key {Key}", instance.Key);
                return true;
            }

            _logger?.LogWarning("Failed to acquire lock for key {Key}: {Message}", instance.Key, response?.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error acquiring lock for key {Key}", instance.Key);
            throw new NacosException(NacosException.ServerError, $"Failed to acquire lock: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RemoteReleaseLockAsync(LockInstance instance, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateLockInstance(instance);

        try
        {
            instance.Owner ??= _clientId;
            instance.NamespaceId ??= _options.Namespace;

            var request = new LockReleaseRequest
            {
                Key = instance.Key,
                LockType = instance.LockType,
                NamespaceId = instance.NamespaceId,
                Owner = instance.Owner
            };

            var response = await _grpcClient.RequestAsync<LockReleaseResponse>(
                LockConstants.GrpcLockReleaseType, request, cancellationToken);

            if (response?.Success == true)
            {
                lock (_lockCountsLock)
                {
                    _localLockCounts.Remove(instance.Key);
                }
                _logger?.LogInformation("Lock released successfully for key {Key}", instance.Key);
                return true;
            }

            _logger?.LogWarning("Failed to release lock for key {Key}: {Message}", instance.Key, response?.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error releasing lock for key {Key}", instance.Key);
            throw new NacosException(NacosException.ServerError, $"Failed to release lock: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public string GetServerStatus()
    {
        return _serverStatus;
    }

    /// <inheritdoc/>
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        _logger?.LogInformation("Shutting down gRPC lock service...");

        // Release all held locks
        List<string> keysToRelease;
        lock (_lockCountsLock)
        {
            keysToRelease = _localLockCounts.Keys.ToList();
        }

        foreach (var key in keysToRelease)
        {
            try
            {
                var instance = new LockInstance { Key = key, Owner = _clientId };
                await UnlockAsync(instance, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to release lock {Key} during shutdown", key);
            }
        }

        _serverStatus = "DOWN";
        _disposed = true;

        _logger?.LogInformation("gRPC lock service shutdown completed");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await ShutdownAsync();
            await _grpcClient.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }

    private void ValidateLockInstance(LockInstance instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (string.IsNullOrWhiteSpace(instance.Key))
        {
            throw new ArgumentException("Lock key cannot be null or empty", nameof(instance));
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NacosGrpcLockService));
        }
    }

    #region gRPC Request/Response Models

    private class LockAcquireRequest
    {
        public string Key { get; set; } = string.Empty;
        public long ExpireTime { get; set; }
        public string LockType { get; set; } = LockConstants.DefaultLockType;
        public string? NamespaceId { get; set; }
        public string? Owner { get; set; }
        public Dictionary<string, object>? Params { get; set; }
    }

    private class LockAcquireResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? CurrentOwner { get; set; }
        public long RemainingTtl { get; set; }
    }

    private class LockReleaseRequest
    {
        public string Key { get; set; } = string.Empty;
        public string LockType { get; set; } = LockConstants.DefaultLockType;
        public string? NamespaceId { get; set; }
        public string? Owner { get; set; }
    }

    private class LockReleaseResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    #endregion
}
