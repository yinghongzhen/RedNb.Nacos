using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using global::Grpc.Core;
using global::Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using RedNb.Nacos;
using RedNb.Nacos.Grpc.Config;
using RedNb.Nacos.Grpc.Naming;
using RedNb.Nacos.Grpc.Protos;
using RedNb.Nacos.Grpc.Serialization;
using ProtoMetadata = RedNb.Nacos.Grpc.Protos.Metadata;

namespace RedNb.Nacos.Grpc;

/// <summary>
/// gRPC client for Nacos server communication.
/// Implements connection management, request/response handling, and server push processing.
/// </summary>
public class NacosGrpcClient : IAsyncDisposable
{
    private readonly NacosClientOptions _options;
    private readonly ILogger? _logger;
    private readonly string _clientId;
    private readonly string _module;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly RedNb.Nacos.Http.Transport.SecurityProxy _securityProxy;

    /// <summary>
    /// The Nacos 3.x unary request method (<c>Request/request</c>, no proto package —
    /// the server registers the bare service name). Every client-to-server request,
    /// ServerCheck included, travels over this method: Nacos 3.2.4's bi-stream
    /// acceptor only accepts ConnectionSetup and client push acknowledgements and
    /// drops anything else ("unknown payload receive", server-side remote log).
    /// Declared by hand because Grpc.Tools generates an uncompilable stub for a
    /// proto method whose name starts with a lowercase letter — see the note in
    /// <c>Protos/nacos_grpc.proto</c>.
    /// </summary>
    private static readonly Method<Payload, Payload> RequestMethod = new(
        MethodType.Unary,
        "Request",
        "request",
        Marshallers.Create<Payload>(payload => payload.ToByteArray(), bytes => Payload.Parser.ParseFrom(bytes)),
        Marshallers.Create<Payload>(payload => payload.ToByteArray(), bytes => Payload.Parser.ParseFrom(bytes)));

    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ConcurrentDictionary<string, Action<string, string>> _pushHandlers = new();

    /// <summary>
    /// The current connected generation. A reconnect replaces it: the previous
    /// generation is cancelled and awaited in <see cref="CleanupConnectionAsync"/>
    /// before a new one is created, and a loop that no longer belongs to the
    /// current generation must not touch the shared connection state — a dying
    /// channel's receive loop would otherwise mark the fresh connection as lost and
    /// cause another reconnect.
    /// </summary>
    private volatile ConnectionGeneration? _current;

    private volatile bool _connected;
    private volatile bool _disposed;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private Task? _reconnectTask;
    private DateTime _lastActiveTime;

    /// <summary>
    /// One connected generation: its own channel, bi-stream, cancellation source and
    /// background loops, so a reconnect can retire all of them together.
    /// </summary>
    private sealed class ConnectionGeneration
    {
        public ConnectionGeneration(string server)
        {
            Server = server;
        }

        /// <summary>The server address this generation was opened against.</summary>
        public string Server { get; }

        /// <summary>Cancels this generation's receive and keep-alive loops.</summary>
        public CancellationTokenSource Cts { get; } = new();

        /// <summary>
        /// Completed when the server answers the ConnectionSetup with a
        /// <c>SetupAckRequest</c>, i.e. when the connection is registered server-side.
        /// </summary>
        public TaskCompletionSource<bool> SetupAck { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>The generation's channel, disposed in cleanup.</summary>
        public GrpcChannel? Channel { get; set; }

        /// <summary>The generation's bi-stream client and call.</summary>
        public BiRequestStream.BiRequestStreamClient? StreamClient { get; set; }

        public AsyncDuplexStreamingCall<Payload, Payload>? Stream { get; set; }

        /// <summary>The connection ID assigned by the server.</summary>
        public string? ConnectionId { get; set; }

        public Task? KeepAliveTask { get; set; }

        public Task? ReceiveTask { get; set; }
    }

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

    /// <summary>
    /// Creates a gRPC client.
    /// </summary>
    /// <param name="options">The client options.</param>
    /// <param name="logger">The optional logger.</param>
    /// <param name="module">
    /// The module announced to the server in the ConnectionSetup labels. Nacos 3.2.4
    /// only tracks a connection in the naming <c>ConnectionBasedClientManager</c> when
    /// the label is <c>naming</c> — an instance registration over a connection labelled
    /// <c>config</c> is rejected with "Client [id] connection already disconnect".
    /// Config traffic keeps the <c>config</c> label, like the Java SDK.
    /// </param>
    public NacosGrpcClient(NacosClientOptions options, ILogger? logger = null, string module = "config")
    {
        _options = options;
        _logger = logger;
        _module = module;
        _clientId = Guid.NewGuid().ToString("N");
        _lastActiveTime = DateTime.UtcNow;

        _jsonOptions = NacosGrpcJsonOptions.Create(options.JsonTypeInfoResolver);

        _securityProxy = new RedNb.Nacos.Http.Transport.SecurityProxy(options, logger);
    }

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    public bool IsConnected => _connected;

    /// <summary>
    /// Gets the connection ID assigned by server.
    /// </summary>
    public string? ConnectionId => _current?.ConnectionId;

    /// <summary>
    /// Gets the client ID.
    /// </summary>
    public string ClientId => _clientId;

    /// <summary>
    /// Connects to the Nacos server.
    /// </summary>
    public virtual async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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
        // Retire the previous generation before opening a new one: without this a
        // reconnect leaks the old channel/stream (sockets and HTTP/2 threads) and
        // leaves its receive loop running, which would report the new connection as
        // lost as soon as the dead channel faults.
        await CleanupConnectionAsync();

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

                var generation = new ConnectionGeneration(server);

                // Create channel with options
                generation.Channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
                {
                    HttpHandler = RedNb.Nacos.Utils.NacosTransport.CreateHandler(_options, grpc: true),
                    DisposeHttpClient = true
                });

                generation.StreamClient = new BiRequestStream.BiRequestStreamClient(generation.Channel);

                // Publish the generation before the first request so the unary path
                // resolves it.
                _current = generation;

                // Server check to get connection ID
                await ServerCheckAsync(generation, cancellationToken);

                // Start bi-directional stream
                await StartBiStreamAsync(generation, cancellationToken);

                // Start keep alive task
                generation.KeepAliveTask = KeepAliveLoopAsync(generation, generation.Cts.Token);

                _connected = true;
                _reconnectTask ??= Task.Run(() => ReconnectLoopAsync(_lifetimeCts.Token));
                _lastActiveTime = DateTime.UtcNow;

                _logger?.LogInformation("Connected to Nacos gRPC server at {Address}, ConnectionId: {ConnectionId}",
                    address, generation.ConnectionId);
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
    public virtual async Task<TResponse?> RequestAsync<TResponse>(string type, object request,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        await EnsureConnectedAsync(cancellationToken);

        return await SendRequestWithResponseAsync<TResponse>(
            type, request, TimeSpan.FromMilliseconds(_options.DefaultTimeout), cancellationToken);
    }

    /// <summary>
    /// Sends a request and does not deserialize the response. Despite the historical
    /// name, Nacos 3.x serves every client request over the unary <c>Request/request</c>
    /// method — the bi-stream carries ConnectionSetup and server push only.
    /// </summary>
    public virtual async Task SendRequestAsync(string type, object request,
        CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        await SendUnaryRequestAsync(type, request,
            TimeSpan.FromMilliseconds(_options.DefaultTimeout), cancellationToken);

        _logger?.LogDebug("Sent request of type {Type}", type);
    }

    /// <summary>
    /// Sends a request over the unary <c>Request/request</c> method and deserializes
    /// the response. The historical name is kept because the transport clients use it
    /// for the requests whose responses the caller inspects.
    /// </summary>
    public virtual async Task<TResponse?> SendRequestWithResponseAsync<TResponse>(string type, object request,
        TimeSpan timeout, CancellationToken cancellationToken = default) where TResponse : class
    {
        await EnsureConnectedAsync(cancellationToken);

        var responseJson = await SendUnaryRequestAsync(type, request, timeout, cancellationToken);
        return string.IsNullOrEmpty(responseJson)
            ? null
            : JsonSerializer.Deserialize(responseJson,
                (JsonTypeInfo<TResponse>)_jsonOptions.GetTypeInfo(typeof(TResponse))!);
    }

    /// <summary>
    /// Sends a payload over the unary <c>Request/request</c> method and returns the
    /// raw JSON of the response body.
    /// </summary>
    private async Task<string?> SendUnaryRequestAsync(string type, object request, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        // Set the request ID if the request carries one; Nacos echoes it in the response.
        switch (request)
        {
            case ConfigRpcRequest configRpcRequest:
                configRpcRequest.RequestId = Guid.NewGuid().ToString("N");
                break;
            case NamingRpcRequest namingRpcRequest:
                namingRpcRequest.RequestId = Guid.NewGuid().ToString("N");
                break;
        }

        var generation = _current ?? throw new NacosException(
            NacosException.ClientDisconnect, "Not connected to Nacos server");

        var payload = await CreatePayloadAsync(generation, type, request, cancellationToken);
        var deadline = DateTime.UtcNow.Add(timeout);

        try
        {
            // Dispose the call so the HTTP/2 response/stream objects are released
            // per request instead of waiting for a GC (same as the generated stubs).
            using var call = generation.Channel!.CreateCallInvoker().AsyncUnaryCall(RequestMethod, null,
                new CallOptions(deadline: deadline, cancellationToken: cancellationToken), payload);

            var response = await call.ResponseAsync;
            if (response.Metadata?.Type == "ErrorResponse")
            {
                using var error = JsonDocument.Parse(response.Body.Value.ToStringUtf8());
                var code = error.RootElement.TryGetProperty("errorCode", out var value) ? value.GetInt32() : NacosException.ServerError;
                if (code == 301 && IsCurrent(generation)) _connected = false;
                var message = error.RootElement.TryGetProperty("message", out var text) ? text.GetString() : "Nacos request rejected";
                throw new NacosException(code, message ?? "Nacos request rejected");
            }

            if (IsCurrent(generation))
            {
                _lastActiveTime = DateTime.UtcNow;
            }

            return response.Body?.Value.ToStringUtf8();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            if (IsCurrent(generation))
            {
                _connected = false;
            }

            _logger?.LogWarning(ex, "gRPC connection lost, will reconnect");
            throw new NacosException(NacosException.ServerError, "Connection lost", ex);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            // A half-open connection typically shows up as a deadline rather than an
            // Unavailable status. Mark it lost so the next request reconnects instead
            // of paying the full timeout again; the exception type is left as-is
            // because the config long-poll listen call uses the same deadline.
            if (IsCurrent(generation))
            {
                _connected = false;
            }

            _logger?.LogWarning(ex, "gRPC call timed out, will reconnect");
            throw;
        }
    }

    /// <summary>
    /// Whether the generation is still the connected one. Only the current
    /// generation may mutate the shared connection state.
    /// </summary>
    private bool IsCurrent(ConnectionGeneration generation)
    {
        return ReferenceEquals(_current, generation);
    }

    /// <summary>
    /// Registers a handler for push messages.
    /// </summary>
    public virtual void RegisterPushHandler(string handlerId, Action<string, string> handler)
    {
        _pushHandlers.TryAdd(handlerId, handler);
    }

    /// <summary>
    /// Unregisters a push handler.
    /// </summary>
    public virtual void UnregisterPushHandler(string handlerId)
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

    private async Task ServerCheckAsync(ConnectionGeneration generation, CancellationToken cancellationToken)
    {
        var request = new ServerCheckRequest();
        var payload = await CreatePayloadAsync(generation, ServerCheckRequest.TYPE, request, cancellationToken);

        using var call = generation.Channel!.CreateCallInvoker().AsyncUnaryCall(
            RequestMethod,
            null,
            new CallOptions(deadline: DateTime.UtcNow.AddMilliseconds(ConnectionTimeoutMs), cancellationToken: cancellationToken),
            payload);

        var response = await call.ResponseAsync;

        if (response.Body != null && !response.Body.Value.IsEmpty)
        {
            var json = response.Body.Value.ToStringUtf8();
            var checkResponse = JsonSerializer.Deserialize(json, NacosGrpcJsonContext.Default.ServerCheckResponse);
            generation.ConnectionId = checkResponse?.ConnectionId;
        }
    }

    private async Task StartBiStreamAsync(ConnectionGeneration generation, CancellationToken cancellationToken)
    {
        generation.Stream = generation.StreamClient!.requestBiStream(cancellationToken: generation.Cts.Token);

        // Receive before writing the setup so the server's SetupAckRequest cannot
        // be missed.
        generation.ReceiveTask = ReceiveStreamMessagesAsync(generation);

        // Send initial setup through stream. An explicit ability table makes the
        // server answer with SetupAckRequest once the connection is registered;
        // without it the server registers silently and a request racing the setup
        // is rejected ("Invalid connection Id ... is unregistered").
        var setupRequest = new ConnectionSetupRequest
        {
            ClientVersion = NacosConstants.ClientVersion,
            Tenant = _options.Namespace,
            Labels = new Dictionary<string, string>
            {
                { "source", "sdk" },
                { "module", _module }
            },
            AbilityTable = new Dictionary<string, bool>()
        };

        var payload = await CreatePayloadAsync(generation, ConnectionSetupRequest.TYPE, setupRequest, cancellationToken);
        await generation.Stream.RequestStream.WriteAsync(payload, cancellationToken);

        // Wait for the registration acknowledgement before reporting the client as
        // connected: requests sent earlier are dropped by the server as coming
        // from an unknown connection.
        var completed = await Task.WhenAny(generation.SetupAck.Task, Task.Delay(ConnectionTimeoutMs, cancellationToken));
        if (completed != generation.SetupAck.Task)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NacosException(NacosException.ClientDisconnect,
                "Nacos did not acknowledge the gRPC connection setup");
        }
    }

    private async Task ReceiveStreamMessagesAsync(ConnectionGeneration generation)
    {
        var cancellationToken = generation.Cts.Token;

        try
        {
            await foreach (var payload in generation.Stream!.ResponseStream.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var type = payload.Metadata?.Type ?? "Unknown";
                    var body = payload.Body?.Value.ToStringUtf8() ?? "{}";

                    if (IsCurrent(generation))
                    {
                        _lastActiveTime = DateTime.UtcNow;
                    }

                    _logger?.LogDebug("Received push message of type {Type}", type);

                    // Everything the server writes to the bi-stream is a push:
                    // request responses travel over the unary Request/request call.
                    await HandleServerPushAsync(generation, type, body, cancellationToken);
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

            // Only the current generation may report itself as lost: an orphaned
            // loop from a retired connection must not flag the fresh one.
            if (IsCurrent(generation))
            {
                _connected = false;
            }
        }
        finally
        {
            if (IsCurrent(generation)) _connected = false;
        }
    }

    private async Task HandleServerPushAsync(ConnectionGeneration generation, string type, string body,
        CancellationToken cancellationToken)
    {
        // The connection setup acknowledgement is the readiness signal awaited by
        // StartBiStreamAsync. It is sent without expecting an ack (sendRequestNoAck).
        if (type == SetupAckRequestType)
        {
            _logger?.LogDebug("Server acknowledged the gRPC connection setup");
            generation.SetupAck.TrySetResult(true);
            return;
        }

        // Handle client detection (health check from server)
        if (type == ClientDetectionRequest.TYPE)
        {
            var detectionResponse = new ClientDetectionResponse { Success = true };

            if (TryGetRequestId(body, out var detectionRequestId))
            {
                detectionResponse.RequestId = detectionRequestId;
            }

            await SendPushAckAsync(generation, ClientDetectionResponse.TYPE, detectionResponse, cancellationToken);
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
            await SendPushAckAsync(generation, responseType, BuildPushAck(body), cancellationToken);
        }
    }

    /// <summary>
    /// The payload type of the push Nacos answers a ConnectionSetup with when the
    /// setup carries an ability table (server: <c>SetupAckRequest</c>, sent through
    /// <c>sendRequestNoAck</c>).
    /// </summary>
    private const string SetupAckRequestType = "SetupAckRequest";

    /// <summary>
    /// Builds a push acknowledgement echoing the push's <c>requestId</c>: the server
    /// correlates an ack with its request through that id and logs "Ack receive on a
    /// outdated request" for an ack whose id is missing.
    /// </summary>
    private static PushAckResponse BuildPushAck(string body)
    {
        return new PushAckResponse
        {
            RequestId = TryGetRequestId(body, out var requestId) ? requestId : null
        };
    }

    private static bool TryGetRequestId(string body, out string? requestId)
    {
        requestId = null;

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("requestId", out var element) &&
                element.ValueKind == JsonValueKind.String)
            {
                requestId = element.GetString();
                return requestId != null;
            }
        }
        catch (JsonException)
        {
            // Not JSON: send the ack without an id.
        }

        return false;
    }

    private async Task SendPushAckAsync(ConnectionGeneration generation, string type, object response,
        CancellationToken cancellationToken)
    {
        try
        {
            var ackPayload = await CreatePayloadAsync(generation, type, response, cancellationToken);
            await generation.Stream!.RequestStream.WriteAsync(ackPayload, cancellationToken);
            _logger?.LogDebug("Sent ack for {Type}", type);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send ack for {Type}", type);
        }
    }

    private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(ReconnectDelayMs, cancellationToken);
                if (!_connected) await ConnectAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger?.LogWarning(ex, "Reconnection attempt failed"); }
        }
    }

    private async Task KeepAliveLoopAsync(ConnectionGeneration generation, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(KeepAliveIntervalMs, cancellationToken);

                // A retired generation must not health-check (or reconnect) anything:
                // its loops are cancelled during cleanup anyway, this only closes the
                // window between the cancellation and the loop observing it.
                if (!IsCurrent(generation) || !_connected) continue;

                // Check if we need to send health check
                if ((DateTime.UtcNow - _lastActiveTime).TotalMilliseconds > KeepAliveIntervalMs)
                {
                    await SendHealthCheckAsync(generation, cancellationToken);
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

    private async Task SendHealthCheckAsync(ConnectionGeneration generation, CancellationToken cancellationToken)
    {
        try
        {
            var request = new HealthCheckRequest();
            var response = await RequestAsync<HealthCheckResponse>(HealthCheckRequest.TYPE, request, cancellationToken);

            if (response?.IsSuccess == true && IsCurrent(generation))
            {
                _lastActiveTime = DateTime.UtcNow;
            }
            else if (IsCurrent(generation))
            {
                _connected = false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Health check failed");

            if (IsCurrent(generation))
            {
                _connected = false;
            }
        }
    }

    private async Task<Payload> CreatePayloadAsync(ConnectionGeneration generation, string type, object request, CancellationToken cancellationToken = default)
    {
        // GetTypeInfo throws NotSupportedException for unregistered types, keeping the
        // 2.0.0 exception contract; rethrow with an SDK-oriented message.
        JsonTypeInfo typeInfo;
        try
        {
            typeInfo = _jsonOptions.GetTypeInfo(request.GetType());
        }
        catch (NotSupportedException ex)
        {
            throw new NotSupportedException($"Cannot serialize request type '{request.GetType().FullName}'. Register its source-generated context with NacosClientOptions.JsonTypeInfoResolver. {ex.Message}", ex);
        }
        var json = JsonSerializer.Serialize(request, typeInfo);
        var body = ByteString.CopyFromUtf8(json);

        var payload = new Payload
        {
            Metadata = new ProtoMetadata
            {
                Type = type,
                ClientIp = GetLocalIp(),
                Headers =
                {
                    { "connectionId", generation.ConnectionId ?? _clientId },
                    { "clientId", _clientId }
                }
            },
            Body = new Any
            {
                Value = body
            }
        };

        // Nacos 3.x gRPC auth travels INSIDE the payload header map (not gRPC
        // call metadata): NacosAuthPluginService resolves identity from the
        // Authorization/accessToken headers, and the official Java client sends
        // the raw JWT under "accessToken" on every payload.
        var token = await _securityProxy.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            payload.Metadata.Headers["accessToken"] = token;
        }

        return payload;
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

    /// <summary>
    /// Retires the current generation: unpublishes it (so its loops can no longer
    /// mutate the shared state), cancels and awaits its loops, then disposes its
    /// stream and channel. Safe to call when nothing is connected.
    /// </summary>
    private async Task CleanupConnectionAsync()
    {
        var generation = _current;
        _current = null;
        _connected = false;

        if (generation == null)
        {
            return;
        }

        try
        {
            await generation.Cts.CancelAsync();
        }
        catch { /* Ignore */ }

        // Wait for the loops to observe the cancellation before touching their
        // resources: a receive loop still reading from a disposed stream would only
        // log errors, but its keep-alive sibling could still issue health checks.
        var loopsCompleted = true;

        foreach (var task in new[] { generation.KeepAliveTask, generation.ReceiveTask })
        {
            if (task == null)
            {
                continue;
            }

            try
            {
                await task.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch
            {
                loopsCompleted = false;
            }
        }

        try
        {
            if (generation.Stream != null)
            {
                await generation.Stream.RequestStream.CompleteAsync();
                generation.Stream.Dispose();
                generation.Stream = null;
            }
        }
        catch { /* Ignore */ }

        try
        {
            generation.Channel?.Dispose();
            generation.Channel = null;
        }
        catch { /* Ignore */ }

        generation.StreamClient = null;

        // Disposing a source whose loops never finished would make their next
        // cancellation-token usage throw ObjectDisposedException, so leave it to the GC.
        if (loopsCompleted)
        {
            generation.Cts.Dispose();
        }
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
        await _lifetimeCts.CancelAsync();
        if (_reconnectTask != null)
            try { await _reconnectTask; } catch (OperationCanceledException) { }
        _lifetimeCts.Dispose();

        // Cancels and awaits the current generation's loops, then disposes its
        // stream and channel.
        await CleanupConnectionAsync();

        _securityProxy.Dispose();
        _connectionLock.Dispose();
    }
}
