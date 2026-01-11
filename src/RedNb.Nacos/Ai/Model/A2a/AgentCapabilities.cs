using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.A2a;

/// <summary>
/// Agent capabilities information.
/// </summary>
public class AgentCapabilities
{
    /// <summary>
    /// Gets or sets whether streaming is supported.
    /// </summary>
    [JsonPropertyName("streaming")]
    public bool? Streaming { get; set; }

    /// <summary>
    /// Gets or sets whether push notifications are supported.
    /// </summary>
    [JsonPropertyName("pushNotifications")]
    public bool? PushNotifications { get; set; }

    /// <summary>
    /// Gets or sets whether state transition history is supported.
    /// </summary>
    [JsonPropertyName("stateTransitionHistory")]
    public bool? StateTransitionHistory { get; set; }

    /// <summary>
    /// Gets or sets the list of extensions.
    /// </summary>
    [JsonPropertyName("extensions")]
    public List<AgentExtension>? Extensions { get; set; }
}
