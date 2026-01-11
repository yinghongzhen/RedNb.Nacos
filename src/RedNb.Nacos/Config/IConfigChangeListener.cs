namespace RedNb.Nacos.Core.Config;

/// <summary>
/// Configuration change listener interface.
/// </summary>
public interface IConfigChangeListener
{
    /// <summary>
    /// Called when configuration changes.
    /// </summary>
    /// <param name="configInfo">Configuration info containing the new config value</param>
    void OnReceiveConfigInfo(ConfigInfo configInfo);
}

/// <summary>
/// Config info containing the configuration content.
/// </summary>
public class ConfigInfo
{
    /// <summary>
    /// Data ID.
    /// </summary>
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Group name.
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Tenant/Namespace.
    /// </summary>
    public string? Tenant { get; set; }

    /// <summary>
    /// Config content.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Config type (e.g., json, yaml, properties).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// MD5 hash of the content.
    /// </summary>
    public string? Md5 { get; set; }

    /// <summary>
    /// Encrypted data key (for encrypted configs).
    /// </summary>
    public string? EncryptedDataKey { get; set; }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    public long LastModified { get; set; }
}
