using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// Package information for MCP server.
/// </summary>
public class Package
{
    /// <summary>
    /// Gets or sets the registry type.
    /// </summary>
    [JsonPropertyName("registry_type")]
    public string? RegistryType { get; set; }

    /// <summary>
    /// Gets or sets the registry name.
    /// </summary>
    [JsonPropertyName("registry_name")]
    public string? RegistryName { get; set; }

    /// <summary>
    /// Gets or sets the package name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the runtime environment.
    /// </summary>
    [JsonPropertyName("runtime")]
    public string? Runtime { get; set; }
}
