using RedNb.Nacos.Core;

namespace RedNb.Nacos.Client.Http;

/// <summary>
/// Manages the list of Nacos servers with health tracking.
/// </summary>
public class ServerListManager
{
    private readonly List<ServerInfo> _servers;
    private readonly object _lock = new();
    private int _currentIndex;

    public ServerListManager(NacosClientOptions options)
    {
        var serverAddresses = options.GetServerAddressList();
        _servers = serverAddresses.Select(addr => new ServerInfo(addr)).ToList();
        _currentIndex = 0;
    }

    /// <summary>
    /// Gets all server addresses.
    /// </summary>
    public List<string> GetServerList()
    {
        lock (_lock)
        {
            return _servers.Select(s => s.Address).ToList();
        }
    }

    /// <summary>
    /// Gets all healthy server addresses.
    /// </summary>
    public List<string> GetHealthyServerList()
    {
        lock (_lock)
        {
            return _servers.Where(s => s.IsHealthy).Select(s => s.Address).ToList();
        }
    }

    /// <summary>
    /// Gets the next server address using round-robin.
    /// </summary>
    public string GetNextServer()
    {
        lock (_lock)
        {
            if (_servers.Count == 0)
            {
                throw new NacosException(NacosException.InvalidParam, "No available servers");
            }

            // Try to find a healthy server first
            var healthyServers = _servers.Where(s => s.IsHealthy).ToList();
            if (healthyServers.Count > 0)
            {
                var index = _currentIndex % healthyServers.Count;
                _currentIndex++;
                return healthyServers[index].Address;
            }

            // All servers are unhealthy, just round-robin through all
            var allIndex = _currentIndex % _servers.Count;
            _currentIndex++;
            return _servers[allIndex].Address;
        }
    }

    /// <summary>
    /// Marks a server as healthy.
    /// </summary>
    public void MarkServerHealthy(string address)
    {
        lock (_lock)
        {
            var server = _servers.FirstOrDefault(s => s.Address == address);
            if (server != null)
            {
                server.IsHealthy = true;
                server.LastSuccessTime = DateTimeOffset.UtcNow;
            }
        }
    }

    /// <summary>
    /// Marks a server as unhealthy.
    /// </summary>
    public void MarkServerUnhealthy(string address)
    {
        lock (_lock)
        {
            var server = _servers.FirstOrDefault(s => s.Address == address);
            if (server != null)
            {
                server.IsHealthy = false;
                server.LastFailTime = DateTimeOffset.UtcNow;
            }
        }
    }

    /// <summary>
    /// Checks if any server is healthy.
    /// </summary>
    public bool HasHealthyServer()
    {
        lock (_lock)
        {
            return _servers.Any(s => s.IsHealthy);
        }
    }

    private class ServerInfo
    {
        public string Address { get; }
        public bool IsHealthy { get; set; }
        public DateTimeOffset LastSuccessTime { get; set; }
        public DateTimeOffset LastFailTime { get; set; }

        public ServerInfo(string address)
        {
            Address = address;
            IsHealthy = true;
            LastSuccessTime = DateTimeOffset.UtcNow;
        }
    }
}
