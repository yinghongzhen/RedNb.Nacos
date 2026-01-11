using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.A2a;

/// <summary>
/// Agent card detail info with Nacos extension fields.
/// </summary>
public class AgentCardDetailInfo : AgentCard
{
    /// <summary>
    /// Gets or sets the registration type (URL or SERVICE).
    /// </summary>
    [JsonPropertyName("registrationType")]
    public string RegistrationType { get; set; } = AiConstants.A2a.EndpointTypeUrl;

    /// <summary>
    /// Gets or sets whether this is the latest version.
    /// </summary>
    [JsonPropertyName("latestVersion")]
    public bool? LatestVersion { get; set; }
}
