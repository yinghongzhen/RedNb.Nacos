using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.A2a;

/// <summary>
/// Basic information of an agent card.
/// </summary>
public class AgentCardBasicInfo
{
    /// <summary>
    /// Gets or sets the protocol version.
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public string? ProtocolVersion { get; set; }

    /// <summary>
    /// Gets or sets the agent name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the agent description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the agent version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the icon URL.
    /// </summary>
    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    /// <summary>
    /// Gets or sets the agent capabilities.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public AgentCapabilities? Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the list of agent skills.
    /// </summary>
    [JsonPropertyName("skills")]
    public List<AgentSkill>? Skills { get; set; }
}
