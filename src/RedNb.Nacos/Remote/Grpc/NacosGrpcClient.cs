using RedNb.Nacos.Auth;
using RedNb.Nacos.Remote.Grpc.Models;

namespace RedNb.Nacos.Remote.Grpc;

/// <summary>
/// Nacos gRPC 客户端实现
/// </summary>
public sealed class NacosGrpcClient : INacosGrpcClient
{
    private readonly NacosOptions _options;
    private readonly IAuthService _authService;
    private readonly ILogger<NacosGrpcClient> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ConcurrentDictionary<Type, Delegate> _pushHandlers = new();

    private GrpcConnection? _currentConnection;
    private CancellationTokenSource? _healthCheckCts;
    private Task? _healthCheckTask;
    private int _currentServerIndex;
    private bool _disposed;

    /// <inheritdoc />
    public bool IsConnected => _currentConnection?.Status == EConnectionStatus.Connected;

    public NacosGrpcClient(
        IOptions<NacosOptions> options,
        IAuthService authService,
        ILogger<NacosGrpcClient> logger)
    {
        _options = options.Value;
        _authService = authService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
            {
                return true;
            }

            // 尝试连接到所有服务器
            for (var i = 0; i < _options.ServerAddresses.Count; i++)
            {
                var serverIndex = (_currentServerIndex + i) % _options.ServerAddresses.Count;
                var serverAddress = GetGrpcServerAddress(_options.ServerAddresses[serverIndex]);

                _logger.LogDebug("尝试连接到服务器: {ServerAddress}", serverAddress);

                var connection = new GrpcConnection(serverAddress, _logger);
                connection.ServerPushHandler = HandleServerPushAsync;

                if (await connection.ConnectAsync(_options.Namespace, cancellationToken))
                {
                    // 清理旧连接
                    if (_currentConnection != null)
                    {
                        await _currentConnection.DisposeAsync();
                    }

                    _currentConnection = connection;
                    _currentServerIndex = serverIndex;

                    // 启动健康检查
                    StartHealthCheck();

                    return true;
                }

                await connection.DisposeAsync();
            }

            _logger.LogError("无法连接到任何 Nacos 服务器");
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : NacosRequest
        where TResponse : NacosResponse
    {
        // 确保已连接
        if (!IsConnected)
        {
            var connected = await ConnectAsync(cancellationToken);
            if (!connected)
            {
                throw new NacosConnectionException("无法连接到 Nacos 服务器");
            }
        }

        // 添加认证信息
        await AddAuthHeadersAsync(request, cancellationToken);

        try
        {
            var response = await _currentConnection!.RequestAsync<TRequest, TResponse>(
                request, cancellationToken);

            if (response != null && !response.IsSuccess)
            {
                _logger.LogWarning("请求失败: {RequestType}, ErrorCode={ErrorCode}, Message={Message}",
                    request.GetRequestType(), response.ErrorCode, response.Message);

                // 如果是认证失败，尝试刷新 Token
                if (response.ErrorCode == 401 || response.ErrorCode == 403)
                {
                    await _authService.RefreshTokenAsync(cancellationToken);
                    await AddAuthHeadersAsync(request, cancellationToken);
                    return await _currentConnection.RequestAsync<TRequest, TResponse>(
                        request, cancellationToken);
                }
            }

            return response;
        }
        catch (NacosConnectionException)
        {
            // 连接失败，尝试重连
            _logger.LogWarning("gRPC 连接异常，尝试重新连接...");

            if (await ReconnectAsync(cancellationToken))
            {
                return await _currentConnection!.RequestAsync<TRequest, TResponse>(
                    request, cancellationToken);
            }

            throw;
        }
    }

    /// <inheritdoc />
    public void RegisterPushHandler<TRequest>(Func<TRequest, Task<NacosResponse?>> handler)
        where TRequest : NacosRequest
    {
        _pushHandlers[typeof(TRequest)] = handler;
    }

    /// <inheritdoc />
    public async Task<bool> ReconnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            // 停止健康检查
            StopHealthCheck();

            // 清理当前连接
            if (_currentConnection != null)
            {
                await _currentConnection.DisposeAsync();
                _currentConnection = null;
            }

            // 尝试下一个服务器
            _currentServerIndex = (_currentServerIndex + 1) % _options.ServerAddresses.Count;
        }
        finally
        {
            _connectionLock.Release();
        }

        return await ConnectAsync(cancellationToken);
    }

    /// <summary>
    /// 处理服务端推送
    /// </summary>
    private async Task<NacosResponse?> HandleServerPushAsync(NacosRequest request)
    {
        var requestType = request.GetType();

        if (_pushHandlers.TryGetValue(requestType, out var handler))
        {
            try
            {
                var method = handler.GetType().GetMethod("Invoke");
                var task = (Task<NacosResponse?>?)method?.Invoke(handler, new object[] { request });
                if (task != null)
                {
                    return await task;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理推送消息失败: {RequestType}", request.GetRequestType());
            }
        }

        return null;
    }

    /// <summary>
    /// 添加认证头
    /// </summary>
    private async Task AddAuthHeadersAsync(NacosRequest request, CancellationToken cancellationToken)
    {
        if (!_authService.IsAuthEnabled)
        {
            return;
        }

        var token = await _authService.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers["accessToken"] = token;
        }

        // 如果有 AK/SK，添加签名
        if (!string.IsNullOrEmpty(_options.AccessKey) && !string.IsNullOrEmpty(_options.SecretKey))
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            request.Headers["Timestamp"] = timestamp;
            request.Headers["Spas-AccessKey"] = _options.AccessKey;

            // TODO: 实现签名
        }
    }

    /// <summary>
    /// 获取 gRPC 服务器地址
    /// </summary>
    private string GetGrpcServerAddress(string httpAddress)
    {
        var uri = new Uri(httpAddress.StartsWith("http") ? httpAddress : $"http://{httpAddress}");
        var grpcPort = uri.Port + _options.GrpcPortOffset;
        return $"http://{uri.Host}:{grpcPort}";
    }

    /// <summary>
    /// 启动健康检查
    /// </summary>
    private void StartHealthCheck()
    {
        StopHealthCheck();

        _healthCheckCts = new CancellationTokenSource();
        _healthCheckTask = Task.Run(async () =>
        {
            var interval = TimeSpan.FromSeconds(5);

            while (!_healthCheckCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, _healthCheckCts.Token);

                    if (_currentConnection == null || _currentConnection.Status != EConnectionStatus.Connected)
                    {
                        continue;
                    }

                    // 检查连接是否超时
                    var idleTime = DateTime.UtcNow - _currentConnection.LastActiveTime;
                    if (idleTime > TimeSpan.FromSeconds(30))
                    {
                        _logger.LogDebug("发送健康检查请求...");
                        var response = await _currentConnection.RequestAsync<HealthCheckRequest, HealthCheckResponse>(
                            new HealthCheckRequest(), _healthCheckCts.Token);

                        if (response == null || !response.IsSuccess)
                        {
                            _logger.LogWarning("健康检查失败，尝试重新连接...");
                            await ReconnectAsync(_healthCheckCts.Token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "健康检查异常");
                }
            }
        }, _healthCheckCts.Token);
    }

    /// <summary>
    /// 停止健康检查
    /// </summary>
    private void StopHealthCheck()
    {
        if (_healthCheckCts != null)
        {
            _healthCheckCts.Cancel();
            _healthCheckCts.Dispose();
            _healthCheckCts = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        StopHealthCheck();

        if (_currentConnection != null)
        {
            await _currentConnection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }
}
