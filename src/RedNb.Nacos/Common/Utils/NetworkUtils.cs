using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RedNb.Nacos.Common.Utils;

/// <summary>
/// 网络工具类
/// </summary>
public static class NetworkUtils
{
    /// <summary>
    /// 获取本机 IP 地址
    /// </summary>
    /// <param name="preferredNetworks">优先网络前缀（支持多个，用逗号分隔）</param>
    /// <returns>IP 地址</returns>
    public static string GetLocalIp(string? preferredNetworks = null)
    {
        var prefixes = string.IsNullOrEmpty(preferredNetworks)
            ? Array.Empty<string>()
            : preferredNetworks.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up
                && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .ToList();

        foreach (var ni in networkInterfaces)
        {
            var properties = ni.GetIPProperties();
            var addresses = properties.UnicastAddresses
                .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(ua => ua.Address.ToString())
                .ToList();

            foreach (var address in addresses)
            {
                // 优先匹配指定前缀
                if (prefixes.Length > 0)
                {
                    if (prefixes.Any(p => address.StartsWith(p)))
                    {
                        return address;
                    }
                }
                else
                {
                    // 排除回环地址和链路本地地址
                    if (!address.StartsWith("127.") && !address.StartsWith("169.254."))
                    {
                        return address;
                    }
                }
            }
        }

        // 如果没有找到匹配的，返回第一个非回环地址
        foreach (var ni in networkInterfaces)
        {
            var properties = ni.GetIPProperties();
            var address = properties.UnicastAddresses
                .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork
                    && !ua.Address.ToString().StartsWith("127."))
                ?.Address.ToString();

            if (!string.IsNullOrEmpty(address))
            {
                return address;
            }
        }

        return "127.0.0.1";
    }

    /// <summary>
    /// 检查端口是否可用
    /// </summary>
    public static bool IsPortAvailable(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从 URL 中解析主机和端口
    /// </summary>
    public static (string host, int port) ParseHostPort(string serverAddress)
    {
        var uri = new Uri(serverAddress);
        var port = uri.Port;
        if (port == -1)
        {
            port = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80;
        }
        return (uri.Host, port);
    }

    /// <summary>
    /// 获取 gRPC 端口（HTTP 端口 + 偏移量）
    /// </summary>
    public static int GetGrpcPort(string serverAddress, int offset = 1000)
    {
        var (_, httpPort) = ParseHostPort(serverAddress);
        return httpPort + offset;
    }
}
