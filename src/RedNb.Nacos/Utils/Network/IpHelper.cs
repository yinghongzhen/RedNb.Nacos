using System.Net;
using System.Net.Sockets;

namespace RedNb.Nacos.Utils.Network;

/// <summary>
/// IP address helper methods.
/// </summary>
public static class IpHelper
{
    /// <summary>
    /// Checks if a string is a valid IPv4 address.
    /// </summary>
    public static bool IsIpv4(string? ip)
    {
        if (string.IsNullOrEmpty(ip))
        {
            return false;
        }

        if (IPAddress.TryParse(ip, out var address))
        {
            return address.AddressFamily == AddressFamily.InterNetwork;
        }

        return false;
    }

    /// <summary>
    /// Checks if a string is a valid IPv6 address.
    /// </summary>
    public static bool IsIpv6(string? ip)
    {
        if (string.IsNullOrEmpty(ip))
        {
            return false;
        }

        if (IPAddress.TryParse(ip, out var address))
        {
            return address.AddressFamily == AddressFamily.InterNetworkV6;
        }

        return false;
    }

    /// <summary>
    /// Checks if a string is a valid IP address (IPv4 or IPv6).
    /// </summary>
    public static bool IsValidIp(string? ip)
    {
        return !string.IsNullOrEmpty(ip) && IPAddress.TryParse(ip, out _);
    }
}
