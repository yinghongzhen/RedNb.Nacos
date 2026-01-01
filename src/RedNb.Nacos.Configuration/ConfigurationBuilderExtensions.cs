using RedNb.Nacos.Configuration.Parsers;

namespace RedNb.Nacos.Configuration;

/// <summary>
/// ConfigurationBuilder 扩展方法
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// 添加 Nacos 配置源
    /// </summary>
    public static IConfigurationBuilder AddRedNbNacosConfiguration(
        this IConfigurationBuilder builder,
        Action<NacosConfigurationSource> configure)
    {
        var source = new NacosConfigurationSource
        {
            NacosOptions = new NacosOptions { ServerAddresses = new List<string>() }
        };
        configure(source);
        builder.Add(source);
        return builder;
    }

    /// <summary>
    /// 添加 Nacos 配置源（简化版本）
    /// </summary>
    public static IConfigurationBuilder AddRedNbNacosConfiguration(
        this IConfigurationBuilder builder,
        string serverAddresses,
        string dataId,
        string group = NacosConstants.DefaultGroup,
        string? namespaceId = null,
        string? username = null,
        string? password = null,
        bool optional = false)
    {
        return builder.AddRedNbNacosConfiguration(source =>
        {
            source.NacosOptions = new NacosOptions
            {
                ServerAddresses = serverAddresses.Split([',', ';']).ToList(),
                Namespace = namespaceId ?? string.Empty,
                UserName = username,
                Password = password
            };
            source.ConfigItems.Add(new NacosConfigItem
            {
                DataId = dataId,
                Group = group,
                Optional = optional
            });
            source.Optional = optional;
            source.Parser = ConfigurationParserFactory.GetParser(dataId);
        });
    }

    /// <summary>
    /// 添加多个 Nacos 配置源
    /// </summary>
    public static IConfigurationBuilder AddRedNbNacosConfiguration(
        this IConfigurationBuilder builder,
        string serverAddresses,
        IEnumerable<(string dataId, string group)> configItems,
        string? namespaceId = null,
        string? username = null,
        string? password = null,
        bool optional = false)
    {
        return builder.AddRedNbNacosConfiguration(source =>
        {
            source.NacosOptions = new NacosOptions
            {
                ServerAddresses = serverAddresses.Split([',', ';']).ToList(),
                Namespace = namespaceId ?? string.Empty,
                UserName = username,
                Password = password
            };

            foreach (var (dataId, group) in configItems)
            {
                source.ConfigItems.Add(new NacosConfigItem
                {
                    DataId = dataId,
                    Group = group,
                    Optional = optional
                });
            }

            source.Optional = optional;
        });
    }

    /// <summary>
    /// 从现有配置加载 Nacos 配置源
    /// </summary>
    public static IConfigurationBuilder AddRedNbNacosConfiguration(
        this IConfigurationBuilder builder,
        IConfiguration configuration,
        string sectionName = NacosOptions.SectionName)
    {
        var options = new NacosOptions { ServerAddresses = new List<string>() };
        configuration.GetSection(sectionName).Bind(options);

        return builder.AddRedNbNacosConfiguration(source =>
        {
            source.NacosOptions = options;

            // 从配置中读取 Listeners
            foreach (var listener in options.Config.Listeners)
            {
                source.ConfigItems.Add(new NacosConfigItem
                {
                    DataId = listener.DataId,
                    Group = listener.Group,
                    Optional = listener.Optional
                });
            }
        });
    }
}
