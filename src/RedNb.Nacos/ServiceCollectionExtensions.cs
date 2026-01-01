using RedNb.Nacos.Auth;
using RedNb.Nacos.Config;
using RedNb.Nacos.Naming;
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

        // 注册 HTTP 客户端
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

        // 核心服务
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INacosHttpClient, NacosHttpClient>();

        // 配置服务
        services.AddSingleton<INacosConfigService, NacosConfigService>();

        // 命名服务
        services.AddSingleton<INacosNamingService, NacosNamingService>();

        return services;
    }

    /// <summary>
    /// 添加 Nacos 服务（使用 Action 配置）
    /// </summary>
    public static IServiceCollection AddRedNbNacos(
        this IServiceCollection services,
        Action<NacosOptions> configure)
    {
        services.Configure(configure);

        // 注册 HTTP 客户端
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

        // 核心服务
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INacosHttpClient, NacosHttpClient>();

        // 配置服务
        services.AddSingleton<INacosConfigService, NacosConfigService>();

        // 命名服务
        services.AddSingleton<INacosNamingService, NacosNamingService>();

        return services;
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

        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INacosHttpClient, NacosHttpClient>();
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

        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INacosHttpClient, NacosHttpClient>();
        services.AddSingleton<INacosNamingService, NacosNamingService>();

        return services;
    }
}
