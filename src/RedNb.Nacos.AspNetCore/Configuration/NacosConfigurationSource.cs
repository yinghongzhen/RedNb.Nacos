using Microsoft.Extensions.Configuration;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Config;

namespace RedNb.Nacos.AspNetCore.Configuration;

/// <summary>
/// Nacos configuration source for Microsoft.Extensions.Configuration.
/// </summary>
public class NacosConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the Nacos client options.
    /// </summary>
    public NacosClientOptions Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration items to load from Nacos.
    /// </summary>
    public List<NacosConfigurationItem> ConfigItems { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to reload configuration when changes are detected.
    /// </summary>
    public bool ReloadOnChange { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional flag - if true, configuration loading failure won't throw exception.
    /// </summary>
    public bool Optional { get; set; } = false;

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new NacosConfigurationProvider(this);
    }
}

/// <summary>
/// Represents a configuration item to load from Nacos.
/// </summary>
public class NacosConfigurationItem
{
    /// <summary>
    /// Gets or sets the data ID.
    /// </summary>
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the group. Defaults to "DEFAULT_GROUP".
    /// </summary>
    public string Group { get; set; } = "DEFAULT_GROUP";

    /// <summary>
    /// Gets or sets the configuration type (json, yaml, properties, xml, text).
    /// </summary>
    public string ConfigType { get; set; } = "json";

    /// <summary>
    /// Gets or sets whether this configuration item is optional.
    /// </summary>
    public bool Optional { get; set; } = false;
}
