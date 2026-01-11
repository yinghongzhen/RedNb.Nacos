namespace RedNb.Nacos.Core;

/// <summary>
/// Nacos client options for configuration.
/// </summary>
public class NacosClientOptions
{
    /// <summary>
    /// Nacos server addresses, separated by comma.
    /// Example: "localhost:8848" or "192.168.1.1:8848,192.168.1.2:8848"
    /// </summary>
    public string ServerAddresses { get; set; } = "localhost:8848";

    /// <summary>
    /// Namespace/Tenant ID.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Access key for authentication.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// Secret key for authentication.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Username for authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Endpoint for address server.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Context path, default is "nacos".
    /// </summary>
    public string ContextPath { get; set; } = "nacos";

    /// <summary>
    /// Cluster name.
    /// </summary>
    public string ClusterName { get; set; } = NacosConstants.DefaultClusterName;

    /// <summary>
    /// Default timeout in milliseconds.
    /// </summary>
    public int DefaultTimeout { get; set; } = NacosConstants.DefaultTimeout;

    /// <summary>
    /// Long poll timeout in milliseconds.
    /// </summary>
    public int LongPollTimeout { get; set; } = NacosConstants.DefaultLongPollTimeout;

    /// <summary>
    /// Retry count for failed requests.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Enable logging.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Load cache at start for naming service.
    /// </summary>
    public bool NamingLoadCacheAtStart { get; set; } = false;

    /// <summary>
    /// Push empty protection for naming service.
    /// </summary>
    public bool NamingPushEmptyProtection { get; set; } = false;

    /// <summary>
    /// Enable gRPC client.
    /// </summary>
    public bool EnableGrpc { get; set; } = true;

    /// <summary>
    /// gRPC port offset from HTTP port.
    /// </summary>
    public int GrpcPortOffset { get; set; } = 1000;

    /// <summary>
    /// Enable TLS/SSL.
    /// </summary>
    public bool EnableTls { get; set; } = false;

    /// <summary>
    /// TLS certificate path.
    /// </summary>
    public string? TlsCertPath { get; set; }

    /// <summary>
    /// TLS key path.
    /// </summary>
    public string? TlsKeyPath { get; set; }

    /// <summary>
    /// TLS CA certificate path.
    /// </summary>
    public string? TlsCaPath { get; set; }

    /// <summary>
    /// App name for registration.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Gets the list of server addresses.
    /// </summary>
    public List<string> GetServerAddressList()
    {
        if (string.IsNullOrWhiteSpace(ServerAddresses))
        {
            return new List<string>();
        }

        return ServerAddresses
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    /// <summary>
    /// Gets the base URL for a server address.
    /// </summary>
    public string GetBaseUrl(string serverAddress)
    {
        var scheme = EnableTls ? "https" : "http";
        var contextPath = string.IsNullOrWhiteSpace(ContextPath) ? "" : $"/{ContextPath.TrimStart('/')}";
        return $"{scheme}://{serverAddress}{contextPath}";
    }

    /// <summary>
    /// Gets the gRPC address for a server address.
    /// </summary>
    public string GetGrpcAddress(string serverAddress)
    {
        var parts = serverAddress.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[1], out var port))
        {
            return $"{parts[0]}:{port + GrpcPortOffset}";
        }
        return serverAddress;
    }

    /// <summary>
    /// Validates the options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerAddresses) && string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new NacosException(NacosException.InvalidParam, "ServerAddresses or Endpoint must be provided");
        }
    }
}
