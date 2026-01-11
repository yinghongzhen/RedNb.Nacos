using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.A2a;

/// <summary>
/// Agent card with full A2A protocol information.
/// </summary>
public class AgentCard : AgentCardBasicInfo
{
    /// <summary>
    /// Gets or sets the agent URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the preferred transport type.
    /// </summary>
    [JsonPropertyName("preferredTransport")]
    public string? PreferredTransport { get; set; }

    /// <summary>
    /// Gets or sets the additional interfaces.
    /// </summary>
    [JsonPropertyName("additionalInterfaces")]
    public List<AgentInterface>? AdditionalInterfaces { get; set; }

    /// <summary>
    /// Gets or sets the provider information.
    /// </summary>
    [JsonPropertyName("provider")]
    public AgentProvider? Provider { get; set; }

    /// <summary>
    /// Gets or sets the documentation URL.
    /// </summary>
    [JsonPropertyName("documentationUrl")]
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// Gets or sets the security schemes.
    /// </summary>
    [JsonPropertyName("securitySchemes")]
    public Dictionary<string, SecurityScheme>? SecuritySchemes { get; set; }

    /// <summary>
    /// Gets or sets the security requirements.
    /// </summary>
    [JsonPropertyName("security")]
    public List<Dictionary<string, List<string>>>? Security { get; set; }

    /// <summary>
    /// Gets or sets the default input modes.
    /// </summary>
    [JsonPropertyName("defaultInputModes")]
    public List<string>? DefaultInputModes { get; set; }

    /// <summary>
    /// Gets or sets the default output modes.
    /// </summary>
    [JsonPropertyName("defaultOutputModes")]
    public List<string>? DefaultOutputModes { get; set; }

    /// <summary>
    /// Gets or sets whether authenticated extended card is supported.
    /// </summary>
    [JsonPropertyName("supportsAuthenticatedExtendedCard")]
    public bool? SupportsAuthenticatedExtendedCard { get; set; }
}
