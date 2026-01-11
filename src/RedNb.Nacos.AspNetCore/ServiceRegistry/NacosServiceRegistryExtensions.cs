using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.AspNetCore.ServiceRegistry;

/// <summary>
/// Extension methods for automatic service registration with Nacos.
/// </summary>
public static class NacosServiceRegistryExtensions
{
    /// <summary>
    /// Registers the current application as a service instance with Nacos.
    /// Automatically deregisters on application shutdown.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="serviceName">The service name to register.</param>
    /// <param name="ip">The IP address. If null, will be auto-detected.</param>
    /// <param name="port">The port number.</param>
    /// <param name="groupName">The group name. Defaults to "DEFAULT_GROUP".</param>
    /// <param name="clusterName">The cluster name. Defaults to "DEFAULT".</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseNacosServiceRegistry(
        this IApplicationBuilder app,
        string serviceName,
        string? ip = null,
        int port = 80,
        string groupName = "DEFAULT_GROUP",
        string clusterName = "DEFAULT",
        Dictionary<string, string>? metadata = null)
    {
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        var namingService = app.ApplicationServices.GetService<INamingService>();
        var logger = app.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger("NacosServiceRegistry");

        if (namingService == null)
        {
            logger?.LogWarning("INamingService not registered. Service registration skipped.");
            return app;
        }

        var instance = new Instance
        {
            ServiceName = serviceName,
            Ip = ip ?? GetLocalIpAddress(),
            Port = port,
            ClusterName = clusterName,
            Healthy = true,
            Enabled = true,
            Weight = 1.0,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        // Register on startup
        lifetime.ApplicationStarted.Register(() =>
        {
            try
            {
                namingService.RegisterInstanceAsync(serviceName, groupName, instance)
                    .GetAwaiter().GetResult();
                logger?.LogInformation(
                    "Registered service {ServiceName} at {Ip}:{Port} with Nacos",
                    serviceName, instance.Ip, port);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to register service {ServiceName} with Nacos", serviceName);
            }
        });

        // Deregister on shutdown
        lifetime.ApplicationStopping.Register(() =>
        {
            try
            {
                namingService.DeregisterInstanceAsync(serviceName, groupName, instance.Ip, port, clusterName)
                    .GetAwaiter().GetResult();
                logger?.LogInformation(
                    "Deregistered service {ServiceName} from Nacos",
                    serviceName);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to deregister service {ServiceName} from Nacos", serviceName);
            }
        });

        return app;
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch
        {
            // Fallback to localhost
        }
        return "127.0.0.1";
    }
}
