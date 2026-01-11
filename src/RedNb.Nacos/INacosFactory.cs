using RedNb.Nacos.Core.Ai;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Core;

/// <summary>
/// Factory interface for creating Nacos services.
/// </summary>
public interface INacosFactory
{
    /// <summary>
    /// Creates a config service.
    /// </summary>
    /// <param name="options">Client options</param>
    /// <returns>Config service instance</returns>
    IConfigService CreateConfigService(NacosClientOptions options);

    /// <summary>
    /// Creates a config service with server address.
    /// </summary>
    /// <param name="serverAddr">Server address</param>
    /// <returns>Config service instance</returns>
    IConfigService CreateConfigService(string serverAddr);

    /// <summary>
    /// Creates a naming service.
    /// </summary>
    /// <param name="options">Client options</param>
    /// <returns>Naming service instance</returns>
    INamingService CreateNamingService(NacosClientOptions options);

    /// <summary>
    /// Creates a naming service with server address.
    /// </summary>
    /// <param name="serverAddr">Server address</param>
    /// <returns>Naming service instance</returns>
    INamingService CreateNamingService(string serverAddr);

    /// <summary>
    /// Creates an AI service for A2A and MCP operations.
    /// </summary>
    /// <param name="options">Client options</param>
    /// <returns>AI service instance</returns>
    IAiService CreateAiService(NacosClientOptions options);

    /// <summary>
    /// Creates an AI service with server address.
    /// </summary>
    /// <param name="serverAddr">Server address</param>
    /// <returns>AI service instance</returns>
    IAiService CreateAiService(string serverAddr);
}
