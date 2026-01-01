namespace RedNb.Nacos.AspNetCore.Hosting;

/// <summary>
/// HostBuilder 扩展方法
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// 使用 Nacos 服务注册
    /// </summary>
    public static IHostBuilder UseRedNbNacos(
        this IHostBuilder builder,
        string sectionName = NacosOptions.SectionName)
    {
        return builder.ConfigureServices((context, services) =>
        {
            services.AddRedNbNacosAspNetCore(context.Configuration, sectionName);
        });
    }

    /// <summary>
    /// 使用 Nacos 服务注册（Action 配置）
    /// </summary>
    public static IHostBuilder UseRedNbNacos(
        this IHostBuilder builder,
        Action<NacosOptions> configure)
    {
        return builder.ConfigureServices((context, services) =>
        {
            services.AddRedNbNacosAspNetCore(configure);
        });
    }
}

/// <summary>
/// WebApplicationBuilder 扩展方法
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// 添加 Nacos（WebApplicationBuilder 扩展）
    /// </summary>
    public static WebApplicationBuilder AddRedNbNacos(
        this WebApplicationBuilder builder,
        string sectionName = NacosOptions.SectionName)
    {
        builder.Services.AddRedNbNacosAspNetCore(builder.Configuration, sectionName);
        return builder;
    }

    /// <summary>
    /// 添加 Nacos（Action 配置）
    /// </summary>
    public static WebApplicationBuilder AddRedNbNacos(
        this WebApplicationBuilder builder,
        Action<NacosOptions> configure)
    {
        builder.Services.AddRedNbNacosAspNetCore(configure);
        return builder;
    }
}
