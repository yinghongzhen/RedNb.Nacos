using System.Collections.Concurrent;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Client.Naming;

/// <summary>
/// Notifies listeners of instance changes.
/// </summary>
public class InstancesChangeNotifier
{
    private readonly ConcurrentDictionary<string, List<Action<IInstancesChangeEvent>>> _listeners = new();
    private readonly object _lock = new();

    /// <summary>
    /// Registers a listener for a service.
    /// </summary>
    public void RegisterListener(string serviceName, string groupName, string clusters, 
        Action<IInstancesChangeEvent> listener)
    {
        var key = GetKey(serviceName, groupName, clusters);
        
        lock (_lock)
        {
            if (!_listeners.TryGetValue(key, out var listeners))
            {
                listeners = new List<Action<IInstancesChangeEvent>>();
                _listeners[key] = listeners;
            }

            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }
    }

    /// <summary>
    /// Deregisters a listener for a service.
    /// </summary>
    public void DeregisterListener(string serviceName, string groupName, string clusters, 
        Action<IInstancesChangeEvent> listener)
    {
        var key = GetKey(serviceName, groupName, clusters);

        lock (_lock)
        {
            if (_listeners.TryGetValue(key, out var listeners))
            {
                listeners.Remove(listener);
                if (listeners.Count == 0)
                {
                    _listeners.TryRemove(key, out _);
                }
            }
        }
    }

    /// <summary>
    /// Notifies all listeners of an event.
    /// </summary>
    public void NotifyListeners(string serviceName, string groupName, string clusters, 
        IInstancesChangeEvent changeEvent)
    {
        var key = GetKey(serviceName, groupName, clusters);

        List<Action<IInstancesChangeEvent>>? listenersCopy;
        lock (_lock)
        {
            if (!_listeners.TryGetValue(key, out var listeners))
            {
                return;
            }
            listenersCopy = new List<Action<IInstancesChangeEvent>>(listeners);
        }

        foreach (var listener in listenersCopy)
        {
            try
            {
                listener.Invoke(changeEvent);
            }
            catch
            {
                // Ignore listener exceptions
            }
        }
    }

    /// <summary>
    /// Gets all subscribed service keys.
    /// </summary>
    public List<string> GetSubscribedServices()
    {
        return _listeners.Keys.ToList();
    }

    /// <summary>
    /// Gets subscribed service infos.
    /// </summary>
    public List<ServiceInfo> GetSubscribedServiceInfos()
    {
        return _listeners.Keys
            .Select(key =>
            {
                var parts = key.Split("@@");
                return new ServiceInfo
                {
                    GroupName = parts.Length > 0 ? parts[0] : "",
                    Name = parts.Length > 1 ? parts[1] : "",
                    Clusters = parts.Length > 2 ? parts[2] : ""
                };
            })
            .ToList();
    }

    private static string GetKey(string serviceName, string groupName, string clusters)
    {
        return $"{groupName}@@{serviceName}@@{clusters}";
    }
}
