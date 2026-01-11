using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Ai.Model.Mcp;

/// <summary>
/// MCP server basic information.
/// </summary>
public class McpServerBasicInfo
{
    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    [JsonPropertyName("namespaceId")]
    public string? NamespaceId { get; set; }

    /// <summary>
    /// Gets or sets the MCP server ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the MCP server name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the protocol type (stdio, mcp-sse, mcp-streamable, http, dubbo).
    /// </summary>
    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    /// <summary>
    /// Gets or sets the front-end protocol.
    /// </summary>
    [JsonPropertyName("frontProtocol")]
    public string? FrontProtocol { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the repository information.
    /// </summary>
    [JsonPropertyName("repository")]
    public Repository? Repository { get; set; }

    /// <summary>
    /// Gets or sets the list of packages.
    /// </summary>
    [JsonPropertyName("packages")]
    public List<Package>? Packages { get; set; }

    /// <summary>
    /// Gets or sets the list of icons.
    /// </summary>
    [JsonPropertyName("icons")]
    public List<Icon>? Icons { get; set; }

    /// <summary>
    /// Gets or sets the website URL.
    /// </summary>
    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the version detail.
    /// </summary>
    [JsonPropertyName("versionDetail")]
    public ServerVersionDetail? VersionDetail { get; set; }

    /// <summary>
    /// Gets or sets the version (deprecated, use VersionDetail instead).
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the remote server configuration.
    /// Should be set when protocol is not "stdio".
    /// </summary>
    [JsonPropertyName("remoteServerConfig")]
    public McpServerRemoteServiceConfig? RemoteServerConfig { get; set; }

    /// <summary>
    /// Gets or sets the local server configuration.
    /// Should be set when protocol is "stdio".
    /// </summary>
    [JsonPropertyName("localServerConfig")]
    public Dictionary<string, object>? LocalServerConfig { get; set; }

    /// <summary>
    /// Gets or sets whether the server is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the lifecycle status (ACTIVE, DEPRECATED, DELETED).
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = AiConstants.Mcp.StatusActive;

    /// <summary>
    /// Gets or sets the capabilities (auto-discovered by Nacos).
    /// </summary>
    [JsonPropertyName("capabilities")]
    public List<McpCapability>? Capabilities { get; set; }
}
