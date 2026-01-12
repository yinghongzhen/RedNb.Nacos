using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Ai;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Core.Lock;
using RedNb.Nacos.Core.Maintainer;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Grpc.Lock;
using RedNb.Nacos.Grpc.Maintainer;
using RedNb.Nacos.GrpcClient.Ai;
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

    /// <summary>
    /// Creates an AI service using gRPC asynchronously.
    /// </summary>
    public static async Task<IAiService> CreateAiServiceAsync(NacosClientOptions options)
    {
        var service = new NacosGrpcAiService(options);
        await service.InitializeAsync();
        return service;
    }

    /// <summary>
    /// Creates an AI service using gRPC with a logger asynchronously.
    /// </summary>
    public static async Task<IAiService> CreateAiServiceAsync(
        NacosClientOptions options,
        ILogger<NacosGrpcAiService> logger)
    {
        var service = new NacosGrpcAiService(options, logger);
        await service.InitializeAsync();
        return service;
    }

    /// <summary>
    /// Creates a lock service using gRPC asynchronously.
    /// </summary>
    public static async Task<ILockService> CreateLockServiceAsync(NacosClientOptions options)
    {
        var service = new NacosGrpcLockService(options);
        await service.InitializeAsync();
        return service;
    }

    /// <summary>
    /// Creates a lock service using gRPC with a logger asynchronously.
    /// </summary>
    public static async Task<ILockService> CreateLockServiceAsync(
        NacosClientOptions options,
        ILogger<NacosGrpcLockService> logger)
    {
        var service = new NacosGrpcLockService(options, logger);
        await service.InitializeAsync();
        return service;
    }

    /// <summary>
    /// Creates a maintainer service (uses HTTP as MaintainerService is HTTP-only).
    /// </summary>
    public static IMaintainerService CreateMaintainerServiceStatic(NacosClientOptions options)
    {
        return new NacosGrpcMaintainerService(options);
    }

    /// <summary>
    /// Creates a maintainer service with a logger (uses HTTP as MaintainerService is HTTP-only).
    /// </summary>
    public static IMaintainerService CreateMaintainerServiceStatic(
        NacosClientOptions options,
        ILogger<NacosGrpcMaintainerService> logger)
    {
        return new NacosGrpcMaintainerService(options, logger);
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
    public IAiService CreateAiService(NacosClientOptions options)
    {
        var logger = _serviceProvider?.GetService<ILogger<NacosGrpcAiService>>();
        var service = new NacosGrpcAiService(options, logger);
        // Note: Must call InitializeAsync before use for gRPC connection
        return service;
    }

    /// <inheritdoc/>
    public IAiService CreateAiService(string serverAddr)
    {
        var options = new NacosClientOptions { ServerAddresses = serverAddr };
        return CreateAiService(options);
    }

    /// <inheritdoc/>
    public ILockService CreateLockService(NacosClientOptions options)
    {
        var logger = _serviceProvider?.GetService<ILogger<NacosGrpcLockService>>();
        var service = new NacosGrpcLockService(options, logger);
        // Note: Must call InitializeAsync before use for gRPC connection
        return service;
    }

    /// <inheritdoc/>
    public ILockService CreateLockService(string serverAddr)
    {
        var options = new NacosClientOptions { ServerAddresses = serverAddr };
        return CreateLockService(options);
    }

    /// <inheritdoc/>
    public IMaintainerService CreateMaintainerService(NacosClientOptions options)
    {
        var logger = _serviceProvider?.GetService<ILogger<NacosGrpcMaintainerService>>();
        return new NacosGrpcMaintainerService(options, logger);
    }

    /// <inheritdoc/>
    public IMaintainerService CreateMaintainerService(string serverAddr)
    {
        var options = new NacosClientOptions { ServerAddresses = serverAddr };
        return CreateMaintainerService(options);
    }
}

/// <summary>
/// Extension methods for adding Nacos gRPC services to DI container.
/// </summary>
public static class NacosGrpcServiceCollectionExtensions
{
    /// <summary>
    /// Adds Nacos gRPC services (config, naming, AI, lock, and maintainer).
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
        services.AddSingleton<IAiService>(sp =>
        {
            var logger = sp.GetService<ILogger<NacosGrpcAiService>>();
            return new NacosGrpcAiService(options, logger);
        });
        services.AddSingleton<ILockService>(sp =>
        {
            var logger = sp.GetService<ILogger<NacosGrpcLockService>>();
            return new NacosGrpcLockService(options, logger);
        });
        services.AddSingleton<IMaintainerService>(sp =>
        {
            var logger = sp.GetService<ILogger<NacosGrpcMaintainerService>>();
            return new NacosGrpcMaintainerService(options, logger);
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

    /// <summary>
    /// Adds Nacos gRPC lock service only.
    /// </summary>
    public static IServiceCollection AddNacosGrpcLock(
        this IServiceCollection services, 
        Action<NacosClientOptions> configure)
    {
        var options = new NacosClientOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<ILockService>(sp => 
        {
            var logger = sp.GetService<ILogger<NacosGrpcLockService>>();
            return new NacosGrpcLockService(options, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds Nacos maintainer service only.
    /// </summary>
    public static IServiceCollection AddNacosGrpcMaintainer(
        this IServiceCollection services, 
        Action<NacosClientOptions> configure)
    {
        var options = new NacosClientOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<IMaintainerService>(sp => 
        {
            var logger = sp.GetService<ILogger<NacosGrpcMaintainerService>>();
            return new NacosGrpcMaintainerService(options, logger);
        });

        return services;
    }
}
