using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Ai;
using RedNb.Nacos.Core.Ai.Listener;
using RedNb.Nacos.Core.Ai.Model;
using RedNb.Nacos.Core.Ai.Model.A2a;
using RedNb.Nacos.Core.Ai.Model.Mcp;
using RedNb.Nacos.Core.Ai.Model.Mcp.Import;
using RedNb.Nacos.Core.Ai.Model.Mcp.Validation;

namespace RedNb.Nacos.GrpcClient.Ai;

/// <summary>
/// Nacos AI service implementation using gRPC.
/// Provides both A2A (Agent-to-Agent) and MCP (Model Context Protocol) capabilities.
/// </summary>
public class NacosGrpcAiService : IAiService
{
    private readonly NacosClientOptions _options;
    private readonly NacosGrpcClient _grpcClient;
    private readonly ILogger<NacosGrpcAiService>? _logger;
    private readonly string _namespaceId;
    
    private readonly ConcurrentDictionary<string, McpServerDetailInfo?> _mcpCache = new();
    private readonly ConcurrentDictionary<string, AgentCardDetailInfo?> _agentCache = new();
    private readonly ConcurrentDictionary<string, List<AbstractNacosMcpServerListener>> _mcpListeners = new();
    private readonly ConcurrentDictionary<string, List<AbstractNacosAgentCardListener>> _agentListeners = new();
    
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Creates a new NacosGrpcAiService.
    /// </summary>
    public NacosGrpcAiService(NacosClientOptions options, ILogger<NacosGrpcAiService>? logger = null)
    {
        _options = options;
        _logger = logger;
        _grpcClient = new NacosGrpcClient(options, logger);
        _namespaceId = options.Namespace ?? string.Empty;

        // Register push handler
        _grpcClient.RegisterPushHandler(HandlePushMessage);
    }

    /// <summary>
    /// Initializes the gRPC connection.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _grpcClient.ConnectAsync(cancellationToken);
        _logger?.LogInformation("NacosGrpcAiService initialized");
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

        var request = new McpServerQueryRequest
        {
            Namespace = _namespaceId,
            McpName = mcpName,
            Version = version
        };

        var response = await _grpcClient.RequestAsync<McpServerQueryResponse>(
            "McpServerQueryRequest", request, cancellationToken);

        return response?.McpServerDetailInfo;
    }

    /// <inheritdoc />
    public Task<string> ReleaseMcpServerAsync(McpServerBasicInfo serverSpecification, McpToolSpecification? toolSpecification, CancellationToken cancellationToken = default)
    {
        return ReleaseMcpServerAsync(serverSpecification, toolSpecification, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ReleaseMcpServerAsync(McpServerBasicInfo serverSpecification, McpToolSpecification? toolSpecification, McpEndpointSpec? endpointSpecification, CancellationToken cancellationToken = default)
    {
        ValidateMcpServerSpec(serverSpecification);

        var request = new McpServerReleaseRequest
        {
            Namespace = _namespaceId,
            McpName = serverSpecification.Name!,
            ServerSpecification = serverSpecification,
            ToolSpecification = toolSpecification,
            EndpointSpecification = endpointSpecification
        };

        var response = await _grpcClient.RequestAsync<McpServerReleaseResponse>(
            "McpServerReleaseRequest", request, cancellationToken);

        return response?.McpServerId ?? string.Empty;
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

        var request = new McpEndpointRequest
        {
            Namespace = _namespaceId,
            McpName = mcpName,
            Address = address,
            Port = port,
            Version = version,
            Type = "register"
        };

        var response = await _grpcClient.RequestAsync<OperationResponse>(
            "McpEndpointRequest", request, cancellationToken);

        if (response?.Success != true)
        {
            throw new NacosException(NacosException.ServerError, "Failed to register MCP endpoint");
        }

        _logger?.LogInformation("Registered MCP endpoint {Address}:{Port} to {McpName}", address, port, mcpName);
    }

    /// <inheritdoc />
    public async Task DeregisterMcpServerEndpointAsync(string mcpName, string address, int port, CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);
        ValidateEndpoint(address, port);

        var request = new McpEndpointRequest
        {
            Namespace = _namespaceId,
            McpName = mcpName,
            Address = address,
            Port = port,
            Type = "deregister"
        };

        var response = await _grpcClient.RequestAsync<OperationResponse>(
            "McpEndpointRequest", request, cancellationToken);

        if (response?.Success != true)
        {
            throw new NacosException(NacosException.ServerError, "Failed to deregister MCP endpoint");
        }

        _logger?.LogInformation("Deregistered MCP endpoint {Address}:{Port} from {McpName}", address, port, mcpName);
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
        if (listener == null) throw new NacosException(NacosException.InvalidParam, "listener is required");

        var key = GetMcpKey(mcpName, version);
        
        _mcpListeners.AddOrUpdate(key,
            _ => new List<AbstractNacosMcpServerListener> { listener },
            (_, list) => { list.Add(listener); return list; });

        // Send subscribe request
        var request = new McpServerSubscribeRequest
        {
            Namespace = _namespaceId,
            McpName = mcpName,
            Version = version,
            Subscribe = true
        };

        await _grpcClient.SendStreamRequestAsync("McpServerSubscribeRequest", request);

        // Get current value
        var current = await GetMcpServerAsync(mcpName, version, cancellationToken);
        if (current != null)
        {
            _mcpCache[key] = current;
            listener.OnEvent(new NacosMcpServerEvent(current));
        }

        _logger?.LogDebug("Subscribed to MCP server {McpName}@{Version}", mcpName, version);
        return current;
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
        if (listener == null) return Task.CompletedTask;

        var key = GetMcpKey(mcpName, version);

        if (_mcpListeners.TryGetValue(key, out var list))
        {
            list.Remove(listener);
            if (list.Count == 0)
            {
                _mcpListeners.TryRemove(key, out _);
                _mcpCache.TryRemove(key, out _);
            }
        }

        _logger?.LogDebug("Unsubscribed from MCP server {McpName}@{Version}", mcpName, version);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteMcpServerAsync(string mcpName, string? version = null, CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);

        var request = new McpServerDeleteRequest
        {
            Namespace = _namespaceId,
            McpName = mcpName,
            Version = version
        };

        var response = await _grpcClient.RequestAsync<OperationResponse>(
            "McpServerDeleteRequest", request, cancellationToken);

        if (response?.Success != true)
        {
            throw new NacosException(NacosException.ServerError, response?.Message ?? "Failed to delete MCP server");
        }

        // Remove from cache
        var key = GetMcpKey(mcpName, version);
        _mcpCache.TryRemove(key, out _);

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

        var request = new McpServerListRequest
        {
            Namespace = _namespaceId,
            McpName = mcpName,
            Search = search,
            PageNo = pageNo,
            PageSize = pageSize
        };

        var response = await _grpcClient.RequestAsync<McpServerListResponse>(
            "McpServerListRequest", request, cancellationToken);

        if (response == null)
        {
            return PageResult<McpServerBasicInfo>.Empty(pageNo, pageSize);
        }

        return PageResult<McpServerBasicInfo>.FromItems(
            response.McpServers ?? new List<McpServerBasicInfo>(),
            response.TotalCount,
            pageNo,
            pageSize);
    }

    #endregion

    #region MCP Server Import/Validation

    /// <inheritdoc />
    public async Task<McpServerImportValidationResult> ValidateImportAsync(
        McpServerImportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new NacosException(NacosException.InvalidParam, "request is required");

        var grpcRequest = new McpServerValidateImportRequest
        {
            Namespace = _namespaceId,
            ImportType = request.ImportType.ToString().ToLowerInvariant(),
            ImportData = request.ImportData,
            OverrideExisting = request.OverrideExisting
        };

        var response = await _grpcClient.RequestAsync<McpServerValidateImportResponse>(
            "McpServerValidateImportRequest", grpcRequest, cancellationToken);

        if (response == null)
        {
            return McpServerImportValidationResult.Failed(new List<string> { "Failed to get validation response from server" });
        }

        return new McpServerImportValidationResult
        {
            IsValid = response.IsValid,
            Errors = response.Errors ?? new List<string>(),
            Servers = response.Servers ?? new List<McpServerValidationItem>(),
            ValidCount = response.ValidCount,
            InvalidCount = response.InvalidCount,
            DuplicateCount = response.DuplicateCount
        };
    }

    /// <inheritdoc />
    public async Task<McpServerImportResponse> ImportMcpServersAsync(
        McpServerImportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new NacosException(NacosException.InvalidParam, "request is required");

        var grpcRequest = new McpServerImportGrpcRequest
        {
            Namespace = _namespaceId,
            ImportType = request.ImportType.ToString().ToLowerInvariant(),
            ImportData = request.ImportData,
            OverrideExisting = request.OverrideExisting,
            ValidateOnly = request.ValidateOnly,
            SkipInvalid = request.SkipInvalid,
            SelectedServers = request.SelectedServers?.ToList()
        };

        var response = await _grpcClient.RequestAsync<McpServerImportGrpcResponse>(
            "McpServerImportRequest", grpcRequest, cancellationToken);

        if (response == null)
        {
            return McpServerImportResponse.Error("Failed to get import response from server");
        }

        return new McpServerImportResponse
        {
            Success = response.Success,
            TotalCount = response.TotalCount,
            SuccessCount = response.SuccessCount,
            FailedCount = response.FailedCount,
            SkippedCount = response.SkippedCount,
            Results = response.Results ?? new List<McpServerImportResult>()
        };
    }

    #endregion

    #region MCP Tool Management

    /// <inheritdoc />
    public async Task<McpToolSpec?> RefreshMcpToolAsync(
        string mcpName,
        string toolName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);
        ValidateToolName(toolName);

        var request = new McpToolRefreshRequest
        {
            Namespace = _namespaceId,
            McpName = mcpName,
            ToolName = toolName,
            Version = version
        };

        var response = await _grpcClient.RequestAsync<McpToolQueryResponse>(
            "McpToolRefreshRequest", request, cancellationToken);

        return response?.Tool;
    }

    /// <inheritdoc />
    public async Task<McpToolSpec?> GetMcpToolAsync(
        string mcpName,
        string toolName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);
        ValidateToolName(toolName);

        var request = new McpToolQueryRequest
        {
            Namespace = _namespaceId,
            McpName = mcpName,
            ToolName = toolName,
            Version = version
        };

        var response = await _grpcClient.RequestAsync<McpToolQueryResponse>(
            "McpToolQueryRequest", request, cancellationToken);

        return response?.Tool;
    }

    /// <inheritdoc />
    public async Task DeleteMcpToolAsync(
        string mcpName,
        string toolName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);
        ValidateToolName(toolName);

        var request = new McpToolDeleteRequest
        {
            Namespace = _namespaceId,
            McpName = mcpName,
            ToolName = toolName,
            Version = version
        };

        var response = await _grpcClient.RequestAsync<OperationResponse>(
            "McpToolDeleteRequest", request, cancellationToken);

        if (response?.Success != true)
        {
            throw new NacosException(NacosException.ServerError, response?.Message ?? "Failed to delete MCP tool");
        }

        _logger?.LogInformation("Deleted MCP tool {ToolName} from {McpName}@{Version}", toolName, mcpName, version ?? "latest");
    }

    /// <inheritdoc />
    public async Task UpdateMcpToolAsync(
        string mcpName,
        McpToolSpec toolSpec,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        ValidateMcpName(mcpName);
        if (toolSpec == null)
            throw new NacosException(NacosException.InvalidParam, "toolSpec is required");
        ValidateToolName(toolSpec.Name);

        var request = new McpToolUpdateRequest
        {
            Namespace = _namespaceId,
            McpName = mcpName,
            Tool = toolSpec,
            Version = version
        };

        var response = await _grpcClient.RequestAsync<OperationResponse>(
            "McpToolUpdateRequest", request, cancellationToken);

        if (response?.Success != true)
        {
            throw new NacosException(NacosException.ServerError, response?.Message ?? "Failed to update MCP tool");
        }

        _logger?.LogInformation("Updated MCP tool {ToolName} in {McpName}@{Version}", toolSpec.Name, mcpName, version ?? "latest");
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

        var request = new AgentCardQueryRequest
        {
            Namespace = _namespaceId,
            AgentName = agentName,
            Version = version,
            RegistrationType = registrationType
        };

        var response = await _grpcClient.RequestAsync<AgentCardQueryResponse>(
            "AgentCardQueryRequest", request, cancellationToken);

        return response?.AgentCardDetailInfo;
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
        ValidateAgentCard(agentCard);

        var request = new AgentCardReleaseRequest
        {
            Namespace = _namespaceId,
            AgentName = agentCard.Name!,
            AgentCard = agentCard,
            RegistrationType = registrationType ?? AiConstants.A2a.A2aEndpointTypeService,
            SetAsLatest = setAsLatest
        };

        var response = await _grpcClient.RequestAsync<OperationResponse>(
            "AgentCardReleaseRequest", request, cancellationToken);

        if (response?.Success != true)
        {
            throw new NacosException(NacosException.ServerError, "Failed to release Agent Card");
        }

        _logger?.LogInformation("Released Agent Card {AgentName}@{Version}", agentCard.Name, agentCard.Version);
    }

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

        var request = new AgentEndpointRequest
        {
            Namespace = _namespaceId,
            AgentName = agentName,
            Endpoint = endpoint,
            Type = "register"
        };

        var response = await _grpcClient.RequestAsync<OperationResponse>(
            "AgentEndpointRequest", request, cancellationToken);

        if (response?.Success != true)
        {
            throw new NacosException(NacosException.ServerError, "Failed to register Agent endpoint");
        }

        _logger?.LogInformation("Registered Agent endpoint {Address}:{Port} to {AgentName}", 
            endpoint.Address, endpoint.Port, agentName);
    }

    /// <inheritdoc />
    public async Task RegisterAgentEndpointsAsync(string agentName, IEnumerable<AgentEndpoint> endpoints, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);
        var endpointList = endpoints?.ToList() ?? throw new NacosException(NacosException.InvalidParam, "endpoints is required");

        foreach (var endpoint in endpointList)
        {
            await RegisterAgentEndpointAsync(agentName, endpoint, cancellationToken);
        }
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

        var request = new AgentEndpointRequest
        {
            Namespace = _namespaceId,
            AgentName = agentName,
            Endpoint = endpoint,
            Type = "deregister"
        };

        var response = await _grpcClient.RequestAsync<OperationResponse>(
            "AgentEndpointRequest", request, cancellationToken);

        if (response?.Success != true)
        {
            throw new NacosException(NacosException.ServerError, "Failed to deregister Agent endpoint");
        }

        _logger?.LogInformation("Deregistered Agent endpoint {Address}:{Port} from {AgentName}", 
            endpoint.Address, endpoint.Port, agentName);
    }

    /// <inheritdoc />
    public Task<AgentCardDetailInfo?> SubscribeAgentCardAsync(string agentName, AbstractNacosAgentCardListener listener, CancellationToken cancellationToken = default)
    {
        return SubscribeAgentCardAsync(agentName, null, listener, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AgentCardDetailInfo?> SubscribeAgentCardAsync(string agentName, string? version, AbstractNacosAgentCardListener listener, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);
        if (listener == null) throw new NacosException(NacosException.InvalidParam, "listener is required");

        var key = GetAgentKey(agentName, version);
        
        _agentListeners.AddOrUpdate(key,
            _ => new List<AbstractNacosAgentCardListener> { listener },
            (_, list) => { list.Add(listener); return list; });

        // Send subscribe request
        var request = new AgentCardSubscribeRequest
        {
            Namespace = _namespaceId,
            AgentName = agentName,
            Version = version,
            Subscribe = true
        };

        await _grpcClient.SendStreamRequestAsync("AgentCardSubscribeRequest", request);

        // Get current value
        var current = await GetAgentCardAsync(agentName, version, cancellationToken);
        if (current != null)
        {
            _agentCache[key] = current;
            listener.OnEvent(new NacosAgentCardEvent(current));
        }

        _logger?.LogDebug("Subscribed to Agent Card {AgentName}@{Version}", agentName, version);
        return current;
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
        if (listener == null) return Task.CompletedTask;

        var key = GetAgentKey(agentName, version);

        if (_agentListeners.TryGetValue(key, out var list))
        {
            list.Remove(listener);
            if (list.Count == 0)
            {
                _agentListeners.TryRemove(key, out _);
                _agentCache.TryRemove(key, out _);
            }
        }

        _logger?.LogDebug("Unsubscribed from Agent Card {AgentName}@{Version}", agentName, version);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteAgentAsync(string agentName, string? version = null, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);

        var request = new AgentDeleteRequest
        {
            Namespace = _namespaceId,
            AgentName = agentName,
            Version = version
        };

        var response = await _grpcClient.RequestAsync<OperationResponse>(
            "AgentDeleteRequest", request, cancellationToken);

        if (response?.Success != true)
        {
            throw new NacosException(NacosException.ServerError, response?.Message ?? "Failed to delete Agent");
        }

        // Remove from cache
        var key = GetAgentKey(agentName, version);
        _agentCache.TryRemove(key, out _);

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

        var request = new AgentListRequest
        {
            Namespace = _namespaceId,
            AgentName = agentName,
            Search = search,
            PageNo = pageNo,
            PageSize = pageSize
        };

        var response = await _grpcClient.RequestAsync<AgentListResponse>(
            "AgentListRequest", request, cancellationToken);

        if (response == null)
        {
            return PageResult<AgentCardBasicInfo>.Empty(pageNo, pageSize);
        }

        return PageResult<AgentCardBasicInfo>.FromItems(
            response.AgentCards ?? new List<AgentCardBasicInfo>(),
            response.TotalCount,
            pageNo,
            pageSize);
    }

    /// <inheritdoc />
    public async Task<List<string>> ListAgentVersionsAsync(string agentName, CancellationToken cancellationToken = default)
    {
        ValidateAgentName(agentName);

        var request = new AgentVersionListRequest
        {
            Namespace = _namespaceId,
            AgentName = agentName
        };

        var response = await _grpcClient.RequestAsync<AgentVersionListResponse>(
            "AgentVersionListRequest", request, cancellationToken);

        return response?.Versions ?? new List<string>();
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

        _mcpListeners.Clear();
        _agentListeners.Clear();
        _mcpCache.Clear();
        _agentCache.Clear();
        
        await _grpcClient.DisposeAsync();
        _disposed = true;
    }

    #endregion

    #region Private Methods

    private void HandlePushMessage(string type, string body)
    {
        try
        {
            if (type.Contains("McpServer") || type.Contains("Mcp"))
            {
                HandleMcpServerPush(body);
            }
            else if (type.Contains("AgentCard") || type.Contains("Agent"))
            {
                HandleAgentCardPush(body);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling AI push message of type {Type}", type);
        }
    }

    private void HandleMcpServerPush(string body)
    {
        var notification = JsonSerializer.Deserialize<McpServerNotification>(body, JsonOptions);
        if (notification == null) return;

        var key = GetMcpKey(notification.McpName, notification.Version);

        if (_mcpListeners.TryGetValue(key, out var listeners) && listeners.Count > 0)
        {
            var detailInfo = notification.McpServerDetailInfo;
            if (detailInfo != null)
            {
                _mcpCache[key] = detailInfo;
                var evt = new NacosMcpServerEvent(detailInfo);

                foreach (var listener in listeners.ToList())
                {
                    try
                    {
                        listener.OnEvent(evt);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error notifying MCP listener for {McpName}", notification.McpName);
                    }
                }
            }
        }
    }

    private void HandleAgentCardPush(string body)
    {
        var notification = JsonSerializer.Deserialize<AgentCardNotification>(body, JsonOptions);
        if (notification == null) return;

        var key = GetAgentKey(notification.AgentName, notification.Version);

        if (_agentListeners.TryGetValue(key, out var listeners) && listeners.Count > 0)
        {
            var detailInfo = notification.AgentCardDetailInfo;
            if (detailInfo != null)
            {
                _agentCache[key] = detailInfo;
                var evt = new NacosAgentCardEvent(detailInfo);

                foreach (var listener in listeners.ToList())
                {
                    try
                    {
                        listener.OnEvent(evt);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error notifying Agent listener for {AgentName}", notification.AgentName);
                    }
                }
            }
        }
    }

    private static string GetMcpKey(string mcpName, string? version) => $"{mcpName}@@{version ?? "latest"}";
    private static string GetAgentKey(string agentName, string? version) => $"{agentName}@@{version ?? "latest"}";

    private static void ValidateMcpName(string mcpName)
    {
        if (string.IsNullOrWhiteSpace(mcpName))
            throw new NacosException(NacosException.InvalidParam, "mcpName is required");
    }

    private static void ValidateAgentName(string agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
            throw new NacosException(NacosException.InvalidParam, "agentName is required");
    }

    private static void ValidateToolName(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new NacosException(NacosException.InvalidParam, "toolName is required");
    }

    private static void ValidateEndpoint(string address, int port)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new NacosException(NacosException.InvalidParam, "address is required");
        if (port <= 0 || port > 65535)
            throw new NacosException(NacosException.InvalidParam, "port must be between 1 and 65535");
    }

    private static void ValidateMcpServerSpec(McpServerBasicInfo? spec)
    {
        if (spec == null)
            throw new NacosException(NacosException.InvalidParam, "serverSpecification is required");
        if (string.IsNullOrWhiteSpace(spec.Name))
            throw new NacosException(NacosException.InvalidParam, "serverSpecification.Name is required");
        if (spec.VersionDetail == null || string.IsNullOrWhiteSpace(spec.VersionDetail.Version))
            throw new NacosException(NacosException.InvalidParam, "serverSpecification.VersionDetail.Version is required");
    }

    private static void ValidateAgentCard(AgentCard? agentCard)
    {
        if (agentCard == null)
            throw new NacosException(NacosException.InvalidParam, "agentCard is required");
        if (string.IsNullOrWhiteSpace(agentCard.Name))
            throw new NacosException(NacosException.InvalidParam, "agentCard.Name is required");
        if (string.IsNullOrWhiteSpace(agentCard.Version))
            throw new NacosException(NacosException.InvalidParam, "agentCard.Version is required");
    }

    private static void ValidateAgentEndpoint(AgentEndpoint? endpoint)
    {
        if (endpoint == null)
            throw new NacosException(NacosException.InvalidParam, "endpoint is required");
        if (string.IsNullOrWhiteSpace(endpoint.Version))
            throw new NacosException(NacosException.InvalidParam, "endpoint.Version is required");
        ValidateEndpoint(endpoint.Address ?? "", endpoint.Port);
    }

    #endregion

    #region Request/Response Models

    private class McpServerQueryRequest
    {
        public string? Namespace { get; set; }
        public string McpName { get; set; } = string.Empty;
        public string? Version { get; set; }
    }

    private class McpServerQueryResponse
    {
        public McpServerDetailInfo? McpServerDetailInfo { get; set; }
    }

    private class McpServerReleaseRequest
    {
        public string? Namespace { get; set; }
        public string McpName { get; set; } = string.Empty;
        public McpServerBasicInfo? ServerSpecification { get; set; }
        public McpToolSpecification? ToolSpecification { get; set; }
        public McpEndpointSpec? EndpointSpecification { get; set; }
    }

    private class McpServerReleaseResponse
    {
        public string? McpServerId { get; set; }
    }

    private class McpEndpointRequest
    {
        public string? Namespace { get; set; }
        public string McpName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Port { get; set; }
        public string? Version { get; set; }
        public string Type { get; set; } = "register";
    }

    private class McpServerSubscribeRequest
    {
        public string? Namespace { get; set; }
        public string McpName { get; set; } = string.Empty;
        public string? Version { get; set; }
        public bool Subscribe { get; set; }
    }

    private class McpServerNotification
    {
        public string McpName { get; set; } = string.Empty;
        public string? Version { get; set; }
        public McpServerDetailInfo? McpServerDetailInfo { get; set; }
    }

    private class AgentCardQueryRequest
    {
        public string? Namespace { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string? RegistrationType { get; set; }
    }

    private class AgentCardQueryResponse
    {
        public AgentCardDetailInfo? AgentCardDetailInfo { get; set; }
    }

    private class AgentCardReleaseRequest
    {
        public string? Namespace { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public AgentCard? AgentCard { get; set; }
        public string RegistrationType { get; set; } = AiConstants.A2a.A2aEndpointTypeService;
        public bool SetAsLatest { get; set; }
    }

    private class AgentEndpointRequest
    {
        public string? Namespace { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public AgentEndpoint? Endpoint { get; set; }
        public string Type { get; set; } = "register";
    }

    private class AgentCardSubscribeRequest
    {
        public string? Namespace { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string? Version { get; set; }
        public bool Subscribe { get; set; }
    }

    private class AgentCardNotification
    {
        public string AgentName { get; set; } = string.Empty;
        public string? Version { get; set; }
        public AgentCardDetailInfo? AgentCardDetailInfo { get; set; }
    }

    private class OperationResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    private class McpServerDeleteRequest
    {
        public string? Namespace { get; set; }
        public string McpName { get; set; } = string.Empty;
        public string? Version { get; set; }
    }

    private class McpServerListRequest
    {
        public string? Namespace { get; set; }
        public string? McpName { get; set; }
        public string? Search { get; set; }
        public int PageNo { get; set; }
        public int PageSize { get; set; }
    }

    private class McpServerListResponse
    {
        public List<McpServerBasicInfo>? McpServers { get; set; }
        public int TotalCount { get; set; }
    }

    private class AgentDeleteRequest
    {
        public string? Namespace { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string? Version { get; set; }
    }

    private class AgentListRequest
    {
        public string? Namespace { get; set; }
        public string? AgentName { get; set; }
        public string? Search { get; set; }
        public int PageNo { get; set; }
        public int PageSize { get; set; }
    }

    private class AgentListResponse
    {
        public List<AgentCardBasicInfo>? AgentCards { get; set; }
        public int TotalCount { get; set; }
    }

    private class AgentVersionListRequest
    {
        public string? Namespace { get; set; }
        public string AgentName { get; set; } = string.Empty;
    }

    private class AgentVersionListResponse
    {
        public List<string>? Versions { get; set; }
    }

    // Import/Validation Request/Response Models
    private class McpServerValidateImportRequest
    {
        public string? Namespace { get; set; }
        public string ImportType { get; set; } = string.Empty;
        public string? ImportData { get; set; }
        public bool OverrideExisting { get; set; }
    }

    private class McpServerValidateImportResponse
    {
        public bool IsValid { get; set; }
        public List<string>? Errors { get; set; }
        public List<McpServerValidationItem>? Servers { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public int DuplicateCount { get; set; }
    }

    private class McpServerImportGrpcRequest
    {
        public string? Namespace { get; set; }
        public string ImportType { get; set; } = string.Empty;
        public string? ImportData { get; set; }
        public bool OverrideExisting { get; set; }
        public bool ValidateOnly { get; set; }
        public bool SkipInvalid { get; set; }
        public List<string>? SelectedServers { get; set; }
    }

    private class McpServerImportGrpcResponse
    {
        public bool Success { get; set; }
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<McpServerImportResult>? Results { get; set; }
    }

    // Tool Management Request/Response Models
    private class McpToolRefreshRequest
    {
        public string? Namespace { get; set; }
        public string McpName { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string? Version { get; set; }
    }

    private class McpToolQueryRequest
    {
        public string? Namespace { get; set; }
        public string McpName { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string? Version { get; set; }
    }

    private class McpToolQueryResponse
    {
        public McpToolSpec? Tool { get; set; }
    }

    private class McpToolDeleteRequest
    {
        public string? Namespace { get; set; }
        public string McpName { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string? Version { get; set; }
    }

    private class McpToolUpdateRequest
    {
        public string? Namespace { get; set; }
        public string McpName { get; set; } = string.Empty;
        public McpToolSpec? Tool { get; set; }
        public string? Version { get; set; }
    }

    #endregion
}
