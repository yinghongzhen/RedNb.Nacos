namespace RedNb.Nacos.Configuration;

/// <summary>
/// HostBuilder 扩展方法
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// 使用 Nacos 配置中心
    /// </summary>
    public static IHostBuilder UseRedNbNacosConfiguration(
        this IHostBuilder hostBuilder,
        Action<NacosConfigurationSource> configure)
    {
        return hostBuilder.ConfigureAppConfiguration((context, builder) =>
        {
            builder.AddRedNbNacosConfiguration(configure);
        });
    }

    /// <summary>
    /// 使用 Nacos 配置中心（简化版本）
    /// </summary>
    public static IHostBuilder UseRedNbNacosConfiguration(
        this IHostBuilder hostBuilder,
        string serverAddresses,
        string dataId,
        string group = NacosConstants.DefaultGroup,
        string? namespaceId = null,
        string? username = null,
        string? password = null)
    {
        return hostBuilder.ConfigureAppConfiguration((context, builder) =>
        {
            builder.AddRedNbNacosConfiguration(
                serverAddresses,
                dataId,
                group,
                namespaceId,
                username,
                password);
        });
    }

    /// <summary>
    /// 使用 Nacos 配置中心（从 appsettings 读取配置）
    /// </summary>
    public static IHostBuilder UseRedNbNacosConfiguration(
        this IHostBuilder hostBuilder,
        string sectionName = NacosOptions.SectionName)
    {
        return hostBuilder.ConfigureAppConfiguration((context, builder) =>
        {
            // 先构建临时配置以读取 Nacos 选项
            var tempConfig = builder.Build();
            builder.AddRedNbNacosConfiguration(tempConfig, sectionName);
        });
    }
}
