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
    private readonly TaskCompletionSource<bool> _connectionReadyTcs = new();

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

            // 2. 建立双向流 - 使用独立的 CancellationTokenSource，不与调用方关联
            _streamCts = new CancellationTokenSource();
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

            await SendBiStreamRequestAsync(setupRequest, CancellationToken.None);

            // 4. 启动接收任务
            _receiveTask = Task.Run(() => ReceiveLoopAsync(_streamCts.Token), _streamCts.Token);

            // 5. 等待连接就绪（服务端处理 ConnectionSetupRequest 需要时间）
            // 增加等待时间到 5 秒，确保服务端有足够时间处理连接
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
            
            try
            {
                await _connectionReadyTcs.Task.WaitAsync(timeoutCts.Token);
                _logger.LogDebug("连接就绪确认收到");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // 超时但未收到确认，添加一个延迟作为后备方案
                _logger.LogDebug("等待连接就绪超时，使用延迟等待");
                await Task.Delay(1000, CancellationToken.None);
            }

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
        // 使用带 connectionId 的 payload（如果连接已建立）
        var payload = string.IsNullOrEmpty(ConnectionId) 
            ? CreatePayload(request) 
            : CreatePayloadWithConnectionId(request);

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

        var payload = CreatePayloadWithConnectionId(request);
        await _biStream.RequestStream.WriteAsync(payload, cancellationToken);
        LastActiveTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 待处理的流请求响应
    /// </summary>
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ProtoPayload>> _pendingStreamRequests = new();

    /// <summary>
    /// 通过双向流发送请求并等待响应
    /// </summary>
    public async Task<TResponse?> StreamRequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : NacosRequest
        where TResponse : NacosResponse
    {
        if (_biStream == null)
        {
            throw new NacosConnectionException("双向流未建立");
        }

        // 生成请求ID
        var requestId = Guid.NewGuid().ToString("N");
        request.RequestId = requestId;

        // 创建等待响应的 TaskCompletionSource
        var tcs = new TaskCompletionSource<ProtoPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingStreamRequests[requestId] = tcs;

        try
        {
            // 发送请求
            var payload = CreatePayloadWithConnectionId(request);
            await _biStream.RequestStream.WriteAsync(payload, cancellationToken);
            LastActiveTime = DateTime.UtcNow;

            _logger.LogDebug("已发送流请求: {RequestType}, RequestId={RequestId}", 
                request.GetRequestType(), requestId);

            // 等待响应（带超时）
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            var responsePayload = await tcs.Task.WaitAsync(timeoutCts.Token);
            return ParseResponse<TResponse>(responsePayload);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("流请求超时: {RequestType}, RequestId={RequestId}", 
                request.GetRequestType(), requestId);
            throw new NacosConnectionException($"流请求超时: {request.GetRequestType()}");
        }
        finally
        {
            _pendingStreamRequests.TryRemove(requestId, out _);
        }
    }

    /// <summary>
    /// 接收循环
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_biStream == null) return;

        var firstMessageReceived = false;

        try
        {
            await foreach (var payload in _biStream.ResponseStream.ReadAllAsync(cancellationToken))
            {
                // 收到第一条消息，表示连接已就绪
                if (!firstMessageReceived)
                {
                    firstMessageReceived = true;
                    _connectionReadyTcs.TrySetResult(true);
                }

                try
                {
                    // 检查是否是响应消息（有 requestId 对应的待处理请求）
                    var requestId = payload.Metadata?.Headers?.GetValueOrDefault("requestId") 
                        ?? ExtractRequestIdFromPayload(payload);
                    
                    if (!string.IsNullOrEmpty(requestId) && _pendingStreamRequests.TryRemove(requestId, out var tcs))
                    {
                        // 这是对流请求的响应
                        _logger.LogDebug("收到流请求响应: RequestId={RequestId}", requestId);
                        tcs.TrySetResult(payload);
                    }
                    else
                    {
                        // 这是服务端推送
                        await HandleServerPushAsync(payload);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理服务端消息失败");
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
    /// 从 Payload 提取 RequestId
    /// </summary>
    private string? ExtractRequestIdFromPayload(ProtoPayload payload)
    {
        try
        {
            var json = ExtractJsonFromPayload(payload);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("requestId", out var requestIdElement))
            {
                return requestIdElement.GetString();
            }
        }
        catch
        {
            // 忽略解析错误
        }
        return null;
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

        var payload = CreateResponsePayloadWithConnectionId(response);
        await _biStream.RequestStream.WriteAsync(payload);
    }

    /// <summary>
    /// 创建请求 Payload（不带 connectionId，用于初始连接）
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
    /// 创建请求 Payload（带 connectionId，用于双向流通信）
    /// </summary>
    private ProtoPayload CreatePayloadWithConnectionId<TRequest>(TRequest request) where TRequest : NacosRequest
    {
        var json = JsonSerializer.Serialize(request);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        var metadata = new ProtoMetadata
        {
            Type = request.GetRequestType(),
            ClientIp = NetworkUtils.GetLocalIp()
        };

        // 添加 connectionId 到 Headers
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            metadata.Headers["connectionId"] = ConnectionId;
        }

        // 添加请求的 Headers
        foreach (var header in request.Headers)
        {
            metadata.Headers[header.Key] = header.Value;
        }

        return new ProtoPayload
        {
            Metadata = metadata,
            Body = new Any
            {
                TypeUrl = "type.googleapis.com/google.protobuf.BytesValue",
                Value = ByteString.CopyFrom(jsonBytes)
            }
        };
    }

    /// <summary>
    /// 创建响应 Payload（带 connectionId）
    /// </summary>
    private ProtoPayload CreateResponsePayloadWithConnectionId(NacosResponse response)
    {
        var json = JsonSerializer.Serialize(response);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        var metadata = new ProtoMetadata
        {
            Type = response.GetResponseType(),
            ClientIp = NetworkUtils.GetLocalIp()
        };

        // 添加 connectionId 到 Headers
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            metadata.Headers["connectionId"] = ConnectionId;
        }

        return new ProtoPayload
        {
            Metadata = metadata,
            Body = new Any
            {
                TypeUrl = "type.googleapis.com/google.protobuf.BytesValue",
                Value = ByteString.CopyFrom(jsonBytes)
            }
        };
    }

    /// <summary>
    /// 创建响应 Payload（静态方法，用于不需要 connectionId 的场景）
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
