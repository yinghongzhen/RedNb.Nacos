using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.GrpcClient.Config;
using RedNb.Nacos.GrpcClient.Protos;
using ProtoMetadata = RedNb.Nacos.GrpcClient.Protos.Metadata;

namespace RedNb.Nacos.GrpcClient;

/// <summary>
/// gRPC client for Nacos server communication.
/// Implements connection management, request/response handling, and server push processing.
/// </summary>
public class NacosGrpcClient : IAsyncDisposable
{
    private readonly NacosClientOptions _options;
    private readonly ILogger? _logger;
    private readonly string _clientId;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private GrpcChannel? _channel;
    private RequestService.RequestServiceClient? _requestClient;
    private BiRequestStream.BiRequestStreamClient? _biStreamClient;
    private AsyncDuplexStreamingCall<Payload, Payload>? _biStream;
    
    private readonly CancellationTokenSource _cts;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ConcurrentDictionary<string, Action<string, string>> _pushHandlers = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();
    
    private volatile bool _connected;
    private volatile bool _disposed;
    private string? _currentServer;
    private string? _connectionId;
    private DateTime _lastActiveTime;
    private Task? _keepAliveTask;
    private Task? _receiveTask;

    /// <summary>
    /// Keep alive interval in milliseconds.
    /// </summary>
    private const int KeepAliveIntervalMs = 5000;

    /// <summary>
    /// Connection timeout in milliseconds.
    /// </summary>
    private const int ConnectionTimeoutMs = 3000;

    /// <summary>
    /// Reconnection delay in milliseconds.
    /// </summary>
    private const int ReconnectDelayMs = 3000;

    public NacosGrpcClient(NacosClientOptions options, ILogger? logger = null)
    {
        _options = options;
        _logger = logger;
        _clientId = Guid.NewGuid().ToString("N");
        _cts = new CancellationTokenSource();
        _lastActiveTime = DateTime.UtcNow;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    public bool IsConnected => _connected;

    /// <summary>
    /// Gets the connection ID assigned by server.
    /// </summary>
    public string? ConnectionId => _connectionId;

    /// <summary>
    /// Gets the client ID.
    /// </summary>
    public string ClientId => _clientId;

    /// <summary>
    /// Connects to the Nacos server.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_connected) return;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connected) return;
            await ConnectInternalAsync(cancellationToken);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task ConnectInternalAsync(CancellationToken cancellationToken)
    {
        var servers = _options.GetServerAddressList();
        Exception? lastException = null;

        foreach (var server in servers)
        {
            try
            {
                var grpcAddress = _options.GetGrpcAddress(server);
                var scheme = _options.EnableTls ? "https" : "http";
                var address = $"{scheme}://{grpcAddress}";

                _logger?.LogInformation("Connecting to Nacos gRPC server at {Address}", address);

                // Create channel with options
                _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
                {
                    HttpHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true,
                        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                        KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                        ConnectTimeout = TimeSpan.FromMilliseconds(ConnectionTimeoutMs)
                    }
                });

                _requestClient = new RequestService.RequestServiceClient(_channel);
                _biStreamClient = new BiRequestStream.BiRequestStreamClient(_channel);

                // Server check to get connection ID
                await ServerCheckAsync(cancellationToken);

                // Send connection setup request
                await SendConnectionSetupAsync(cancellationToken);

                // Start bi-directional stream
                await StartBiStreamAsync(cancellationToken);

                // Start keep alive task
                _keepAliveTask = KeepAliveLoopAsync(_cts.Token);

                _currentServer = server;
                _connected = true;
                _lastActiveTime = DateTime.UtcNow;
                
                _logger?.LogInformation("Connected to Nacos gRPC server at {Address}, ConnectionId: {ConnectionId}", 
                    address, _connectionId);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger?.LogWarning(ex, "Failed to connect to Nacos gRPC server at {Server}", server);
                await CleanupConnectionAsync();
            }
        }

        throw new NacosException(NacosException.ServerError, 
            $"Failed to connect to any Nacos gRPC server: {lastException?.Message}", lastException!);
    }

    /// <summary>
    /// Sends a request and waits for response.
    /// </summary>
    public async Task<TResponse?> RequestAsync<TResponse>(string type, object request, 
        CancellationToken cancellationToken = default) where TResponse : class
    {
        await EnsureConnectedAsync(cancellationToken);

        var payload = CreatePayload(type, request);
        
        try
        {
            var response = await _requestClient!.SendRequestAsync(payload, 
                deadline: DateTime.UtcNow.AddMilliseconds(_options.DefaultTimeout),
                cancellationToken: cancellationToken);

            _lastActiveTime = DateTime.UtcNow;

            if (response.Body == null || response.Body.Value.IsEmpty)
            {
                return null;
            }

            var json = response.Body.Value.ToStringUtf8();
            _logger?.LogDebug("Received response for {Type}: {Response}", type, json);
            
            return JsonSerializer.Deserialize<TResponse>(json, _jsonOptions);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _connected = false;
            _logger?.LogWarning(ex, "gRPC connection lost, will reconnect");
            throw new NacosException(NacosException.ServerError, "Connection lost", ex);
        }
    }

    /// <summary>
    /// Sends a request through the bi-directional stream.
    /// </summary>
    public async Task SendStreamRequestAsync(string type, object request, 
        CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var payload = CreatePayload(type, request);
        
        try
        {
            await _biStream!.RequestStream.WriteAsync(payload, cancellationToken);
            _lastActiveTime = DateTime.UtcNow;
            _logger?.LogDebug("Sent stream request of type {Type}", type);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send stream request of type {Type}", type);
            throw;
        }
    }

    /// <summary>
    /// Sends a request through stream and waits for response.
    /// </summary>
    public async Task<TResponse?> SendStreamRequestWithResponseAsync<TResponse>(string type, object request,
        TimeSpan timeout, CancellationToken cancellationToken = default) where TResponse : class
    {
        await EnsureConnectedAsync(cancellationToken);

        var requestId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<string>();
        
        _pendingRequests.TryAdd(requestId, tcs);
        
        try
        {
            // Set request ID if the request has it
            if (request is ConfigRpcRequest configRpcRequest)
            {
                configRpcRequest.RequestId = requestId;
            }

            var payload = CreatePayload(type, request);
            await _biStream!.RequestStream.WriteAsync(payload, cancellationToken);
            _lastActiveTime = DateTime.UtcNow;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var responseJson = await tcs.Task.WaitAsync(cts.Token);
            return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Request {RequestId} of type {Type} timed out", requestId, type);
            throw;
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    /// <summary>
    /// Registers a handler for push messages.
    /// </summary>
    public void RegisterPushHandler(string handlerId, Action<string, string> handler)
    {
        _pushHandlers.TryAdd(handlerId, handler);
    }

    /// <summary>
    /// Unregisters a push handler.
    /// </summary>
    public void UnregisterPushHandler(string handlerId)
    {
        _pushHandlers.TryRemove(handlerId, out _);
    }

    /// <summary>
    /// Registers a handler for push messages (legacy method).
    /// </summary>
    public void RegisterPushHandler(Action<string, string> handler)
    {
        RegisterPushHandler(handler.GetHashCode().ToString(), handler);
    }

    private async Task ServerCheckAsync(CancellationToken cancellationToken)
    {
        var request = new ServerCheckRequest();
        var payload = CreatePayload(ServerCheckRequest.TYPE, request);
        
        var response = await _requestClient!.SendRequestAsync(payload, 
            deadline: DateTime.UtcNow.AddMilliseconds(ConnectionTimeoutMs),
            cancellationToken: cancellationToken);

        if (response.Body != null && !response.Body.Value.IsEmpty)
        {
            var json = response.Body.Value.ToStringUtf8();
            var checkResponse = JsonSerializer.Deserialize<ServerCheckResponse>(json, _jsonOptions);
            _connectionId = checkResponse?.ConnectionId;
        }
    }

    private async Task SendConnectionSetupAsync(CancellationToken cancellationToken)
    {
        var setupRequest = new ConnectionSetupRequest
        {
            ClientVersion = "RedNb.Nacos/2.0.0",
            Tenant = _options.Namespace,
            Labels = new Dictionary<string, string>
            {
                { "source", "sdk" },
                { "AppName", _options.AppName ?? "unknown" },
                { "module", "config" }
            },
            Abilities = new ClientAbilities
            {
                RemoteAbility = new RemoteAbility { SupportRemoteConnection = true },
                ConfigAbility = new ConfigAbility { SupportRemoteMetrics = false },
                NamingAbility = new NamingAbility { SupportDeltaPush = false, SupportRemoteMetric = false }
            }
        };

        var payload = CreatePayload(ConnectionSetupRequest.TYPE, setupRequest);
        await _requestClient!.SendRequestAsync(payload, cancellationToken: cancellationToken);
    }

    private async Task StartBiStreamAsync(CancellationToken cancellationToken)
    {
        _biStream = _biStreamClient!.RequestBiStream(cancellationToken: _cts.Token);

        // Send initial setup through stream
        var setupRequest = new ConnectionSetupRequest
        {
            ClientVersion = "RedNb.Nacos/2.0.0",
            Tenant = _options.Namespace,
            Labels = new Dictionary<string, string>
            {
                { "source", "sdk" },
                { "module", "config" }
            }
        };
        
        var payload = CreatePayload(ConnectionSetupRequest.TYPE, setupRequest);
        await _biStream.RequestStream.WriteAsync(payload, cancellationToken);

        // Start receiving messages
        _receiveTask = ReceiveStreamMessagesAsync(_cts.Token);
    }

    private async Task ReceiveStreamMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var payload in _biStream!.ResponseStream.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var type = payload.Metadata?.Type ?? "Unknown";
                    var body = payload.Body?.Value.ToStringUtf8() ?? "{}";

                    _lastActiveTime = DateTime.UtcNow;
                    _logger?.LogDebug("Received push message of type {Type}", type);

                    // Check if this is a response to a pending request
                    if (TryHandlePendingResponse(type, body))
                    {
                        continue;
                    }

                    // Handle server push
                    await HandleServerPushAsync(type, body, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing push message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Stream receive cancelled");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            _logger?.LogDebug("Stream cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in bi-stream receive loop");
            _connected = false;
        }
    }

    private bool TryHandlePendingResponse(string type, string body)
    {
        // Try to extract requestId from response
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("requestId", out var requestIdElement))
            {
                var requestId = requestIdElement.GetString();
                if (!string.IsNullOrEmpty(requestId) && _pendingRequests.TryRemove(requestId, out var tcs))
                {
                    tcs.TrySetResult(body);
                    return true;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        
        return false;
    }

    private async Task HandleServerPushAsync(string type, string body, CancellationToken cancellationToken)
    {
        // Handle client detection (health check from server)
        if (type == ClientDetectionRequest.TYPE)
        {
            await SendPushAckAsync(ClientDetectionResponse.TYPE, new ClientDetectionResponse { Success = true }, cancellationToken);
            return;
        }

        // Notify all registered handlers
        foreach (var handler in _pushHandlers.Values)
        {
            try
            {
                handler(type, body);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in push handler for type {Type}", type);
            }
        }

        // Send ack for requests
        if (type.EndsWith("Request"))
        {
            var responseType = type.Replace("Request", "Response");
            await SendPushAckAsync(responseType, new { success = true }, cancellationToken);
        }
    }

    private async Task SendPushAckAsync(string type, object response, CancellationToken cancellationToken)
    {
        try
        {
            var ackPayload = CreatePayload(type, response);
            await _biStream!.RequestStream.WriteAsync(ackPayload, cancellationToken);
            _logger?.LogDebug("Sent ack for {Type}", type);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send ack for {Type}", type);
        }
    }

    private async Task KeepAliveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(KeepAliveIntervalMs, cancellationToken);

                if (!_connected) continue;

                // Check if we need to send health check
                if ((DateTime.UtcNow - _lastActiveTime).TotalMilliseconds > KeepAliveIntervalMs)
                {
                    await SendHealthCheckAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error in keep alive loop");
            }
        }
    }

    private async Task SendHealthCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = new HealthCheckRequest();
            var response = await RequestAsync<HealthCheckResponse>(HealthCheckRequest.TYPE, request, cancellationToken);
            
            if (response?.IsSuccess == true)
            {
                _lastActiveTime = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Health check failed");
            _connected = false;
        }
    }

    private Payload CreatePayload(string type, object request)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var body = ByteString.CopyFromUtf8(json);

        return new Payload
        {
            Metadata = new ProtoMetadata
            {
                Type = type,
                ClientIp = GetLocalIp(),
                Headers = 
                { 
                    { "connectionId", _connectionId ?? _clientId },
                    { "clientId", _clientId }
                }
            },
            Body = new Body
            {
                Value = body
            }
        };
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (!_connected)
        {
            await ConnectAsync(cancellationToken);
        }
        
        if (!_connected)
        {
            throw new NacosException(NacosException.ClientDisconnect, "Not connected to Nacos server");
        }
    }

    private async Task CleanupConnectionAsync()
    {
        _connected = false;
        
        try
        {
            if (_biStream != null)
            {
                await _biStream.RequestStream.CompleteAsync();
                _biStream.Dispose();
                _biStream = null;
            }
        }
        catch { /* Ignore */ }

        try
        {
            _channel?.Dispose();
            _channel = null;
        }
        catch { /* Ignore */ }

        _requestClient = null;
        _biStreamClient = null;
        _connectionId = null;
    }

    private static string GetLocalIp()
    {
        try
        {
            var hostName = System.Net.Dns.GetHostName();
            var addresses = System.Net.Dns.GetHostAddresses(hostName);
            var ipv4 = addresses.FirstOrDefault(a => 
                a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                !System.Net.IPAddress.IsLoopback(a));
            return ipv4?.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _cts.CancelAsync();
        
        // Wait for background tasks
        if (_keepAliveTask != null)
        {
            try { await _keepAliveTask.WaitAsync(TimeSpan.FromSeconds(2)); } catch { /* Ignore */ }
        }
        if (_receiveTask != null)
        {
            try { await _receiveTask.WaitAsync(TimeSpan.FromSeconds(2)); } catch { /* Ignore */ }
        }

        await CleanupConnectionAsync();
        
        _cts.Dispose();
        _connectionLock.Dispose();
    }
}
