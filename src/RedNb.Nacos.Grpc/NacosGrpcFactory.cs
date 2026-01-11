using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Ai;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.GrpcClient.Config;
using RedNb.Nacos.GrpcClient.Naming;

namespace RedNb.Nacos.GrpcClient;

/// <summary>
/// Factory for creating Nacos gRPC services.
/// </summary>
public class NacosGrpcFactory : INacosFactory
{
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    /// Creates a new instance of NacosGrpcFactory.
    /// </summary>
    public NacosGrpcFactory(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates a config service using gRPC asynchronously.
    /// </summary>
    public static async Task<IConfigService> CreateConfigServiceAsync(NacosClientOptions options)
    {
        var service = new NacosGrpcConfigService(options);
        await service.InitializeAsync();
        return service;
    }

    /// <summary>
    /// Creates a config service using gRPC with a logger asynchronously.
    /// </summary>
    public static async Task<IConfigService> CreateConfigServiceAsync(
        NacosClientOptions options, 
        ILogger<NacosGrpcConfigService> logger)
    {
        var service = new NacosGrpcConfigService(options, logger);
        await service.InitializeAsync();
        return service;
    }

    /// <summary>
    /// Creates a naming service using gRPC asynchronously.
    /// </summary>
    public static async Task<INamingService> CreateNamingServiceAsync(NacosClientOptions options)
    {
        var service = new NacosGrpcNamingService(options);
        await service.InitializeAsync();
        return service;
    }

    /// <summary>
    /// Creates a naming service using gRPC with a logger asynchronously.
    /// </summary>
    public static async Task<INamingService> CreateNamingServiceAsync(
        NacosClientOptions options, 
        ILogger<NacosGrpcNamingService> logger)
    {
        var service = new NacosGrpcNamingService(options, logger);
        await service.InitializeAsync();
        return service;
    }

    /// <inheritdoc/>
    public IConfigService CreateConfigService(NacosClientOptions options)
    {
        var logger = _serviceProvider?.GetService<ILogger<NacosGrpcConfigService>>();
        var service = new NacosGrpcConfigService(options, logger);
        // Note: Must call InitializeAsync before use for gRPC connection
        return service;
    }

    /// <inheritdoc/>
    public IConfigService CreateConfigService(string serverAddr)
    {
        var options = new NacosClientOptions { ServerAddresses = serverAddr };
        return CreateConfigService(options);
    }

    /// <inheritdoc/>
    public INamingService CreateNamingService(NacosClientOptions options)
    {
        var logger = _serviceProvider?.GetService<ILogger<NacosGrpcNamingService>>();
        var service = new NacosGrpcNamingService(options, logger);
        // Note: Must call InitializeAsync before use for gRPC connection
        return service;
    }

    /// <inheritdoc/>
    public INamingService CreateNamingService(string serverAddr)
    {
        var options = new NacosClientOptions { ServerAddresses = serverAddr };
        return CreateNamingService(options);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// AI Service is not currently supported for gRPC transport.
    /// Please use the HTTP factory instead.
    /// </remarks>
    public IAiService CreateAiService(NacosClientOptions options)
    {
        throw new NotSupportedException(
            "AI Service is not supported for gRPC transport. " +
            "Please use NacosFactory from RedNb.Nacos.Http package instead.");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// AI Service is not currently supported for gRPC transport.
    /// Please use the HTTP factory instead.
    /// </remarks>
    public IAiService CreateAiService(string serverAddr)
    {
        throw new NotSupportedException(
            "AI Service is not supported for gRPC transport. " +
            "Please use NacosFactory from RedNb.Nacos.Http package instead.");
    }
}

/// <summary>
/// Extension methods for adding Nacos gRPC services to DI container.
/// </summary>
public static class NacosGrpcServiceCollectionExtensions
{
    /// <summary>
    /// Adds Nacos gRPC services (config and naming).
    /// </summary>
    public static IServiceCollection AddNacosGrpc(
        this IServiceCollection services, 
        Action<NacosClientOptions> configure)
    {
        var options = new NacosClientOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<INacosFactory>(sp => new NacosGrpcFactory(sp));
        services.AddSingleton<IConfigService>(sp => 
        {
            var logger = sp.GetService<ILogger<NacosGrpcConfigService>>();
            return new NacosGrpcConfigService(options, logger);
        });
        services.AddSingleton<INamingService>(sp => 
        {
            var logger = sp.GetService<ILogger<NacosGrpcNamingService>>();
            return new NacosGrpcNamingService(options, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds Nacos gRPC config service only.
    /// </summary>
    public static IServiceCollection AddNacosGrpcConfig(
        this IServiceCollection services, 
        Action<NacosClientOptions> configure)
    {
        var options = new NacosClientOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<IConfigService>(sp => 
        {
            var logger = sp.GetService<ILogger<NacosGrpcConfigService>>();
            return new NacosGrpcConfigService(options, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds Nacos gRPC naming service only.
    /// </summary>
    public static IServiceCollection AddNacosGrpcNaming(
        this IServiceCollection services, 
        Action<NacosClientOptions> configure)
    {
        var options = new NacosClientOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<INamingService>(sp => 
        {
            var logger = sp.GetService<ILogger<NacosGrpcNamingService>>();
            return new NacosGrpcNamingService(options, logger);
        });

        return services;
    }
}
