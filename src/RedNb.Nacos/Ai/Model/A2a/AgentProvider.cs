using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.A2a;

/// <summary>
/// Agent provider information.
/// </summary>
public class AgentProvider
{
    /// <summary>
    /// Gets or sets the organization name.
    /// </summary>
    [JsonPropertyName("organization")]
    public string? Organization { get; set; }

    /// <summary>
    /// Gets or sets the provider URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
