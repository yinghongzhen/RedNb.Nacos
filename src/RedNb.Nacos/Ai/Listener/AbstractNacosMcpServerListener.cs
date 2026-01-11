namespace RedNb.Nacos.Core.Ai.Listener;

/// <summary>
/// Abstract base class for MCP server event listeners.
/// </summary>
public abstract class AbstractNacosMcpServerListener : INacosAiListener<NacosMcpServerEvent>
{
    /// <summary>
    /// Callback when an MCP server event is received.
    /// </summary>
    /// <param name="event">The MCP server event.</param>
    public abstract void OnEvent(NacosMcpServerEvent @event);
}
