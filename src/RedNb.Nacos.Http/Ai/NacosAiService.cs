using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Client.Http;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Ai;
using RedNb.Nacos.Core.Ai.Listener;
using RedNb.Nacos.Core.Ai.Model;
using RedNb.Nacos.Core.Ai.Model.A2a;
using RedNb.Nacos.Core.Ai.Model.Mcp;
using RedNb.Nacos.Utils;

namespace RedNb.Nacos.Client.Ai;

/// <summary>
/// Nacos AI service implementation using HTTP.
/// Provides both A2A (Agent-to-Agent) and MCP (Model Context Protocol) capabilities.
/// </summary>
public class NacosAiService : IAiService
{
    private readonly NacosClientOptions _options;
    private readonly NacosHttpClient _httpClient;
    private readonly ILogger<NacosAiService>? _logger;
    private readonly AiListenerManager _listenerManager;
    private readonly AiCacheHolder _cacheHolder;
    private readonly CancellationTokenSource _cts;
    private readonly string _namespaceId;
    private bool _disposed;

    // API paths
    private const string McpBasePath = "v3/console/ai/mcp";
    private const string A2aBasePath = "v3/console/ai/a2a";

    // Polling interval for subscriptions (in milliseconds)
    private const int PollingIntervalMs = 10000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public NacosAiService(NacosClientOptions options, ILogger<NacosAiService>? logger = null)
    {
        _options = options;
        _logger = logger;
        _httpClient = new NacosHttpClient(options, logger);
        _listenerManager = new AiListenerManager();
        _cacheHolder = new AiCacheHolder();
        _cts = new CancellationTokenSource();
        _namespaceId = options.Namespace ?? string.Empty;

        // Start background polling for subscriptions
        _ = StartPollingAsync(_cts.Token);
    }

    #region MCP Server Operations

    /// <inheritdoc />
    public Task<McpServerDetailInfo?> GetMcpServerAsync(string mcpName, CancellationToken cancellationToken = default)
    {
        return GetMcpServerAsync(mcpName, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<McpServerDetailInfo?> GetMcpServerAsync(string mcpName, string? version, CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "mcpName", mcpName },
            { "version", version }
        };

        try
        {
            var response = await _httpClient.GetAsync(McpBasePath, parameters, _options.DefaultTimeout, cancellationToken);
            if (string.IsNullOrEmpty(response))
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<ApiResult<McpServerDetailInfo>>(response, JsonOptions);
            return result?.Data;
        }
        catch (NacosException ex) when (ex.ErrorCode == NacosException.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public Task<string> ReleaseMcpServerAsync(McpServerBasicInfo serverSpecification, McpToolSpecification? toolSpecification, CancellationToken cancellationToken = default)
    {
        return ReleaseMcpServerAsync(serverSpecification, toolSpecification, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ReleaseMcpServerAsync(McpServerBasicInfo serverSpecification, McpToolSpecification? toolSpecification, McpEndpointSpec? endpointSpecification, CancellationToken cancellationToken = default)
    {
        if (serverSpecification == null)
        {
            throw new NacosException(NacosException.InvalidParam, "serverSpecification is required");
        }

        if (string.IsNullOrWhiteSpace(serverSpecification.Name))
        {
            throw new NacosException(NacosException.InvalidParam, "serverSpecification.Name is required");
        }

        if (serverSpecification.VersionDetail == null || string.IsNullOrWhiteSpace(serverSpecification.VersionDetail.Version))
        {
            throw new NacosException(NacosException.InvalidParam, "serverSpecification.VersionDetail.Version is required");
        }

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "mcpName", serverSpecification.Name },
            { "serverSpec", JsonSerializer.Serialize(serverSpecification, JsonOptions) }
        };

        if (toolSpecification != null)
        {
            parameters["toolSpec"] = JsonSerializer.Serialize(toolSpecification, JsonOptions);
        }

        if (endpointSpecification != null)
        {
            parameters["endpointSpec"] = JsonSerializer.Serialize(endpointSpecification, JsonOptions);
        }

        var body = NacosUtils.BuildQueryString(parameters);
        var response = await _httpClient.PostAsync(McpBasePath, null, body, _options.DefaultTimeout, cancellationToken);

        var result = JsonSerializer.Deserialize<ApiResult<string>>(response ?? "{}", JsonOptions);
        return result?.Data ?? string.Empty;
    }

    /// <inheritdoc />
    public Task RegisterMcpServerEndpointAsync(string mcpName, string address, int port, CancellationToken cancellationToken = default)
    {
        return RegisterMcpServerEndpointAsync(mcpName, address, port, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RegisterMcpServerEndpointAsync(string mcpName, string address, int port, string? version, CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);
        ValidateEndpoint(address, port);

        _logger?.LogInformation("Registering MCP endpoint {Address}:{Port} to {McpName}", address, port, mcpName);

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "mcpName", mcpName },
            { "address", address },
            { "port", port.ToString() },
            { "version", version },
            { "type", "register" }
        };

        var body = NacosUtils.BuildQueryString(parameters);
        await _httpClient.PostAsync($"{McpBasePath}/endpoint", null, body, _options.DefaultTimeout, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeregisterMcpServerEndpointAsync(string mcpName, string address, int port, CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);
        ValidateEndpoint(address, port);

        _logger?.LogInformation("Deregistering MCP endpoint {Address}:{Port} from {McpName}", address, port, mcpName);

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "mcpName", mcpName },
            { "address", address },
            { "port", port.ToString() },
            { "type", "deregister" }
        };

        await _httpClient.DeleteAsync($"{McpBasePath}/endpoint", parameters, _options.DefaultTimeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<McpServerDetailInfo?> SubscribeMcpServerAsync(string mcpName, AbstractNacosMcpServerListener listener, CancellationToken cancellationToken = default)
    {
        return SubscribeMcpServerAsync(mcpName, null, listener, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<McpServerDetailInfo?> SubscribeMcpServerAsync(string mcpName, string? version, AbstractNacosMcpServerListener listener, CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);
        if (listener == null)
        {
            throw new NacosException(NacosException.InvalidParam, "listener is required");
        }

        _listenerManager.AddMcpListener(mcpName, version, listener);

        // Try to get current value
        var cached = _cacheHolder.GetMcpServer(mcpName, version);
        if (cached == null)
        {
            try
            {
                cached = await GetMcpServerAsync(mcpName, version, cancellationToken);
                if (cached != null)
                {
                    _cacheHolder.UpdateMcpServer(mcpName, version, cached);
                    // Notify listener immediately
                    listener.OnEvent(new NacosMcpServerEvent(cached));
                }
            }
            catch (NacosException ex) when (ex.ErrorCode == NacosException.NotFound)
            {
                // Ignore not found
            }
        }
        else
        {
            // Notify with cached value
            listener.OnEvent(new NacosMcpServerEvent(cached));
        }

        return cached;
    }

    /// <inheritdoc />
    public Task UnsubscribeMcpServerAsync(string mcpName, AbstractNacosMcpServerListener listener, CancellationToken cancellationToken = default)
    {
        return UnsubscribeMcpServerAsync(mcpName, null, listener, cancellationToken);
    }

    /// <inheritdoc />
    public Task UnsubscribeMcpServerAsync(string mcpName, string? version, AbstractNacosMcpServerListener listener, CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);
        if (listener == null)
        {
            return Task.CompletedTask;
        }

        var shouldRemoveCache = _listenerManager.RemoveMcpListener(mcpName, version, listener);
        if (shouldRemoveCache)
        {
            _cacheHolder.RemoveMcpServer(mcpName, version);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteMcpServerAsync(string mcpName, string? version = null, CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);

        _logger?.LogInformation("Deleting MCP server {McpName}@{Version}", mcpName, version ?? "all");

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "mcpName", mcpName },
            { "version", version }
        };

        await _httpClient.DeleteAsync(McpBasePath, parameters, _options.DefaultTimeout, cancellationToken);

        // Remove from cache
        _cacheHolder.RemoveMcpServer(mcpName, version);

        _logger?.LogInformation("Deleted MCP server {McpName}@{Version}", mcpName, version ?? "all");
    }

    /// <inheritdoc />
    public async Task<PageResult<McpServerBasicInfo>> ListMcpServersAsync(
        string? mcpName = null,
        string? search = null,
        int pageNo = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNo < 1) pageNo = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "mcpName", mcpName },
            { "search", search },
            { "pageNo", pageNo.ToString() },
            { "pageSize", pageSize.ToString() }
        };

        try
        {
            var response = await _httpClient.GetAsync($"{McpBasePath}/list", parameters, _options.DefaultTimeout, cancellationToken);
            if (string.IsNullOrEmpty(response))
            {
                return PageResult<McpServerBasicInfo>.Empty(pageNo, pageSize);
            }

            var result = JsonSerializer.Deserialize<ApiResult<PagedData<McpServerBasicInfo>>>(response, JsonOptions);
            if (result?.Data == null)
            {
                return PageResult<McpServerBasicInfo>.Empty(pageNo, pageSize);
            }

            return PageResult<McpServerBasicInfo>.FromItems(
                result.Data.PageItems ?? new List<McpServerBasicInfo>(),
                result.Data.TotalCount,
                pageNo,
                pageSize);
        }
        catch (NacosException ex) when (ex.ErrorCode == NacosException.NotFound)
        {
            return PageResult<McpServerBasicInfo>.Empty(pageNo, pageSize);
        }
    }

    #endregion

    #region Agent Card Operations

    /// <inheritdoc />
    public Task<AgentCardDetailInfo?> GetAgentCardAsync(string agentName, CancellationToken cancellationToken = default)
    {
        return GetAgentCardAsync(agentName, null, null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentCardDetailInfo?> GetAgentCardAsync(string agentName, string? version, CancellationToken cancellationToken = default)
    {
        return GetAgentCardAsync(agentName, version, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AgentCardDetailInfo?> GetAgentCardAsync(string agentName, string? version, string? registrationType, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "agentName", agentName },
            { "version", version },
            { "registrationType", registrationType }
        };

        try
        {
            var response = await _httpClient.GetAsync(A2aBasePath, parameters, _options.DefaultTimeout, cancellationToken);
            if (string.IsNullOrEmpty(response))
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<ApiResult<AgentCardDetailInfo>>(response, JsonOptions);
            return result?.Data;
        }
        catch (NacosException ex) when (ex.ErrorCode == NacosException.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public Task ReleaseAgentCardAsync(AgentCard agentCard, CancellationToken cancellationToken = default)
    {
        return ReleaseAgentCardAsync(agentCard, AiConstants.A2a.A2aEndpointTypeService, false, cancellationToken);
    }

    /// <inheritdoc />
    public Task ReleaseAgentCardAsync(AgentCard agentCard, string registrationType, CancellationToken cancellationToken = default)
    {
        return ReleaseAgentCardAsync(agentCard, registrationType, false, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReleaseAgentCardAsync(AgentCard agentCard, string registrationType, bool setAsLatest, CancellationToken cancellationToken = default)
    {
        if (agentCard == null)
        {
            throw new NacosException(NacosException.InvalidParam, "agentCard is required");
        }

        ValidateAgentCardFields(agentCard);

        if (string.IsNullOrWhiteSpace(registrationType))
        {
            registrationType = AiConstants.A2a.A2aEndpointTypeService;
        }

        _logger?.LogInformation("Releasing Agent Card {AgentName}, version {Version}", agentCard.Name, agentCard.Version);

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "agentName", agentCard.Name },
            { "registrationType", registrationType },
            { "setAsLatest", setAsLatest.ToString().ToLowerInvariant() },
            { "agentCard", JsonSerializer.Serialize(agentCard, JsonOptions) }
        };

        var body = NacosUtils.BuildQueryString(parameters);
        await _httpClient.PostAsync(A2aBasePath, null, body, _options.DefaultTimeout, cancellationToken);
    }

    #endregion

    #region Agent Endpoint Operations

    /// <inheritdoc />
    public Task RegisterAgentEndpointAsync(string agentName, string version, string address, int port, CancellationToken cancellationToken = default)
    {
        return RegisterAgentEndpointAsync(agentName, version, address, port, AiConstants.A2a.TransportJsonRpc, cancellationToken);
    }

    /// <inheritdoc />
    public Task RegisterAgentEndpointAsync(string agentName, string version, string address, int port, string transport, CancellationToken cancellationToken = default)
    {
        var endpoint = new AgentEndpoint
        {
            Address = address,
            Port = port,
            Version = version,
            Transport = transport
        };
        return RegisterAgentEndpointAsync(agentName, endpoint, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RegisterAgentEndpointAsync(string agentName, AgentEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);
        ValidateAgentEndpoint(endpoint);

        _logger?.LogInformation("Registering Agent endpoint {Address}:{Port} to {AgentName}", endpoint.Address, endpoint.Port, agentName);

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "agentName", agentName },
            { "type", "register" },
            { "endpoint", JsonSerializer.Serialize(endpoint, JsonOptions) }
        };

        var body = NacosUtils.BuildQueryString(parameters);
        await _httpClient.PostAsync($"{A2aBasePath}/endpoint", null, body, _options.DefaultTimeout, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RegisterAgentEndpointsAsync(string agentName, IEnumerable<AgentEndpoint> endpoints, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);
        var endpointList = endpoints?.ToList() ?? throw new NacosException(NacosException.InvalidParam, "endpoints is required");
        
        if (endpointList.Count == 0)
        {
            throw new NacosException(NacosException.InvalidParam, "endpoints cannot be empty");
        }

        // Validate all endpoints have the same version
        var versions = endpointList.Select(e => e.Version).Distinct().ToList();
        if (versions.Count > 1)
        {
            throw new NacosException(NacosException.InvalidParam, $"All endpoints must have the same version, current versions: {string.Join(",", versions)}");
        }

        foreach (var endpoint in endpointList)
        {
            ValidateAgentEndpoint(endpoint);
        }

        _logger?.LogInformation("Batch registering {Count} Agent endpoints to {AgentName}", endpointList.Count, agentName);

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "agentName", agentName },
            { "endpoints", JsonSerializer.Serialize(endpointList, JsonOptions) }
        };

        var body = NacosUtils.BuildQueryString(parameters);
        await _httpClient.PostAsync($"{A2aBasePath}/endpoints", null, body, _options.DefaultTimeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeregisterAgentEndpointAsync(string agentName, string version, string address, int port, CancellationToken cancellationToken = default)
    {
        var endpoint = new AgentEndpoint
        {
            Address = address,
            Port = port,
            Version = version
        };
        return DeregisterAgentEndpointAsync(agentName, endpoint, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeregisterAgentEndpointAsync(string agentName, AgentEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);
        ValidateAgentEndpoint(endpoint);

        _logger?.LogInformation("Deregistering Agent endpoint {Address}:{Port} from {AgentName}", endpoint.Address, endpoint.Port, agentName);

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "agentName", agentName },
            { "type", "deregister" },
            { "endpoint", JsonSerializer.Serialize(endpoint, JsonOptions) }
        };

        await _httpClient.DeleteAsync($"{A2aBasePath}/endpoint", parameters, _options.DefaultTimeout, cancellationToken);
    }

    #endregion

    #region Agent Card Subscription

    /// <inheritdoc />
    public Task<AgentCardDetailInfo?> SubscribeAgentCardAsync(string agentName, AbstractNacosAgentCardListener listener, CancellationToken cancellationToken = default)
    {
        return SubscribeAgentCardAsync(agentName, null, listener, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AgentCardDetailInfo?> SubscribeAgentCardAsync(string agentName, string? version, AbstractNacosAgentCardListener listener, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);
        if (listener == null)
        {
            throw new NacosException(NacosException.InvalidParam, "listener is required");
        }

        _listenerManager.AddAgentListener(agentName, version, listener);

        // Try to get current value
        var cached = _cacheHolder.GetAgentCard(agentName, version);
        if (cached == null)
        {
            try
            {
                cached = await GetAgentCardAsync(agentName, version, cancellationToken);
                if (cached != null)
                {
                    _cacheHolder.UpdateAgentCard(agentName, version, cached);
                    // Notify listener immediately
                    listener.OnEvent(new NacosAgentCardEvent(cached));
                }
            }
            catch (NacosException ex) when (ex.ErrorCode == NacosException.NotFound)
            {
                // Ignore not found
            }
        }
        else
        {
            // Notify with cached value
            listener.OnEvent(new NacosAgentCardEvent(cached));
        }

        return cached;
    }

    /// <inheritdoc />
    public Task UnsubscribeAgentCardAsync(string agentName, AbstractNacosAgentCardListener listener, CancellationToken cancellationToken = default)
    {
        return UnsubscribeAgentCardAsync(agentName, null, listener, cancellationToken);
    }

    /// <inheritdoc />
    public Task UnsubscribeAgentCardAsync(string agentName, string? version, AbstractNacosAgentCardListener listener, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);
        if (listener == null)
        {
            return Task.CompletedTask;
        }

        var shouldRemoveCache = _listenerManager.RemoveAgentListener(agentName, version, listener);
        if (shouldRemoveCache)
        {
            _cacheHolder.RemoveAgentCard(agentName, version);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteAgentAsync(string agentName, string? version = null, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);

        _logger?.LogInformation("Deleting Agent {AgentName}@{Version}", agentName, version ?? "all");

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "agentName", agentName },
            { "version", version }
        };

        await _httpClient.DeleteAsync(A2aBasePath, parameters, _options.DefaultTimeout, cancellationToken);

        // Remove from cache
        _cacheHolder.RemoveAgentCard(agentName, version);

        _logger?.LogInformation("Deleted Agent {AgentName}@{Version}", agentName, version ?? "all");
    }

    /// <inheritdoc />
    public async Task<PageResult<AgentCardBasicInfo>> ListAgentCardsAsync(
        string? agentName = null,
        string? search = null,
        int pageNo = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNo < 1) pageNo = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "agentName", agentName },
            { "search", search },
            { "pageNo", pageNo.ToString() },
            { "pageSize", pageSize.ToString() }
        };

        try
        {
            var response = await _httpClient.GetAsync($"{A2aBasePath}/list", parameters, _options.DefaultTimeout, cancellationToken);
            if (string.IsNullOrEmpty(response))
            {
                return PageResult<AgentCardBasicInfo>.Empty(pageNo, pageSize);
            }

            var result = JsonSerializer.Deserialize<ApiResult<PagedData<AgentCardBasicInfo>>>(response, JsonOptions);
            if (result?.Data == null)
            {
                return PageResult<AgentCardBasicInfo>.Empty(pageNo, pageSize);
            }

            return PageResult<AgentCardBasicInfo>.FromItems(
                result.Data.PageItems ?? new List<AgentCardBasicInfo>(),
                result.Data.TotalCount,
                pageNo,
                pageSize);
        }
        catch (NacosException ex) when (ex.ErrorCode == NacosException.NotFound)
        {
            return PageResult<AgentCardBasicInfo>.Empty(pageNo, pageSize);
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> ListAgentVersionsAsync(string agentName, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);

        var parameters = new Dictionary<string, string?>
        {
            { "namespaceId", _namespaceId },
            { "agentName", agentName }
        };

        try
        {
            var response = await _httpClient.GetAsync($"{A2aBasePath}/versions", parameters, _options.DefaultTimeout, cancellationToken);
            if (string.IsNullOrEmpty(response))
            {
                return new List<string>();
            }

            var result = JsonSerializer.Deserialize<ApiResult<List<string>>>(response, JsonOptions);
            return result?.Data ?? new List<string>();
        }
        catch (NacosException ex) when (ex.ErrorCode == NacosException.NotFound)
        {
            return new List<string>();
        }
    }

    #endregion

    #region Lifecycle

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        await DisposeAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await _cts.CancelAsync();
        _cts.Dispose();
        _listenerManager.Clear();
        _cacheHolder.Clear();
        _httpClient.Dispose();
        _disposed = true;
    }

    #endregion

    #region Private Methods

    private async Task StartPollingAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting AI subscription polling");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollingIntervalMs, cancellationToken);

                var subscriptions = _listenerManager.GetAllSubscriptions();
                foreach (var (name, version, isMcp) in subscriptions)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        if (isMcp)
                        {
                            await PollMcpServerAsync(name, version, cancellationToken);
                        }
                        else
                        {
                            await PollAgentCardAsync(name, version, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error polling {Type} {Name}@{Version}", isMcp ? "MCP" : "Agent", name, version);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in AI subscription polling");
            }
        }

        _logger?.LogInformation("AI subscription polling stopped");
    }

    private async Task PollMcpServerAsync(string mcpName, string? version, CancellationToken cancellationToken)
    {
        var current = await GetMcpServerAsync(mcpName, version, cancellationToken);
        var cached = _cacheHolder.GetMcpServer(mcpName, version);

        // Simple change detection using version comparison
        var currentVersion = current?.VersionDetail?.Version;
        var cachedVersion = cached?.VersionDetail?.Version;

        if (currentVersion != cachedVersion || !string.Equals(
            JsonSerializer.Serialize(current, JsonOptions),
            JsonSerializer.Serialize(cached, JsonOptions)))
        {
            _cacheHolder.UpdateMcpServer(mcpName, version, current);
            if (current != null)
            {
                _listenerManager.NotifyMcpListeners(mcpName, version, current);
            }
        }
    }

    private async Task PollAgentCardAsync(string agentName, string? version, CancellationToken cancellationToken)
    {
        var current = await GetAgentCardAsync(agentName, version, cancellationToken);
        var cached = _cacheHolder.GetAgentCard(agentName, version);

        // Simple change detection using version comparison
        var currentVersion = current?.LatestVersion;
        var cachedVersion = cached?.LatestVersion;

        if (currentVersion != cachedVersion || !string.Equals(
            JsonSerializer.Serialize(current, JsonOptions),
            JsonSerializer.Serialize(cached, JsonOptions)))
        {
            _cacheHolder.UpdateAgentCard(agentName, version, current);
            if (current != null)
            {
                _listenerManager.NotifyAgentListeners(agentName, version, current);
            }
        }
    }

    private static void ValidateMcpName(string mcpName)
    {
        if (string.IsNullOrWhiteSpace(mcpName))
        {
            throw new NacosException(NacosException.InvalidParam, "mcpName is required");
        }
    }

    private static void ValidateAgentName(string agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            throw new NacosException(NacosException.InvalidParam, "agentName is required");
        }
    }

    private static void ValidateEndpoint(string address, int port)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new NacosException(NacosException.InvalidParam, "address is required");
        }

        if (port <= 0 || port > 65535)
        {
            throw new NacosException(NacosException.InvalidParam, "port must be between 1 and 65535");
        }
    }

    private static void ValidateAgentEndpoint(AgentEndpoint? endpoint)
    {
        if (endpoint == null)
        {
            throw new NacosException(NacosException.InvalidParam, "endpoint is required");
        }

        if (string.IsNullOrWhiteSpace(endpoint.Version))
        {
            throw new NacosException(NacosException.InvalidParam, "endpoint.Version is required");
        }

        ValidateEndpoint(endpoint.Address ?? string.Empty, endpoint.Port);
    }

    private static void ValidateAgentCardFields(AgentCard agentCard)
    {
        if (string.IsNullOrWhiteSpace(agentCard.Name))
        {
            throw new NacosException(NacosException.InvalidParam, "agentCard.Name is required");
        }

        if (string.IsNullOrWhiteSpace(agentCard.Version))
        {
            throw new NacosException(NacosException.InvalidParam, "agentCard.Version is required");
        }

        if (string.IsNullOrWhiteSpace(agentCard.ProtocolVersion))
        {
            throw new NacosException(NacosException.InvalidParam, "agentCard.ProtocolVersion is required");
        }
    }

    #endregion

    /// <summary>
    /// API result wrapper for Nacos v2 API responses.
    /// </summary>
    private class ApiResult<T>
    {
        public int Code { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    /// <summary>
    /// Paged data from API response.
    /// </summary>
    private class PagedData<T>
    {
        public int TotalCount { get; set; }
        public List<T>? PageItems { get; set; }
    }
}
