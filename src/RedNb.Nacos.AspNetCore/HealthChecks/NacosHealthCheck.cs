using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedNb.Nacos.Config;
using RedNb.Nacos.Naming;
using RedNb.Nacos.Utils;

namespace RedNb.Nacos.AspNetCore.HealthChecks;

/// <summary>Checks supplied client connectivity or explicitly selected HTTP components.</summary>
public class NacosHealthCheck : IHealthCheck, IDisposable
{
    private readonly IConfigService? _configService;
    private readonly INamingService? _namingService;
    private readonly HttpClient? _httpClient;
    private readonly bool _ownsClient;
    private readonly Func<HttpClient>? _clientFactory;
    private int _disposed;
    private readonly NacosHealthCheckMode _mode;
    private readonly NacosHealthCheckClients _requiredClients;
    private readonly NacosHealthCheckStrategy _strategy;
    private readonly bool _probeAll;
    private readonly TimeSpan _endpointTimeout;
    private readonly TimeSpan _totalTimeout;
    private readonly SemaphoreSlim _slots;
    private readonly (string Component, Uri Url)[] _endpoints;
    private readonly string[] _components;

    /// <summary>Checks Config client state, or default Console readiness when no client is supplied.</summary>
    public NacosHealthCheck(IConfigService? configService = null)
        : this(new(), new() { Mode = configService == null ? NacosHealthCheckMode.Console : NacosHealthCheckMode.ClientConnections }, configService: configService) { }
    /// <summary>Creates an owned reusable HTTP client. Dispose this check when no longer needed.</summary>
    public NacosHealthCheck(NacosClientOptions options) : this(options, new NacosHealthCheckOptions()) { }
    /// <summary>Uses a caller-owned client without disposing it or changing its Timeout.</summary>
    public NacosHealthCheck(NacosClientOptions options, HttpClient httpClient) : this(options, new NacosHealthCheckOptions(), httpClient) { }
    /// <summary>Configures node probes or supplied Config/Naming client state. External clients remain caller-owned.</summary>
    public NacosHealthCheck(NacosClientOptions options, NacosHealthCheckOptions healthOptions,
        HttpClient? httpClient = null, IConfigService? configService = null, INamingService? namingService = null)
        : this(options, healthOptions, httpClient, configService, namingService, null) { }

    private NacosHealthCheck(NacosClientOptions options, NacosHealthCheckOptions healthOptions,
        HttpClient? httpClient, IConfigService? configService, INamingService? namingService, Func<HttpClient>? clientFactory)
    {
        _clientFactory = clientFactory;
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(healthOptions);
        if (!Enum.IsDefined(healthOptions.Mode) || !Enum.IsDefined(healthOptions.RequiredClients) || !Enum.IsDefined(healthOptions.Strategy) || healthOptions.MaxConcurrency < 1 ||
            healthOptions.EndpointTimeout <= TimeSpan.Zero || healthOptions.TotalTimeout <= TimeSpan.Zero)
            throw new ArgumentException("Health mode, concurrency and timeouts must be valid.", nameof(healthOptions));
        _mode = healthOptions.Mode; _strategy = healthOptions.Strategy; _probeAll = healthOptions.ProbeAll;
        _requiredClients = healthOptions.RequiredClients;
        _endpointTimeout = healthOptions.EndpointTimeout; _totalTimeout = healthOptions.TotalTimeout;
        _configService = configService; _namingService = namingService;
        _slots = new SemaphoreSlim(healthOptions.MaxConcurrency);
        _components = _mode switch
        {
            NacosHealthCheckMode.Server => ["Server"],
            NacosHealthCheckMode.Console => ["Console"],
            NacosHealthCheckMode.ServerAndConsole => ["Server", "Console"],
            _ => []
        };
        _endpoints = _components.SelectMany(component =>
            (component == "Server" ? options.GetServerAddressList() : options.GetConsoleAddressList()).Distinct(StringComparer.Ordinal)
                .Select(address => (component, new Uri(component == "Server"
                    ? options.GetBaseUrl(address).TrimEnd('/') + "/v3/admin/core/state/readiness"
                    : options.GetConsoleBaseUrl(address).TrimEnd('/') + "/v3/console/health/readiness")))).ToArray();
        if (_mode != NacosHealthCheckMode.ClientConnections && clientFactory == null)
        {
            _ownsClient = httpClient == null;
            _httpClient = httpClient ?? new HttpClient(NacosTransport.CreateHandler(options)) { Timeout = Timeout.InfiniteTimeSpan };
        }
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (_mode == NacosHealthCheckMode.ClientConnections)
        {
            var clients = new List<NacosNodeHealthResult>();
            void Check(string name, Func<string>? status)
            {
                if (status == null) { clients.Add(new(name, "", false, 0, null, "ClientNotRegistered")); return; }
                try { var up = status() == "UP"; clients.Add(new(name, "", up, 0, null, up ? null : "NotConnected")); }
                catch { clients.Add(new(name, "", false, 0, null, "ClientStateError")); }
            }
            if (_requiredClients.HasFlag(NacosHealthCheckClients.Config)) Check("Config", _configService == null ? null : _configService.GetServerStatus);
            if (_requiredClients.HasFlag(NacosHealthCheckClients.Naming)) Check("Naming", _namingService == null ? null : _namingService.GetServerStatus);
            cancellationToken.ThrowIfCancellationRequested();
            return new HealthCheckResult(clients.Count > 0 && clients.All(c => c.Healthy) ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                "Registered Nacos client connectivity", data: Data(clients));
        }
        using var clientLease = _clientFactory?.Invoke();
        var client = clientLease ?? _httpClient!;
        using var deadline = new CancellationTokenSource(_totalTimeout);
        using var work = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        var tasks = _endpoints.Select(e => ProbeAsync(e, client, work.Token)).ToArray();
        var pending = tasks.ToList();
        var completed = new List<NacosNodeHealthResult>();
        while (pending.Count > 0)
        {
            var task = await Task.WhenAny(pending).ConfigureAwait(false);
            pending.Remove(task);
            completed.Add(await task.ConfigureAwait(false));
            if (_strategy == NacosHealthCheckStrategy.Any && !_probeAll &&
                _components.All(c => completed.Any(r => r.Component == c && r.Healthy)))
            {
                await work.CancelAsync().ConfigureAwait(false);
                break;
            }
        }
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var available = _components.All(c => results.Any(r => r.Component == c && r.Healthy));
        var status = !available ? HealthStatus.Unhealthy
            : _strategy == NacosHealthCheckStrategy.All && results.Any(r => !r.Healthy) ? HealthStatus.Degraded : HealthStatus.Healthy;
        return new HealthCheckResult(status, "Nacos HTTP readiness (not Config/Naming/gRPC connectivity)", data: Data(results));
    }

    private async Task<NacosNodeHealthResult> ProbeAsync((string Component, Uri Url) endpoint, HttpClient client, CancellationToken deadline)
    {
        var watch = Stopwatch.StartNew();
        var address = endpoint.Url.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
        var entered = false;
        try
        {
            await _slots.WaitAsync(deadline).ConfigureAwait(false);
            entered = true;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(deadline);
            timeout.CancelAfter(_endpointTimeout);
            using var response = await client.GetAsync(endpoint.Url, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            return new(endpoint.Component, address, response.IsSuccessStatusCode, watch.ElapsedMilliseconds, (int)response.StatusCode,
                response.IsSuccessStatusCode ? null : "HttpStatus");
        }
        catch (OperationCanceledException)
        { return new(endpoint.Component, address, false, watch.ElapsedMilliseconds, null, deadline.IsCancellationRequested ? "CancelledOrDeadline" : "Timeout"); }
        catch (HttpRequestException)
        { return new(endpoint.Component, address, false, watch.ElapsedMilliseconds, null, "TransportError"); }
        finally { if (entered) _slots.Release(); }
    }
    private static IReadOnlyDictionary<string, object> Data(IEnumerable<NacosNodeHealthResult> nodes) =>
        new Dictionary<string, object> { ["nodes"] = nodes.ToArray() };
    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        if (_ownsClient) _httpClient?.Dispose();
        _slots.Dispose();
    }

    internal static NacosHealthCheck FromFactory(NacosClientOptions options, NacosHealthCheckOptions health,
        IHttpClientFactory factory, string name, IConfigService? config, INamingService? naming) =>
        new(options, health, null, config, naming, () => factory.CreateClient(name));
}
