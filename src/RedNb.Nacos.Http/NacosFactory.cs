using Microsoft.Extensions.Logging;
using RedNb.Nacos.Client.Ai;
using RedNb.Nacos.Client.Config;
using RedNb.Nacos.Client.Naming;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Ai;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Lock;
using RedNb.Nacos.Core.Maintainer;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Http.Lock;
using RedNb.Nacos.Http.Maintainer;

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
    /// Creates a distributed lock service with the specified options.
    /// </summary>
    public ILockService CreateLockService(NacosClientOptions options)
    {
        options.Validate();
        var logger = _loggerFactory?.CreateLogger<NacosLockService>();
        return new NacosLockService(options, logger);
    }

    /// <summary>
    /// Creates a distributed lock service with server address.
    /// </summary>
    public ILockService CreateLockService(string serverAddr)
    {
        return CreateLockService(new NacosClientOptions { ServerAddresses = serverAddr });
    }

    /// <summary>
    /// Creates a maintainer service with the specified options.
    /// </summary>
    public IMaintainerService CreateMaintainerService(NacosClientOptions options)
    {
        options.Validate();
        var logger = _loggerFactory?.CreateLogger<NacosMaintainerService>();
        return new NacosMaintainerService(options, logger);
    }

    /// <summary>
    /// Creates a maintainer service with server address.
    /// </summary>
    public IMaintainerService CreateMaintainerService(string serverAddr)
    {
        return CreateMaintainerService(new NacosClientOptions { ServerAddresses = serverAddr });
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

    /// <summary>
    /// Creates a lock service with the specified options (static method).
    /// </summary>
    public static ILockService CreateLockServiceStatic(NacosClientOptions options)
    {
        return new NacosFactory().CreateLockService(options);
    }

    /// <summary>
    /// Creates a lock service with server address (static method).
    /// </summary>
    public static ILockService CreateLockServiceStatic(string serverAddr)
    {
        return new NacosFactory().CreateLockService(serverAddr);
    }

    /// <summary>
    /// Creates a maintainer service with the specified options (static method).
    /// </summary>
    public static IMaintainerService CreateMaintainerServiceStatic(NacosClientOptions options)
    {
        return new NacosFactory().CreateMaintainerService(options);
    }

    /// <summary>
    /// Creates a maintainer service with server address (static method).
    /// </summary>
    public static IMaintainerService CreateMaintainerServiceStatic(string serverAddr)
    {
        return new NacosFactory().CreateMaintainerService(serverAddr);
    }
}
