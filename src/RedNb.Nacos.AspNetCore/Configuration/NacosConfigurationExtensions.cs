using Microsoft.Extensions.Configuration;

namespace RedNb.Nacos.AspNetCore.Configuration;

/// <summary>
/// Extension methods for adding Nacos as a configuration source.
/// </summary>
public static class NacosConfigurationExtensions
{
    /// <summary>
    /// Adds Nacos as a configuration source to the configuration builder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configure">Action to configure the Nacos configuration source.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddNacosConfiguration(
        this IConfigurationBuilder builder,
        Action<NacosConfigurationSource> configure)
    {
        var source = new NacosConfigurationSource();
        configure(source);
        builder.Add(source);
        return builder;
    }

    /// <summary>
    /// Adds Nacos as a configuration source with simple connection settings.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="serverAddresses">Nacos server addresses.</param>
    /// <param name="dataId">The data ID to load.</param>
    /// <param name="group">The group name. Defaults to "DEFAULT_GROUP".</param>
    /// <param name="namespace">Optional namespace.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddNacosConfiguration(
        this IConfigurationBuilder builder,
        string serverAddresses,
        string dataId,
        string group = "DEFAULT_GROUP",
        string? @namespace = null)
    {
        return builder.AddNacosConfiguration(source =>
        {
            source.Options.ServerAddresses = serverAddresses;
            source.Options.Namespace = @namespace ?? string.Empty;
            source.ConfigItems.Add(new NacosConfigurationItem
            {
                DataId = dataId,
                Group = group
            });
        });
    }

    /// <summary>
    /// Adds multiple configuration items from Nacos.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="serverAddresses">Nacos server addresses.</param>
    /// <param name="configItems">Configuration items to load.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddNacosConfiguration(
        this IConfigurationBuilder builder,
        string serverAddresses,
        params (string DataId, string Group)[] configItems)
    {
        return builder.AddNacosConfiguration(source =>
        {
            source.Options.ServerAddresses = serverAddresses;
            foreach (var (dataId, group) in configItems)
            {
                source.ConfigItems.Add(new NacosConfigurationItem
                {
                    DataId = dataId,
                    Group = group
                });
            }
        });
    }
}
