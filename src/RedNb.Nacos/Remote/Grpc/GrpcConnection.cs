using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using RedNb.Nacos.Remote.Grpc.Models;
using RedNb.Nacos.Remote.Grpc.Proto;

// 使用别名避免命名冲突
using ProtoMetadata = RedNb.Nacos.Remote.Grpc.Proto.Metadata;
using ProtoPayload = RedNb.Nacos.Remote.Grpc.Proto.Payload;

namespace RedNb.Nacos.Remote.Grpc;

/// <summary>
/// gRPC 连接信息
/// </summary>
public sealed class GrpcConnection : IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly CallInvoker _callInvoker;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    // 定义 gRPC 方法
    private static readonly Method<ProtoPayload, ProtoPayload> RequestMethod = new(
        MethodType.Unary,
        "Request",
        "request",
        Marshallers.Create(
            payload => payload.ToByteArray(),
            bytes => ProtoPayload.Parser.ParseFrom(bytes)),
        Marshallers.Create(
            payload => payload.ToByteArray(),
            bytes => ProtoPayload.Parser.ParseFrom(bytes)));

    private static readonly Method<ProtoPayload, ProtoPayload> BiStreamMethod = new(
        MethodType.DuplexStreaming,
        "BiRequestStream",
        "requestBiStream",
        Marshallers.Create(
            payload => payload.ToByteArray(),
            bytes => ProtoPayload.Parser.ParseFrom(bytes)),
        Marshallers.Create(
            payload => payload.ToByteArray(),
            bytes => ProtoPayload.Parser.ParseFrom(bytes)));

    private AsyncDuplexStreamingCall<ProtoPayload, ProtoPayload>? _biStream;
    private CancellationTokenSource? _streamCts;
    private Task? _receiveTask;

    /// <summary>
    /// 连接ID
    /// </summary>
    public string? ConnectionId { get; private set; }

    /// <summary>
    /// 服务器地址
    /// </summary>
    public string ServerAddress { get; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public EConnectionStatus Status { get; private set; } = EConnectionStatus.Disconnected;

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActiveTime { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// 服务端推送处理器
    /// </summary>
    public Func<NacosRequest, Task<NacosResponse?>>? ServerPushHandler { get; set; }

    public GrpcConnection(string serverAddress, ILogger logger)
    {
        ServerAddress = serverAddress;
        _logger = logger;

        var channelOptions = new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan
            }
        };

        _channel = GrpcChannel.ForAddress(serverAddress, channelOptions);
        _callInvoker = _channel.CreateCallInvoker();
    }

    /// <summary>
    /// 连接到服务器
    /// </summary>
    public async Task<bool> ConnectAsync(string tenant, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (Status == EConnectionStatus.Connected)
            {
                return true;
            }

            Status = EConnectionStatus.Connecting;
            _logger.LogDebug("正在连接到 Nacos gRPC 服务: {ServerAddress}", ServerAddress);

            // 1. 服务端检查
            var checkRequest = new ServerCheckRequest();
            var checkResponse = await RequestAsync<ServerCheckRequest, ServerCheckResponse>(
                checkRequest, cancellationToken);

            if (checkResponse == null || !checkResponse.IsSuccess)
            {
                _logger.LogError("服务端检查失败: {Message}", checkResponse?.Message);
                Status = EConnectionStatus.Disconnected;
                return false;
            }

            ConnectionId = checkResponse.ConnectionId;
            _logger.LogDebug("服务端检查通过，ConnectionId: {ConnectionId}", ConnectionId);

            // 2. 建立双向流
            _streamCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _biStream = _callInvoker.AsyncDuplexStreamingCall(
                BiStreamMethod,
                null,
                new CallOptions(cancellationToken: _streamCts.Token));

            // 3. 发送连接建立请求
            var setupRequest = new ConnectionSetupRequest
            {
                ClientVersion = "Nacos-CSharp-Client:v1.0.0",
                Tenant = tenant,
                Abilities = new ClientAbilities
                {
                    RemoteAbility = new RemoteAbility { SupportRemoteConnection = true },
                    ConfigAbility = new ConfigAbility { SupportRemoteMetrics = true },
                    NamingAbility = new NamingAbility
                    {
                        SupportDeltaPush = true,
                        SupportRemoteMetric = true
                    }
                },
                Labels = new Dictionary<string, string>
                {
                    ["source"] = "sdk",
                    ["module"] = "naming,config",
                    ["AppName"] = "unknown"
                }
            };

            await SendBiStreamRequestAsync(setupRequest, cancellationToken);

            // 4. 启动接收任务
            _receiveTask = Task.Run(() => ReceiveLoopAsync(_streamCts.Token), _streamCts.Token);

            Status = EConnectionStatus.Connected;
            LastActiveTime = DateTime.UtcNow;

            _logger.LogInformation("已连接到 Nacos gRPC 服务: {ServerAddress}", ServerAddress);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接 Nacos gRPC 服务失败: {ServerAddress}", ServerAddress);
            Status = EConnectionStatus.Disconnected;
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 发送请求并等待响应
    /// </summary>
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : NacosRequest
        where TResponse : NacosResponse
    {
        var payload = CreatePayload(request);

        try
        {
            var call = _callInvoker.AsyncUnaryCall(
                RequestMethod,
                null,
                new CallOptions(cancellationToken: cancellationToken),
                payload);

            var responsePayload = await call.ResponseAsync;

            LastActiveTime = DateTime.UtcNow;

            return ParseResponse<TResponse>(responsePayload);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC 请求失败: {RequestType}", request.GetRequestType());
            throw new NacosConnectionException($"gRPC 请求失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 通过双向流发送请求
    /// </summary>
    public async Task SendBiStreamRequestAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : NacosRequest
    {
        if (_biStream == null)
        {
            throw new NacosConnectionException("双向流未建立");
        }

        var payload = CreatePayload(request);
        await _biStream.RequestStream.WriteAsync(payload, cancellationToken);
        LastActiveTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 接收循环
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_biStream == null) return;

        try
        {
            await foreach (var payload in _biStream.ResponseStream.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await HandleServerPushAsync(payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理服务端推送消息失败");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("双向流接收已取消");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            _logger.LogDebug("双向流已关闭");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "双向流接收异常");
            Status = EConnectionStatus.Disconnected;
        }
    }

    /// <summary>
    /// 处理服务端推送
    /// </summary>
    private async Task HandleServerPushAsync(ProtoPayload payload)
    {
        var requestType = payload.Metadata?.Type;
        if (string.IsNullOrEmpty(requestType))
        {
            return;
        }

        LastActiveTime = DateTime.UtcNow;
        _logger.LogDebug("收到服务端推送: {RequestType}", requestType);

        NacosRequest? request = requestType switch
        {
            "ClientDetectionRequest" => ParseRequest<ClientDetectionRequest>(payload),
            "ConnectResetRequest" => ParseRequest<ConnectResetRequest>(payload),
            "ConfigChangeNotifyRequest" => ParseRequest<ConfigChangeNotifyRequest>(payload),
            "NotifySubscriberRequest" => ParseRequest<NotifySubscriberRequest>(payload),
            _ => null
        };

        if (request == null)
        {
            _logger.LogWarning("未知的服务端推送类型: {RequestType}", requestType);
            return;
        }

        // 处理内置请求
        NacosResponse? response = null;

        if (request is ClientDetectionRequest)
        {
            response = new ClientDetectionResponse { ResultCode = 200 };
        }
        else if (request is ConnectResetRequest resetRequest)
        {
            _logger.LogWarning("收到连接重置请求，需要重新连接: {ServerIp}:{ServerPort}",
                resetRequest.ServerIp, resetRequest.ServerPort);
            response = new ConnectResetResponse { ResultCode = 200 };
            Status = EConnectionStatus.Disconnected;
        }
        else if (ServerPushHandler != null)
        {
            response = await ServerPushHandler(request);
        }

        // 发送响应
        if (response != null)
        {
            response.RequestId = request.RequestId;
            await SendResponseAsync(response);
        }
    }

    /// <summary>
    /// 发送响应
    /// </summary>
    private async Task SendResponseAsync(NacosResponse response)
    {
        if (_biStream == null) return;

        var payload = CreateResponsePayload(response);
        await _biStream.RequestStream.WriteAsync(payload);
    }

    /// <summary>
    /// 创建请求 Payload
    /// </summary>
    private static ProtoPayload CreatePayload<TRequest>(TRequest request) where TRequest : NacosRequest
    {
        var json = JsonSerializer.Serialize(request);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        return new ProtoPayload
        {
            Metadata = new ProtoMetadata
            {
                Type = request.GetRequestType(),
                ClientIp = NetworkUtils.GetLocalIp()
            },
            Body = new Any
            {
                TypeUrl = "type.googleapis.com/google.protobuf.BytesValue",
                Value = ByteString.CopyFrom(jsonBytes)
            }
        };
    }

    /// <summary>
    /// 创建响应 Payload
    /// </summary>
    private static ProtoPayload CreateResponsePayload(NacosResponse response)
    {
        var json = JsonSerializer.Serialize(response);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        return new ProtoPayload
        {
            Metadata = new ProtoMetadata
            {
                Type = response.GetResponseType(),
                ClientIp = NetworkUtils.GetLocalIp()
            },
            Body = new Any
            {
                TypeUrl = "type.googleapis.com/google.protobuf.BytesValue",
                Value = ByteString.CopyFrom(jsonBytes)
            }
        };
    }

    /// <summary>
    /// 解析响应
    /// </summary>
    private TResponse? ParseResponse<TResponse>(ProtoPayload payload) where TResponse : NacosResponse
    {
        try
        {
            if (payload.Body == null)
            {
                return null;
            }

            var json = ExtractJsonFromPayload(payload);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            _logger.LogDebug("收到响应 JSON: {Json}", json);
            return JsonSerializer.Deserialize<TResponse>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析响应失败");
            return null;
        }
    }

    /// <summary>
    /// 解析请求
    /// </summary>
    private TRequest? ParseRequest<TRequest>(ProtoPayload payload) where TRequest : NacosRequest
    {
        try
        {
            var json = ExtractJsonFromPayload(payload);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<TRequest>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析请求失败");
            return null;
        }
    }

    /// <summary>
    /// 从 Payload 提取 JSON
    /// </summary>
    private static string? ExtractJsonFromPayload(ProtoPayload payload)
    {
        if (payload.Body == null)
        {
            return null;
        }

        // Nacos 使用 JSON 作为 body 内容，直接从 Value 字段获取字节
        var bodyBytes = payload.Body.Value.ToByteArray();
        return Encoding.UTF8.GetString(bodyBytes);
    }

    public async ValueTask DisposeAsync()
    {
        if (_streamCts != null)
        {
            await _streamCts.CancelAsync();
            _streamCts.Dispose();
        }

        if (_biStream != null)
        {
            _biStream.Dispose();
        }

        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask;
            }
            catch (OperationCanceledException)
            {
                // 忽略
            }
        }

        _channel.Dispose();
        _lock.Dispose();
    }
}
