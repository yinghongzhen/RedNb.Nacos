using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.GrpcClient.Protos;
using ProtoMetadata = RedNb.Nacos.GrpcClient.Protos.Metadata;

namespace RedNb.Nacos.GrpcClient;

/// <summary>
/// gRPC client for Nacos server communication.
/// </summary>
public class NacosGrpcClient : IAsyncDisposable
{
    private readonly NacosClientOptions _options;
    private readonly ILogger? _logger;
    private readonly string _clientId;
    private GrpcChannel? _channel;
    private RequestService.RequestServiceClient? _requestClient;
    private BiRequestStream.BiRequestStreamClient? _biStreamClient;
    private AsyncDuplexStreamingCall<Payload, Payload>? _biStream;
    private readonly CancellationTokenSource _cts;
    private bool _connected;
    private bool _disposed;
    private string? _currentServer;
    private readonly List<Action<string, string>> _pushHandlers = new();

    public NacosGrpcClient(NacosClientOptions options, ILogger? logger = null)
    {
        _options = options;
        _logger = logger;
        _clientId = Guid.NewGuid().ToString("N");
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    public bool IsConnected => _connected;

    /// <summary>
    /// Connects to the Nacos server.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_connected) return;

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

                _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
                {
                    HttpHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true,
                        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                        KeepAlivePingTimeout = TimeSpan.FromSeconds(30)
                    }
                });

                _requestClient = new RequestService.RequestServiceClient(_channel);
                _biStreamClient = new BiRequestStream.BiRequestStreamClient(_channel);

                // Send connection setup request
                await SendConnectionSetupAsync(cancellationToken);

                // Start bi-directional stream
                await StartBiStreamAsync(cancellationToken);

                _currentServer = server;
                _connected = true;
                _logger?.LogInformation("Connected to Nacos gRPC server at {Address}", address);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger?.LogWarning(ex, "Failed to connect to Nacos gRPC server at {Server}", server);
            }
        }

        throw new NacosException(NacosException.ServerError, 
            $"Failed to connect to any Nacos gRPC server: {lastException?.Message}", lastException!);
    }

    /// <summary>
    /// Sends a request and waits for response.
    /// </summary>
    public async Task<T?> RequestAsync<T>(string type, object request, 
        CancellationToken cancellationToken = default) where T : class
    {
        EnsureConnected();

        var payload = CreatePayload(type, request);
        var response = await _requestClient!.SendRequestAsync(payload, cancellationToken: cancellationToken);

        if (response.Body == null || response.Body.Value.IsEmpty)
        {
            return null;
        }

        var json = response.Body.Value.ToStringUtf8();
        return JsonSerializer.Deserialize<T>(json);
    }

    /// <summary>
    /// Sends a request through the bi-directional stream.
    /// </summary>
    public async Task SendStreamRequestAsync(string type, object request, 
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        var payload = CreatePayload(type, request);
        await _biStream!.RequestStream.WriteAsync(payload, cancellationToken);
    }

    /// <summary>
    /// Registers a handler for push messages.
    /// </summary>
    public void RegisterPushHandler(Action<string, string> handler)
    {
        _pushHandlers.Add(handler);
    }

    private async Task SendConnectionSetupAsync(CancellationToken cancellationToken)
    {
        var setupRequest = new ConnectionSetupRequest
        {
            ClientVersion = "RedNb.Nacos/1.0.0",
            Tenant = _options.Namespace,
            Labels = new Dictionary<string, string>
            {
                { "source", "sdk" },
                { "module", "naming,config" }
            }
        };

        var payload = CreatePayload("ConnectionSetupRequest", setupRequest);
        await _requestClient!.SendRequestAsync(payload, cancellationToken: cancellationToken);
    }

    private async Task StartBiStreamAsync(CancellationToken cancellationToken)
    {
        _biStream = _biStreamClient!.RequestBiStream(cancellationToken: _cts.Token);

        // Start receiving messages
        _ = ReceiveStreamMessagesAsync(_cts.Token);
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

                    _logger?.LogDebug("Received push message of type {Type}", type);

                    // Handle server push
                    foreach (var handler in _pushHandlers)
                    {
                        try
                        {
                            handler(type, body);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error in push handler");
                        }
                    }

                    // Send ack if needed
                    if (type.Contains("Request"))
                    {
                        await SendPushAckAsync(type, body, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing push message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in bi-stream receive loop");
            _connected = false;
        }
    }

    private async Task SendPushAckAsync(string type, string body, CancellationToken cancellationToken)
    {
        var ackType = type.Replace("Request", "Response");
        var ackPayload = CreatePayload(ackType, new { success = true });
        await _biStream!.RequestStream.WriteAsync(ackPayload, cancellationToken);
    }

    private Payload CreatePayload(string type, object request)
    {
        var json = JsonSerializer.Serialize(request);
        var body = ByteString.CopyFromUtf8(json);

        return new Payload
        {
            Metadata = new ProtoMetadata
            {
                Type = type,
                ClientIp = GetLocalIp(),
                Headers = { { "connectionId", _clientId } }
            },
            Body = new Body
            {
                Value = body
            }
        };
    }

    private void EnsureConnected()
    {
        if (!_connected)
        {
            throw new NacosException(NacosException.ClientDisconnect, "Not connected to Nacos server");
        }
    }

    private static string GetLocalIp()
    {
        try
        {
            var hostName = System.Net.Dns.GetHostName();
            var addresses = System.Net.Dns.GetHostAddresses(hostName);
            var ipv4 = addresses.FirstOrDefault(a => 
                a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
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

        await _cts.CancelAsync();
        
        if (_biStream != null)
        {
            await _biStream.RequestStream.CompleteAsync();
            _biStream.Dispose();
        }

        _channel?.Dispose();
        _cts.Dispose();
        _disposed = true;
    }

    private class ConnectionSetupRequest
    {
        public string ClientVersion { get; set; } = string.Empty;
        public string? Tenant { get; set; }
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}
