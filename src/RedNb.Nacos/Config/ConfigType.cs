namespace RedNb.Nacos.Core.Config;

/// <summary>
/// Configuration types supported by Nacos.
/// </summary>
public static class ConfigType
{
    /// <summary>
    /// Properties format.
    /// </summary>
    public const string Properties = "properties";

    /// <summary>
    /// XML format.
    /// </summary>
    public const string Xml = "xml";

    /// <summary>
    /// JSON format.
    /// </summary>
    public const string Json = "json";

    /// <summary>
    /// Plain text format.
    /// </summary>
    public const string Text = "text";

    /// <summary>
    /// HTML format.
    /// </summary>
    public const string Html = "html";

    /// <summary>
    /// YAML format.
    /// </summary>
    public const string Yaml = "yaml";

    /// <summary>
    /// TOML format.
    /// </summary>
    public const string Toml = "toml";

    /// <summary>
    /// Default config type.
    /// </summary>
    public const string Default = Text;

    /// <summary>
    /// Gets the config type from file extension.
    /// </summary>
    public static string GetTypeByExtension(string extension)
    {
        return extension.ToLowerInvariant().TrimStart('.') switch
        {
            "properties" => Properties,
            "xml" => Xml,
            "json" => Json,
            "txt" => Text,
            "html" or "htm" => Html,
            "yaml" or "yml" => Yaml,
            "toml" => Toml,
            _ => Text
        };
    }

    /// <summary>
    /// Checks if the config type is valid.
    /// </summary>
    public static bool IsValidType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            Properties or Xml or Json or Text or Html or Yaml or Toml => true,
            _ => false
        };
    }
}
