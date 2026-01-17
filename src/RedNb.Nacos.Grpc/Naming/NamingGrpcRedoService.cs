using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.GrpcClient.Naming;

/// <summary>
/// Manages redo operations for naming service after reconnection.
/// Ensures registered instances and subscriptions are restored.
/// </summary>
internal class NamingGrpcRedoService : IAsyncDisposable
{
    private readonly NamingRpcTransportClient _transportClient;
    private readonly ILogger? _logger;
    private readonly string? _namespace;

    // Registered instances for redo
    private readonly ConcurrentDictionary<string, InstanceRedoData> _registeredInstances = new();

    // Subscribed services for redo
    private readonly ConcurrentDictionary<string, SubscribeRedoData> _subscribedServices = new();

    // Batch registered instances for redo
    private readonly ConcurrentDictionary<string, BatchInstanceRedoData> _batchRegisteredInstances = new();

    private readonly SemaphoreSlim _redoLock = new(1, 1);
    private bool _disposed;

    public NamingGrpcRedoService(NamingRpcTransportClient transportClient, string? ns, ILogger? logger = null)
    {
        _transportClient = transportClient;
        _namespace = ns;
        _logger = logger;
    }

    #region Instance Registration Redo

    /// <summary>
    /// Caches a registered instance for redo.
    /// </summary>
    public void CacheRegisteredInstance(string serviceName, string groupName, Instance instance)
    {
        var key = GetInstanceKey(serviceName, groupName, instance);
        _registeredInstances[key] = new InstanceRedoData
        {
            ServiceName = serviceName,
            GroupName = groupName,
            Instance = NamingServiceInfoHolder.MapToNamingInstance(instance),
            RegisterTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Removes a registered instance from redo cache.
    /// </summary>
    public void RemoveRegisteredInstance(string serviceName, string groupName, Instance instance)
    {
        var key = GetInstanceKey(serviceName, groupName, instance);
        _registeredInstances.TryRemove(key, out _);
    }

    /// <summary>
    /// Caches batch registered instances for redo.
    /// </summary>
    public void CacheBatchRegisteredInstances(string serviceName, string groupName, List<Instance> instances)
    {
        var key = GetServiceKey(serviceName, groupName);
        _batchRegisteredInstances[key] = new BatchInstanceRedoData
        {
            ServiceName = serviceName,
            GroupName = groupName,
            Instances = instances.Select(NamingServiceInfoHolder.MapToNamingInstance).ToList(),
            RegisterTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Removes batch registered instances from redo cache.
    /// </summary>
    public void RemoveBatchRegisteredInstances(string serviceName, string groupName)
    {
        var key = GetServiceKey(serviceName, groupName);
        _batchRegisteredInstances.TryRemove(key, out _);
    }

    #endregion

    #region Subscription Redo

    /// <summary>
    /// Caches a subscribed service for redo.
    /// </summary>
    public void CacheSubscribedService(string serviceName, string groupName, string? clusters)
    {
        var key = GetSubscribeKey(serviceName, groupName, clusters);
        _subscribedServices[key] = new SubscribeRedoData
        {
            ServiceName = serviceName,
            GroupName = groupName,
            Clusters = clusters,
            SubscribeTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Removes a subscribed service from redo cache.
    /// </summary>
    public void RemoveSubscribedService(string serviceName, string groupName, string? clusters)
    {
        var key = GetSubscribeKey(serviceName, groupName, clusters);
        _subscribedServices.TryRemove(key, out _);
    }

    /// <summary>
    /// Gets all subscribed services.
    /// </summary>
    public IEnumerable<SubscribeRedoData> GetSubscribedServices()
    {
        return _subscribedServices.Values;
    }

    #endregion

    #region Redo Execution

    /// <summary>
    /// Executes all redo operations after reconnection.
    /// </summary>
    public async Task RedoAsync(CancellationToken cancellationToken = default)
    {
        await _redoLock.WaitAsync(cancellationToken);
        try
        {
            _logger?.LogInformation("Starting redo operations after reconnection");

            // Redo instance registrations
            await RedoInstanceRegistrationsAsync(cancellationToken);

            // Redo batch instance registrations
            await RedoBatchInstanceRegistrationsAsync(cancellationToken);

            // Redo service subscriptions
            await RedoServiceSubscriptionsAsync(cancellationToken);

            _logger?.LogInformation("Completed redo operations");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during redo operations");
            throw;
        }
        finally
        {
            _redoLock.Release();
        }
    }

    private async Task RedoInstanceRegistrationsAsync(CancellationToken cancellationToken)
    {
        foreach (var kvp in _registeredInstances)
        {
            try
            {
                var data = kvp.Value;
                var success = await _transportClient.RegisterInstanceAsync(
                    data.ServiceName, data.GroupName, _namespace,
                    data.Instance, cancellationToken);

                if (success)
                {
                    _logger?.LogDebug("Redo: Re-registered instance {Ip}:{Port} for {Service}@{Group}",
                        data.Instance.Ip, data.Instance.Port, data.ServiceName, data.GroupName);
                }
                else
                {
                    _logger?.LogWarning("Redo: Failed to re-register instance {Ip}:{Port} for {Service}@{Group}",
                        data.Instance.Ip, data.Instance.Port, data.ServiceName, data.GroupName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Redo: Error re-registering instance for key {Key}", kvp.Key);
            }
        }
    }

    private async Task RedoBatchInstanceRegistrationsAsync(CancellationToken cancellationToken)
    {
        foreach (var kvp in _batchRegisteredInstances)
        {
            try
            {
                var data = kvp.Value;
                var success = await _transportClient.BatchRegisterInstanceAsync(
                    data.ServiceName, data.GroupName, _namespace,
                    data.Instances, cancellationToken);

                if (success)
                {
                    _logger?.LogDebug("Redo: Re-registered {Count} instances for {Service}@{Group}",
                        data.Instances.Count, data.ServiceName, data.GroupName);
                }
                else
                {
                    _logger?.LogWarning("Redo: Failed to re-register batch instances for {Service}@{Group}",
                        data.ServiceName, data.GroupName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Redo: Error re-registering batch instances for key {Key}", kvp.Key);
            }
        }
    }

    private async Task RedoServiceSubscriptionsAsync(CancellationToken cancellationToken)
    {
        foreach (var kvp in _subscribedServices)
        {
            try
            {
                var data = kvp.Value;
                var serviceInfo = await _transportClient.SubscribeServiceAsync(
                    data.ServiceName, data.GroupName, _namespace,
                    data.Clusters, cancellationToken);

                if (serviceInfo != null)
                {
                    _logger?.LogDebug("Redo: Re-subscribed to {Service}@{Group}",
                        data.ServiceName, data.GroupName);
                }
                else
                {
                    _logger?.LogWarning("Redo: Failed to re-subscribe to {Service}@{Group}",
                        data.ServiceName, data.GroupName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Redo: Error re-subscribing to key {Key}", kvp.Key);
            }
        }
    }

    #endregion

    #region Helpers

    private static string GetInstanceKey(string serviceName, string groupName, Instance instance)
    {
        return $"{groupName}@@{serviceName}@@{instance.Ip}@@{instance.Port}";
    }

    private static string GetServiceKey(string serviceName, string groupName)
    {
        return $"{groupName}@@{serviceName}";
    }

    private static string GetSubscribeKey(string serviceName, string groupName, string? clusters)
    {
        return $"{groupName}@@{serviceName}@@{clusters ?? ""}";
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _registeredInstances.Clear();
        _subscribedServices.Clear();
        _batchRegisteredInstances.Clear();
        _redoLock.Dispose();
    }
}

#region Redo Data Classes

/// <summary>
/// Data for instance registration redo.
/// </summary>
internal class InstanceRedoData
{
    public string ServiceName { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public NamingInstance Instance { get; init; } = new();
    public DateTime RegisterTime { get; init; }
}

/// <summary>
/// Data for batch instance registration redo.
/// </summary>
internal class BatchInstanceRedoData
{
    public string ServiceName { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public List<NamingInstance> Instances { get; init; } = new();
    public DateTime RegisterTime { get; init; }
}

/// <summary>
/// Data for service subscription redo.
/// </summary>
internal class SubscribeRedoData
{
    public string ServiceName { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public string? Clusters { get; init; }
    public DateTime SubscribeTime { get; init; }
}

#endregion
