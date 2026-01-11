namespace RedNb.Nacos.Core.Ai.Listener;

/// <summary>
/// Abstract base class for agent card event listeners.
/// </summary>
public abstract class AbstractNacosAgentCardListener : INacosAiListener<NacosAgentCardEvent>
{
    /// <summary>
    /// Callback when an agent card event is received.
    /// </summary>
    /// <param name="event">The agent card event.</param>
    public abstract void OnEvent(NacosAgentCardEvent @event);
}
