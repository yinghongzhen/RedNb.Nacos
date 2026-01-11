using System.Collections.Concurrent;
using RedNb.Nacos.Core.Ai.Model.A2a;
using RedNb.Nacos.Core.Ai.Model.Mcp;

namespace RedNb.Nacos.Client.Ai;

/// <summary>
/// Holds cached MCP server and Agent card data.
/// </summary>
public class AiCacheHolder
{
    private readonly ConcurrentDictionary<string, McpServerDetailInfo> _mcpCache = new();
    private readonly ConcurrentDictionary<string, AgentCardDetailInfo> _agentCache = new();

    #region MCP Server Cache

    /// <summary>
    /// Gets cached MCP server info.
    /// </summary>
    public McpServerDetailInfo? GetMcpServer(string mcpName, string? version)
    {
        var key = AiListenerManager.BuildMcpKey(mcpName, version);
        return _mcpCache.TryGetValue(key, out var info) ? info : null;
    }

    /// <summary>
    /// Updates cached MCP server info.
    /// </summary>
    public void UpdateMcpServer(string mcpName, string? version, McpServerDetailInfo? info)
    {
        var key = AiListenerManager.BuildMcpKey(mcpName, version);
        if (info != null)
        {
            _mcpCache[key] = info;
        }
        else
        {
            _mcpCache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Removes cached MCP server info.
    /// </summary>
    public void RemoveMcpServer(string mcpName, string? version)
    {
        var key = AiListenerManager.BuildMcpKey(mcpName, version);
        _mcpCache.TryRemove(key, out _);
    }

    /// <summary>
    /// Gets all cached MCP server keys.
    /// </summary>
    public IReadOnlyList<string> GetMcpKeys()
    {
        return _mcpCache.Keys.ToList();
    }

    #endregion

    #region Agent Card Cache

    /// <summary>
    /// Gets cached agent card info.
    /// </summary>
    public AgentCardDetailInfo? GetAgentCard(string agentName, string? version)
    {
        var key = AiListenerManager.BuildAgentKey(agentName, version);
        return _agentCache.TryGetValue(key, out var info) ? info : null;
    }

    /// <summary>
    /// Updates cached agent card info.
    /// </summary>
    public void UpdateAgentCard(string agentName, string? version, AgentCardDetailInfo? info)
    {
        var key = AiListenerManager.BuildAgentKey(agentName, version);
        if (info != null)
        {
            _agentCache[key] = info;
        }
        else
        {
            _agentCache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Removes cached agent card info.
    /// </summary>
    public void RemoveAgentCard(string agentName, string? version)
    {
        var key = AiListenerManager.BuildAgentKey(agentName, version);
        _agentCache.TryRemove(key, out _);
    }

    /// <summary>
    /// Gets all cached agent card keys.
    /// </summary>
    public IReadOnlyList<string> GetAgentKeys()
    {
        return _agentCache.Keys.ToList();
    }

    #endregion

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    public void Clear()
    {
        _mcpCache.Clear();
        _agentCache.Clear();
    }
}
