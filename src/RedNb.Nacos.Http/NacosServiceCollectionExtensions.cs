using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Client.Config;
using RedNb.Nacos.Client.Naming;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Client;

/// <summary>
/// Extension methods for registering Nacos services with dependency injection.
/// </summary>
public static class NacosServiceCollectionExtensions
{
    /// <summary>
    /// Adds Nacos client services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNacos(this IServiceCollection services, 
        Action<NacosClientOptions> configureOptions)
    {
        services.Configure(configureOptions);
        
        services.TryAddSingleton<INacosFactory, NacosFactory>();
        
        services.TryAddSingleton<IConfigService>(sp =>
        {
            var options = new NacosClientOptions();
            configureOptions(options);
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<NacosConfigService>();
            return new NacosConfigService(options, logger);
        });

        services.TryAddSingleton<INamingService>(sp =>
        {
            var options = new NacosClientOptions();
            configureOptions(options);
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<NacosNamingService>();
            return new NacosNamingService(options, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds only Nacos config service to the service collection.
    /// </summary>
    public static IServiceCollection AddNacosConfig(this IServiceCollection services, 
        Action<NacosClientOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.TryAddSingleton<IConfigService>(sp =>
        {
            var options = new NacosClientOptions();
            configureOptions(options);
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<NacosConfigService>();
            return new NacosConfigService(options, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds only Nacos naming service to the service collection.
    /// </summary>
    public static IServiceCollection AddNacosNaming(this IServiceCollection services, 
        Action<NacosClientOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.TryAddSingleton<INamingService>(sp =>
        {
            var options = new NacosClientOptions();
            configureOptions(options);
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<NacosNamingService>();
            return new NacosNamingService(options, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds Nacos client with pre-configured options.
    /// </summary>
    public static IServiceCollection AddNacos(this IServiceCollection services, 
        string serverAddresses, string? username = null, string? password = null, string? @namespace = null)
    {
        return services.AddNacos(options =>
        {
            options.ServerAddresses = serverAddresses;
            options.Username = username;
            options.Password = password;
            options.Namespace = @namespace ?? string.Empty;
        });
    }
}
