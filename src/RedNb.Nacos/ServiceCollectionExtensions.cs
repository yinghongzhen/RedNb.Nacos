using RedNb.Nacos.Auth;
using RedNb.Nacos.Common.Failover;
using RedNb.Nacos.Config;
using RedNb.Nacos.Naming;
using RedNb.Nacos.Remote.Grpc;
using RedNb.Nacos.Remote.Http;

namespace RedNb.Nacos;

/// <summary>
/// 依赖注入扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 Nacos 服务（配置中心 + 服务注册发现）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="sectionName">配置节名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddRedNbNacos(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = NacosOptions.SectionName)
    {
        services.Configure<NacosOptions>(configuration.GetSection(sectionName));

        return services.AddNacosCore();
    }

    /// <summary>
    /// 添加 Nacos 服务（使用 Action 配置）
    /// </summary>
    public static IServiceCollection AddRedNbNacos(
        this IServiceCollection services,
        Action<NacosOptions> configure)
    {
        services.Configure(configure);

        return services.AddNacosCore();
    }

    /// <summary>
    /// 仅添加 Nacos 配置中心服务
    /// </summary>
    public static IServiceCollection AddRedNbNacosConfig(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = NacosOptions.SectionName)
    {
        services.Configure<NacosOptions>(configuration.GetSection(sectionName));

        services.AddNacosHttpClients();
        services.AddNacosBaseServices();
        services.AddNacosSnapshot();
        services.AddNacosGrpc();

        // 仅配置服务
        services.AddSingleton<INacosConfigService, NacosConfigService>();

        return services;
    }

    /// <summary>
    /// 仅添加 Nacos 服务注册发现服务
    /// </summary>
    public static IServiceCollection AddRedNbNacosNaming(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = NacosOptions.SectionName)
    {
        services.Configure<NacosOptions>(configuration.GetSection(sectionName));

        services.AddNacosHttpClients();
        services.AddNacosBaseServices();
        services.AddNacosSnapshot();
        services.AddNacosGrpc();

        // 仅命名服务
        services.AddSingleton<INacosNamingService, NacosNamingService>();

        return services;
    }

    /// <summary>
    /// 添加核心 Nacos 服务（完整功能）
    /// </summary>
    private static IServiceCollection AddNacosCore(this IServiceCollection services)
    {
        services.AddNacosHttpClients();
        services.AddNacosBaseServices();
        services.AddNacosSnapshot();
        services.AddNacosGrpc();

        // 配置服务
        services.AddSingleton<INacosConfigService, NacosConfigService>();

        // 命名服务
        services.AddSingleton<INacosNamingService, NacosNamingService>();

        return services;
    }

    /// <summary>
    /// 注册 HTTP 客户端
    /// </summary>
    private static IServiceCollection AddNacosHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient("Nacos", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "RedNb.Nacos/1.0");
        });

        services.AddHttpClient("NacosAuth", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "RedNb.Nacos/1.0");
        });

        return services;
    }

    /// <summary>
    /// 注册基础服务（认证、HTTP 客户端）
    /// </summary>
    private static IServiceCollection AddNacosBaseServices(this IServiceCollection services)
    {
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INacosHttpClient, NacosHttpClient>();

        return services;
    }

    /// <summary>
    /// 注册快照/容灾服务
    /// </summary>
    private static IServiceCollection AddNacosSnapshot(this IServiceCollection services)
    {
        services.AddSingleton<IConfigSnapshot, LocalFileConfigSnapshot>();
        services.AddSingleton<IServiceSnapshot, LocalFileServiceSnapshot>();

        return services;
    }

    /// <summary>
    /// 注册 gRPC 服务
    /// </summary>
    private static IServiceCollection AddNacosGrpc(this IServiceCollection services)
    {
        services.AddSingleton<INacosGrpcClient, NacosGrpcClient>();

        return services;
    }
}
