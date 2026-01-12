using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Lock;

namespace RedNb.Nacos.Http.Lock;

/// <summary>
/// HTTP implementation of the Nacos lock service.
/// </summary>
public class NacosLockService : ILockService
{
    private readonly NacosClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<NacosLockService>? _logger;
    private readonly string _clientId;
    private volatile bool _disposed;
    private volatile string _serverStatus = "UP";

    // Local lock tracking for reentrant support
    private readonly Dictionary<string, int> _localLockCounts = new();
    private readonly object _lockCountsLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NacosLockService"/> class.
    /// </summary>
    /// <param name="options">The Nacos client options.</param>
    /// <param name="logger">The logger.</param>
    public NacosLockService(NacosClientOptions options, ILogger<NacosLockService>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _clientId = Guid.NewGuid().ToString("N");

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(GetServerAddress()),
            Timeout = TimeSpan.FromMilliseconds(_options.DefaultTimeout > 0 ? _options.DefaultTimeout : 10000)
        };
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

        try
        {
            instance.Owner ??= _clientId;
            instance.NamespaceId ??= _options.Namespace;
            instance.AcquireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (instance.ExpireTime <= 0)
            {
                instance.ExpireTime = LockConstants.DefaultExpireTime;
            }

            var response = await _httpClient.PostAsJsonAsync(
                LockConstants.LockAcquirePath,
                instance,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LockResult>(cancellationToken: cancellationToken);
                if (result?.Success == true)
                {
                    lock (_lockCountsLock)
                    {
                        _localLockCounts[instance.Key] = 1;
                    }
                    _logger?.LogInformation("Lock acquired successfully for key {Key}", instance.Key);
                    return true;
                }
                _logger?.LogWarning("Failed to acquire lock for key {Key}: {Message}", instance.Key, result?.Message);
                return false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger?.LogWarning("Lock acquire request failed for key {Key}: {StatusCode} - {Error}",
                instance.Key, response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error acquiring lock for key {Key}", instance.Key);
            throw new NacosException(NacosException.ServerError, $"Failed to acquire lock: {ex.Message}", ex);
        }
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

        try
        {
            instance.Owner ??= _clientId;
            instance.NamespaceId ??= _options.Namespace;

            var response = await _httpClient.PostAsJsonAsync(
                LockConstants.LockReleasePath,
                instance,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LockResult>(cancellationToken: cancellationToken);
                if (result?.Success == true)
                {
                    lock (_lockCountsLock)
                    {
                        _localLockCounts.Remove(instance.Key);
                    }
                    _logger?.LogInformation("Lock released successfully for key {Key}", instance.Key);
                    return true;
                }
                _logger?.LogWarning("Failed to release lock for key {Key}: {Message}", instance.Key, result?.Message);
                return false;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger?.LogWarning("Lock release request failed for key {Key}: {StatusCode} - {Error}",
                instance.Key, response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error releasing lock for key {Key}", instance.Key);
            throw new NacosException(NacosException.ServerError, $"Failed to release lock: {ex.Message}", ex);
        }
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
    public Task<bool> RemoteTryLockAsync(LockInstance instance, CancellationToken cancellationToken = default)
    {
        // HTTP implementation delegates to LockAsync
        return LockAsync(instance, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> RemoteReleaseLockAsync(LockInstance instance, CancellationToken cancellationToken = default)
    {
        // HTTP implementation delegates to UnlockAsync
        return UnlockAsync(instance, cancellationToken);
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

        _logger?.LogInformation("Shutting down lock service...");

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

        _logger?.LogInformation("Lock service shutdown completed");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await ShutdownAsync();
            _httpClient.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    private string GetServerAddress()
    {
        var addresses = _options.ServerAddresses?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (addresses == null || addresses.Length == 0)
        {
            throw new ArgumentException("Server addresses not configured");
        }

        var address = addresses[0].Trim();
        if (!address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            address = "http://" + address;
        }

        return address;
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
            throw new ObjectDisposedException(nameof(NacosLockService));
        }
    }
}
