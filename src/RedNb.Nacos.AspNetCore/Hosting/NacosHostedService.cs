namespace RedNb.Nacos.AspNetCore.Hosting;

/// <summary>
/// Nacos 后台服务（服务注册和心跳维护）
/// </summary>
public sealed class NacosHostedService : IHostedService, IAsyncDisposable
{
    private readonly NacosOptions _options;
    private readonly INacosNamingService _namingService;
    private readonly IServer _server;
    private readonly ILogger<NacosHostedService> _logger;

    private Instance? _registeredInstance;
    private CancellationTokenSource? _heartbeatCts;
    private Task? _heartbeatTask;
    private bool _disposed;

    public NacosHostedService(
        IOptions<NacosOptions> options,
        INacosNamingService namingService,
        IServer server,
        ILogger<NacosHostedService> logger)
    {
        _options = options.Value;
        _namingService = namingService;
        _server = server;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Naming.RegisterEnabled)
        {
            _logger.LogInformation("Nacos 服务注册已禁用");
            return;
        }

        if (string.IsNullOrEmpty(_options.Naming.ServiceName))
        {
            _logger.LogWarning("未配置 ServiceName，跳过服务注册");
            return;
        }

        // 延迟等待服务器启动完成
        await Task.Delay(1000, cancellationToken);

        try
        {
            var instance = CreateInstance();
            _registeredInstance = instance;

            await _namingService.RegisterInstanceAsync(
                _options.Naming.ServiceName,
                _options.Naming.GroupName,
                instance,
                cancellationToken);

            _logger.LogInformation(
                "Nacos 服务注册成功: serviceName={ServiceName}, ip={Ip}, port={Port}",
                _options.Naming.ServiceName,
                instance.Ip,
                instance.Port);

            // 启动心跳任务（如果是临时实例）
            if (_options.Naming.Ephemeral)
            {
                StartHeartbeat();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nacos 服务注册失败");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // 停止心跳
        if (_heartbeatCts != null)
        {
            await _heartbeatCts.CancelAsync();
        }

        // 注销服务
        if (_registeredInstance != null && _options.Naming.RegisterEnabled)
        {
            try
            {
                await _namingService.DeregisterInstanceAsync(
                    _options.Naming.ServiceName!,
                    _options.Naming.GroupName,
                    _registeredInstance,
                    cancellationToken);

                _logger.LogInformation("Nacos 服务注销成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nacos 服务注销失败");
            }
        }
    }

    private Instance CreateInstance()
    {
        var ip = _options.Naming.Ip;
        var port = _options.Naming.Port;

        // 自动获取 IP
        if (string.IsNullOrEmpty(ip))
        {
            ip = NetworkUtils.GetLocalIp(_options.Naming.PreferredNetworks);
        }

        // 自动获取端口
        if (port == 0)
        {
            port = GetServerPort();
        }

        var metadata = new Dictionary<string, string>(_options.Naming.Metadata);

        // 添加协议信息
        if (_options.Naming.Secure)
        {
            metadata["secure"] = "true";
        }

        return new Instance
        {
            Ip = ip,
            Port = port,
            Weight = _options.Naming.Weight,
            Enabled = _options.Naming.InstanceEnabled,
            Healthy = true,
            Ephemeral = _options.Naming.Ephemeral,
            ClusterName = _options.Naming.ClusterName,
            ServiceName = _options.Naming.ServiceName!,
            Metadata = metadata
        };
    }

    private int GetServerPort()
    {
        try
        {
            var features = _server.Features;
            var addresses = features.Get<IServerAddressesFeature>();

            if (addresses?.Addresses != null)
            {
                foreach (var address in addresses.Addresses)
                {
                    var uri = new Uri(address.Replace("*", "localhost").Replace("+", "localhost"));
                    if (uri.Port > 0)
                    {
                        return uri.Port;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "无法自动获取服务端口");
        }

        return 80;
    }

    private void StartHeartbeat()
    {
        _heartbeatCts = new CancellationTokenSource();
        _heartbeatTask = Task.Run(async () =>
        {
            while (!_heartbeatCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.Naming.HeartbeatIntervalMs, _heartbeatCts.Token);

                    if (_registeredInstance != null)
                    {
                        var success = await _namingService.SendHeartbeatAsync(
                            _options.Naming.ServiceName!,
                            _options.Naming.GroupName,
                            _registeredInstance,
                            _heartbeatCts.Token);

                        if (success)
                        {
                            _logger.LogDebug("Nacos 心跳发送成功");
                        }
                        else
                        {
                            _logger.LogWarning("Nacos 心跳发送失败，将在下次重试");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Nacos 心跳异常");
                }
            }
        }, _heartbeatCts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_heartbeatCts != null)
        {
            await _heartbeatCts.CancelAsync();
            _heartbeatCts.Dispose();
        }

        if (_heartbeatTask != null)
        {
            try
            {
                await _heartbeatTask;
            }
            catch (OperationCanceledException)
            {
                // 忽略
            }
        }
    }
}
