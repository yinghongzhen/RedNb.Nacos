using RedNb.Nacos.Core.Ai.Model.A2a;

namespace RedNb.Nacos.Core.Ai.Listener;

/// <summary>
/// Event for agent card changes.
/// </summary>
public class NacosAgentCardEvent : INacosAiEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NacosAgentCardEvent"/> class.
    /// </summary>
    /// <param name="agentCard">The agent card detail info.</param>
    public NacosAgentCardEvent(AgentCardDetailInfo agentCard)
    {
        AgentName = agentCard.Name ?? string.Empty;
        AgentCard = agentCard;
    }

    /// <summary>
    /// Gets the agent name.
    /// </summary>
    public string AgentName { get; }

    /// <summary>
    /// Gets the agent card detail info.
    /// </summary>
    public AgentCardDetailInfo AgentCard { get; }
}
