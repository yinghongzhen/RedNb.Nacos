using RedNb.Nacos.AspNetCore.Hosting;

namespace RedNb.Nacos.AspNetCore;

/// <summary>
/// ASP.NET Core 依赖注入扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 Nacos 服务（含服务注册）
    /// </summary>
    public static IServiceCollection AddRedNbNacosAspNetCore(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = NacosOptions.SectionName)
    {
        // 添加核心服务
        services.AddRedNbNacos(configuration, sectionName);

        // 添加后台服务（服务注册/心跳）
        services.AddHostedService<NacosHostedService>();

        return services;
    }

    /// <summary>
    /// 添加 Nacos 服务（使用 Action 配置）
    /// </summary>
    public static IServiceCollection AddRedNbNacosAspNetCore(
        this IServiceCollection services,
        Action<NacosOptions> configure)
    {
        // 添加核心服务
        services.AddRedNbNacos(configure);

        // 添加后台服务（服务注册/心跳）
        services.AddHostedService<NacosHostedService>();

        return services;
    }
}
