namespace RedNb.Nacos;

/// <summary>
/// Nacos client options for configuration.
/// </summary>
public class NacosClientOptions
{
    /// <summary>
    /// Optional source-generated metadata for application-owned JSON types.
    /// SDK wire contracts take precedence. Required for custom types in NativeAOT.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver? JsonTypeInfoResolver { get; set; }
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
    /// Nacos **console** server addresses, comma-separated. The console is a
    /// separate listener (default port 8080) and serves the AI admin/UI API
    /// (<c>/v3/console/ai/**</c>) plus the readiness/health probes. Distinct
    /// from <see cref="ServerAddresses"/>, which targets the API/gRPC port
    /// (default 8848).
    /// <para>
    /// When left empty, the SDK derives console addresses from
    /// <see cref="ServerAddresses"/> by replacing each port with the default
    /// console port (8080).
    /// </para>
    /// </summary>
    public string ConsoleAddresses { get; set; } = string.Empty;

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
        var contextPath = string.IsNullOrWhiteSpace(ContextPath) ? "" : $"/{ContextPath.Trim('/')}";
        return $"{GetConsoleBaseUrl(serverAddress)}{contextPath}";
    }

    /// <summary>
    /// Gets the console server address list, deriving from
    /// <see cref="ServerAddresses"/> (port → 8080 substitution) when
    /// <see cref="ConsoleAddresses"/> is empty.
    /// </summary>
    public List<string> GetConsoleAddressList()
    {
        if (!string.IsNullOrWhiteSpace(ConsoleAddresses))
        {
            // Explicit console addresses win verbatim — the operator already
            // picked the port (e.g. 8443 behind TLS), so do NOT substitute
            // 8080 onto them. Only the ServerAddresses fallback path is
            // port-substituted, since those addresses describe the API port
            // (8848) and need to be re-targeted at the console port.
            return ConsoleAddresses
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
        return GetServerAddressList()
            .Select(SubstituteConsolePort)
            .ToList();
    }

    /// <summary>
    /// Gets the base URL for a console server address. Unlike
    /// <see cref="GetBaseUrl"/>, no <see cref="ContextPath"/> is appended: the
    /// console listener expects paths under <c>/v3/console/**</c> with no
    /// <c>/nacos</c> prefix.
    /// An explicit HTTP/HTTPS scheme takes precedence over <see cref="EnableTls"/>;
    /// that option selects the default scheme only for bare host:port addresses.
    /// </summary>
    public string GetConsoleBaseUrl(string serverAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverAddress);
        var address = serverAddress.Trim().TrimEnd('/');
        if (address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return address;
        }
        var scheme = EnableTls ? "https" : "http";
        return $"{scheme}://{address}";
    }

    private static string SubstituteConsolePort(string hostPort)
    {
        if (hostPort.Contains("://", StringComparison.Ordinal))
        {
            return new UriBuilder(hostPort) { Port = 8080 }.Uri.GetLeftPart(UriPartial.Authority);
        }
        var idx = hostPort.LastIndexOf(':');
        // IPv6 literal — leave as-is (operator can override via explicit ConsoleAddresses)
        if (hostPort.Count(c => c == ':') > 1) return hostPort;
        if (idx < 0) return $"{hostPort}:8080";
        return $"{hostPort[..idx]}:8080";
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
        var hasCore = !string.IsNullOrWhiteSpace(ServerAddresses);
        var hasConsole = !string.IsNullOrWhiteSpace(ConsoleAddresses);
        var hasEndpoint = !string.IsNullOrWhiteSpace(Endpoint);
        if (!hasCore && !hasConsole && !hasEndpoint)
        {
            throw new NacosException(
                NacosException.InvalidParam,
                "At least one of ServerAddresses, ConsoleAddresses, or Endpoint must be provided");
        }

        var hasUsernamePassword = !string.IsNullOrWhiteSpace(Username) || !string.IsNullOrWhiteSpace(Password);
        if (hasUsernamePassword && !hasCore)
        {
            throw new NacosException(
                NacosException.InvalidParam,
                "Username/Password require ServerAddresses: login is performed against the server API port (8848), so a console-only configuration cannot authenticate");
        }

        // Symmetric AK/SK guards: SecurityProxy.ResolveLoginStrategy requires
        // BOTH AccessKey AND SecretKey (AND semantics). Without these guards, a
        // caller that sets only one of them passes Validate() silently, the
        // runtime strategy resolves to None, and the configured credential is
        // dropped without a clear error.
        var hasAccessKeyOnly = !string.IsNullOrWhiteSpace(AccessKey) && string.IsNullOrWhiteSpace(SecretKey);
        if (hasAccessKeyOnly)
        {
            throw new NacosException(
                NacosException.InvalidParam,
                "SecretKey is required when AccessKey is set");
        }

        var hasSecretKeyOnly = !string.IsNullOrWhiteSpace(SecretKey) && string.IsNullOrWhiteSpace(AccessKey);
        if (hasSecretKeyOnly)
        {
            throw new NacosException(
                NacosException.InvalidParam,
                "AccessKey is required when SecretKey is set");
        }

        var hasAccessKey = !string.IsNullOrWhiteSpace(AccessKey) || !string.IsNullOrWhiteSpace(SecretKey);
        if (hasAccessKey && !hasCore)
        {
            throw new NacosException(
                NacosException.InvalidParam,
                "AccessKey/SecretKey require ServerAddresses: AK/SK signing is performed against the server API port (8848), so a console-only configuration cannot authenticate");
        }
    }
}
