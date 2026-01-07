using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RedNb.Nacos.Common.Options;
using RedNb.Nacos.Config;
using RedNb.Nacos.Naming;
using RedNb.Nacos.Naming.Models;
using RedNb.Nacos.Remote.Grpc;

namespace RedNb.Nacos.Sample.WebApi.Controllers;

/// <summary>
/// Nacos 功能演示控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NacosController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly INacosConfigService _configService;
    private readonly INacosNamingService _namingService;
    private readonly INacosGrpcClient? _grpcClient;
    private readonly NacosOptions _options;
    private readonly ILogger<NacosController> _logger;

    public NacosController(
        IConfiguration configuration,
        INacosConfigService configService,
        INacosNamingService namingService,
        IOptions<NacosOptions> options,
        ILogger<NacosController> logger,
        INacosGrpcClient? grpcClient = null)
    {
        _configuration = configuration;
        _configService = configService;
        _namingService = namingService;
        _grpcClient = grpcClient;
        _options = options.Value;
        _logger = logger;
    }

    #region gRPC 状态

    /// <summary>
    /// 获取 gRPC 连接状态
    /// </summary>
    [HttpGet("grpc/status")]
    public IActionResult GetGrpcStatus()
    {
        return Ok(new
        {
            enabled = _options.UseGrpc,
            grpcPortOffset = _options.GrpcPortOffset,
            isConnected = _grpcClient?.IsConnected ?? false,
            grpcEndpoints = _options.ServerAddresses.Select(addr =>
            {
                var uri = new Uri(addr.StartsWith("http") ? addr : $"http://{addr}");
                return $"{uri.Host}:{uri.Port + _options.GrpcPortOffset}";
            }).ToList()
        });
    }

    /// <summary>
    /// 测试 gRPC 连接
    /// </summary>
    [HttpPost("grpc/connect")]
    public async Task<IActionResult> TestGrpcConnect()
    {
        if (_grpcClient == null)
        {
            return BadRequest(new { success = false, message = "gRPC 客户端未启用" });
        }

        try
        {
            var connected = await _grpcClient.ConnectAsync();
            return Ok(new
            {
                success = connected,
                isConnected = _grpcClient.IsConnected,
                message = connected ? "gRPC 连接成功" : "gRPC 连接失败"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC 连接测试失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 测试 gRPC 重连
    /// </summary>
    [HttpPost("grpc/reconnect")]
    public async Task<IActionResult> TestGrpcReconnect()
    {
        if (_grpcClient == null)
        {
            return BadRequest(new { success = false, message = "gRPC 客户端未启用" });
        }

        try
        {
            var reconnected = await _grpcClient.ReconnectAsync();
            return Ok(new
            {
                success = reconnected,
                isConnected = _grpcClient.IsConnected,
                message = reconnected ? "gRPC 重连成功" : "gRPC 重连失败"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC 重连测试失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    /// <summary>
    /// 获取配置
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(
        [FromQuery] string dataId = "sample-webapi.json",
        [FromQuery] string group = "DEFAULT_GROUP")
    {
        var content = await _configService.GetConfigAsync(dataId, group);
        return Ok(new
        {
            dataId,
            group,
            content
        });
    }

    /// <summary>
    /// 发布配置
    /// </summary>
    [HttpPost("config")]
    public async Task<IActionResult> PublishConfig(
        [FromQuery] string dataId,
        [FromQuery] string group = "DEFAULT_GROUP",
        [FromBody] string content = "")
    {
        var result = await _configService.PublishConfigAsync(dataId, group, content);
        return Ok(new { success = result });
    }

    /// <summary>
    /// 删除配置
    /// </summary>
    [HttpDelete("config")]
    public async Task<IActionResult> RemoveConfig(
        [FromQuery] string dataId,
        [FromQuery] string group = "DEFAULT_GROUP")
    {
        var result = await _configService.RemoveConfigAsync(dataId, group);
        return Ok(new { success = result });
    }

    /// <summary>
    /// 获取服务列表
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServices(
        [FromQuery] string? serviceName = null,
        [FromQuery] string group = "DEFAULT_GROUP")
    {
        if (string.IsNullOrEmpty(serviceName))
        {
            // 获取所有服务
            var services = await _namingService.GetServicesAsync(group);
            return Ok(services);
        }

        // 获取指定服务的实例
        var instances = await _namingService.GetAllInstancesAsync(serviceName, group);
        return Ok(instances);
    }

    /// <summary>
    /// 获取健康实例
    /// </summary>
    [HttpGet("instances/healthy")]
    public async Task<IActionResult> GetHealthyInstances(
        [FromQuery] string serviceName,
        [FromQuery] string group = "DEFAULT_GROUP")
    {
        var instances = await _namingService.GetHealthyInstancesAsync(serviceName, group);
        return Ok(instances);
    }

    /// <summary>
    /// 选择一个实例（负载均衡）
    /// </summary>
    [HttpGet("instances/select")]
    public async Task<IActionResult> SelectInstance(
        [FromQuery] string serviceName,
        [FromQuery] string group = "DEFAULT_GROUP",
        [FromQuery] string strategy = "random")
    {
        var instance = await _namingService.SelectOneHealthyInstanceAsync(serviceName, group);
        return Ok(instance);
    }

    /// <summary>
    /// 手动注册实例
    /// </summary>
    [HttpPost("instances")]
    public async Task<IActionResult> RegisterInstance(
        [FromQuery] string serviceName,
        [FromQuery] string ip,
        [FromQuery] int port,
        [FromQuery] string group = "DEFAULT_GROUP")
    {
        var instance = new Instance
        {
            Ip = ip,
            Port = port,
            ServiceName = serviceName,
            Healthy = true,
            Enabled = true,
            Weight = 1.0
        };

        await _namingService.RegisterInstanceAsync(serviceName, group, instance);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 注销实例
    /// </summary>
    [HttpDelete("instances")]
    public async Task<IActionResult> DeregisterInstance(
        [FromQuery] string serviceName,
        [FromQuery] string ip,
        [FromQuery] int port,
        [FromQuery] string group = "DEFAULT_GROUP")
    {
        var instance = new Instance
        {
            Ip = ip,
            Port = port,
            ServiceName = serviceName
        };

        await _namingService.DeregisterInstanceAsync(serviceName, group, instance);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 获取当前配置信息
    /// </summary>
    [HttpGet("options")]
    public IActionResult GetOptions()
    {
        return Ok(new
        {
            serverAddresses = _options.ServerAddresses,
            @namespace = _options.Namespace,
            username = _options.UserName,
            naming = new
            {
                serviceName = _options.Naming.ServiceName,
                groupName = _options.Naming.GroupName,
                clusterName = _options.Naming.ClusterName,
                registerEnabled = _options.Naming.RegisterEnabled,
                weight = _options.Naming.Weight,
                metadata = _options.Naming.Metadata
            }
        });
    }

    /// <summary>
    /// 读取 IConfiguration 中的配置值
    /// </summary>
    [HttpGet("configuration")]
    public IActionResult GetConfiguration([FromQuery] string key)
    {
        var value = _configuration[key];
        return Ok(new { key, value });
    }
}
