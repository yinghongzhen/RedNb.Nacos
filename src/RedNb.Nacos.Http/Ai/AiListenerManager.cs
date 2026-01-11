using System.Collections.Concurrent;
using RedNb.Nacos.Core.Ai.Listener;
using RedNb.Nacos.Core.Ai.Model.A2a;
using RedNb.Nacos.Core.Ai.Model.Mcp;

namespace RedNb.Nacos.Client.Ai;

/// <summary>
/// Manages AI service listeners for MCP servers and Agent cards.
/// </summary>
public class AiListenerManager
{
    private readonly ConcurrentDictionary<string, HashSet<AbstractNacosMcpServerListener>> _mcpListeners = new();
    private readonly ConcurrentDictionary<string, HashSet<AbstractNacosAgentCardListener>> _agentCardListeners = new();
    private readonly object _mcpLock = new();
    private readonly object _agentLock = new();

    /// <summary>
    /// Generates a cache key for MCP server subscriptions.
    /// </summary>
    public static string BuildMcpKey(string mcpName, string? version)
    {
        return $"{mcpName}@@{version ?? string.Empty}";
    }

    /// <summary>
    /// Generates a cache key for agent card subscriptions.
    /// </summary>
    public static string BuildAgentKey(string agentName, string? version)
    {
        return $"{agentName}@@{version ?? string.Empty}";
    }

    #region MCP Server Listeners

    /// <summary>
    /// Adds an MCP server listener.
    /// </summary>
    public void AddMcpListener(string mcpName, string? version, AbstractNacosMcpServerListener listener)
    {
        var key = BuildMcpKey(mcpName, version);
        lock (_mcpLock)
        {
            if (!_mcpListeners.TryGetValue(key, out var listeners))
            {
                listeners = new HashSet<AbstractNacosMcpServerListener>();
                _mcpListeners[key] = listeners;
            }
            listeners.Add(listener);
        }
    }

    /// <summary>
    /// Removes an MCP server listener.
    /// </summary>
    public bool RemoveMcpListener(string mcpName, string? version, AbstractNacosMcpServerListener listener)
    {
        var key = BuildMcpKey(mcpName, version);
        lock (_mcpLock)
        {
            if (_mcpListeners.TryGetValue(key, out var listeners))
            {
                listeners.Remove(listener);
                if (listeners.Count == 0)
                {
                    _mcpListeners.TryRemove(key, out _);
                    return true; // No more listeners, should unsubscribe
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Gets all MCP server listeners for a specific key.
    /// </summary>
    public IReadOnlyCollection<AbstractNacosMcpServerListener> GetMcpListeners(string mcpName, string? version)
    {
        var key = BuildMcpKey(mcpName, version);
        lock (_mcpLock)
        {
            if (_mcpListeners.TryGetValue(key, out var listeners))
            {
                return listeners.ToList();
            }
        }
        return Array.Empty<AbstractNacosMcpServerListener>();
    }

    /// <summary>
    /// Notifies all MCP server listeners of a change.
    /// </summary>
    public void NotifyMcpListeners(string mcpName, string? version, McpServerDetailInfo serverInfo)
    {
        var listeners = GetMcpListeners(mcpName, version);
        var evt = new NacosMcpServerEvent(serverInfo);
        
        foreach (var listener in listeners)
        {
            try
            {
                listener.OnEvent(evt);
            }
            catch
            {
                // Ignore listener exceptions
            }
        }
    }

    /// <summary>
    /// Checks if there are any MCP server listeners for a specific key.
    /// </summary>
    public bool HasMcpListeners(string mcpName, string? version)
    {
        var key = BuildMcpKey(mcpName, version);
        lock (_mcpLock)
        {
            return _mcpListeners.TryGetValue(key, out var listeners) && listeners.Count > 0;
        }
    }

    #endregion

    #region Agent Card Listeners

    /// <summary>
    /// Adds an agent card listener.
    /// </summary>
    public void AddAgentListener(string agentName, string? version, AbstractNacosAgentCardListener listener)
    {
        var key = BuildAgentKey(agentName, version);
        lock (_agentLock)
        {
            if (!_agentCardListeners.TryGetValue(key, out var listeners))
            {
                listeners = new HashSet<AbstractNacosAgentCardListener>();
                _agentCardListeners[key] = listeners;
            }
            listeners.Add(listener);
        }
    }

    /// <summary>
    /// Removes an agent card listener.
    /// </summary>
    public bool RemoveAgentListener(string agentName, string? version, AbstractNacosAgentCardListener listener)
    {
        var key = BuildAgentKey(agentName, version);
        lock (_agentLock)
        {
            if (_agentCardListeners.TryGetValue(key, out var listeners))
            {
                listeners.Remove(listener);
                if (listeners.Count == 0)
                {
                    _agentCardListeners.TryRemove(key, out _);
                    return true; // No more listeners, should unsubscribe
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Gets all agent card listeners for a specific key.
    /// </summary>
    public IReadOnlyCollection<AbstractNacosAgentCardListener> GetAgentListeners(string agentName, string? version)
    {
        var key = BuildAgentKey(agentName, version);
        lock (_agentLock)
        {
            if (_agentCardListeners.TryGetValue(key, out var listeners))
            {
                return listeners.ToList();
            }
        }
        return Array.Empty<AbstractNacosAgentCardListener>();
    }

    /// <summary>
    /// Notifies all agent card listeners of a change.
    /// </summary>
    public void NotifyAgentListeners(string agentName, string? version, AgentCardDetailInfo agentCard)
    {
        var listeners = GetAgentListeners(agentName, version);
        var evt = new NacosAgentCardEvent(agentCard);
        
        foreach (var listener in listeners)
        {
            try
            {
                listener.OnEvent(evt);
            }
            catch
            {
                // Ignore listener exceptions
            }
        }
    }

    /// <summary>
    /// Checks if there are any agent card listeners for a specific key.
    /// </summary>
    public bool HasAgentListeners(string agentName, string? version)
    {
        var key = BuildAgentKey(agentName, version);
        lock (_agentLock)
        {
            return _agentCardListeners.TryGetValue(key, out var listeners) && listeners.Count > 0;
        }
    }

    #endregion

    /// <summary>
    /// Gets all subscription keys for polling.
    /// </summary>
    public IReadOnlyList<(string Name, string? Version, bool IsMcp)> GetAllSubscriptions()
    {
        var result = new List<(string, string?, bool)>();
        
        lock (_mcpLock)
        {
            foreach (var key in _mcpListeners.Keys)
            {
                var parts = key.Split("@@");
                var name = parts[0];
                var version = parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) ? parts[1] : null;
                result.Add((name, version, true));
            }
        }
        
        lock (_agentLock)
        {
            foreach (var key in _agentCardListeners.Keys)
            {
                var parts = key.Split("@@");
                var name = parts[0];
                var version = parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) ? parts[1] : null;
                result.Add((name, version, false));
            }
        }
        
        return result;
    }

    /// <summary>
    /// Clears all listeners.
    /// </summary>
    public void Clear()
    {
        lock (_mcpLock)
        {
            _mcpListeners.Clear();
        }
        
        lock (_agentLock)
        {
            _agentCardListeners.Clear();
        }
    }
}
