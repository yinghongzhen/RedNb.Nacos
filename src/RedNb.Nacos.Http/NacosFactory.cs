using Microsoft.Extensions.Logging;
using RedNb.Nacos.Client.Ai;
using RedNb.Nacos.Client.Config;
using RedNb.Nacos.Client.Naming;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Ai;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Client;

/// <summary>
/// Factory for creating Nacos service instances.
/// </summary>
public class NacosFactory : INacosFactory
{
    private readonly ILoggerFactory? _loggerFactory;

    public NacosFactory(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates a config service with the specified options.
    /// </summary>
    public IConfigService CreateConfigService(NacosClientOptions options)
    {
        options.Validate();
        var logger = _loggerFactory?.CreateLogger<NacosConfigService>();
        return new NacosConfigService(options, logger);
    }

    /// <summary>
    /// Creates a config service with server address.
    /// </summary>
    public IConfigService CreateConfigService(string serverAddr)
    {
        return CreateConfigService(new NacosClientOptions { ServerAddresses = serverAddr });
    }

    /// <summary>
    /// Creates a naming service with the specified options.
    /// </summary>
    public INamingService CreateNamingService(NacosClientOptions options)
    {
        options.Validate();
        var logger = _loggerFactory?.CreateLogger<NacosNamingService>();
        return new NacosNamingService(options, logger);
    }

    /// <summary>
    /// Creates a naming service with server address.
    /// </summary>
    public INamingService CreateNamingService(string serverAddr)
    {
        return CreateNamingService(new NacosClientOptions { ServerAddresses = serverAddr });
    }

    /// <summary>
    /// Creates an AI service with the specified options.
    /// </summary>
    public IAiService CreateAiService(NacosClientOptions options)
    {
        options.Validate();
        var logger = _loggerFactory?.CreateLogger<NacosAiService>();
        return new NacosAiService(options, logger);
    }

    /// <summary>
    /// Creates an AI service with server address.
    /// </summary>
    public IAiService CreateAiService(string serverAddr)
    {
        return CreateAiService(new NacosClientOptions { ServerAddresses = serverAddr });
    }

    /// <summary>
    /// Creates a config service with the specified options (static method).
    /// </summary>
    public static IConfigService CreateConfigServiceStatic(NacosClientOptions options)
    {
        return new NacosFactory().CreateConfigService(options);
    }

    /// <summary>
    /// Creates a config service with server address (static method).
    /// </summary>
    public static IConfigService CreateConfigServiceStatic(string serverAddr)
    {
        return new NacosFactory().CreateConfigService(serverAddr);
    }

    /// <summary>
    /// Creates a naming service with the specified options (static method).
    /// </summary>
    public static INamingService CreateNamingServiceStatic(NacosClientOptions options)
    {
        return new NacosFactory().CreateNamingService(options);
    }

    /// <summary>
    /// Creates a naming service with server address (static method).
    /// </summary>
    public static INamingService CreateNamingServiceStatic(string serverAddr)
    {
        return new NacosFactory().CreateNamingService(serverAddr);
    }

    /// <summary>
    /// Creates an AI service with the specified options (static method).
    /// </summary>
    public static IAiService CreateAiServiceStatic(NacosClientOptions options)
    {
        return new NacosFactory().CreateAiService(options);
    }

    /// <summary>
    /// Creates an AI service with server address (static method).
    /// </summary>
    public static IAiService CreateAiServiceStatic(string serverAddr)
    {
        return new NacosFactory().CreateAiService(serverAddr);
    }
}
