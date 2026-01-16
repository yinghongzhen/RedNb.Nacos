using Microsoft.AspNetCore.Mvc;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Sample.WebApi.Controllers;

/// <summary>
/// Controller for Naming/Service Discovery operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NamingController : ControllerBase
{
    private readonly INamingService _namingService;
    private readonly ILogger<NamingController> _logger;

    public NamingController(INamingService namingService, ILogger<NamingController> logger)
    {
        _namingService = namingService;
        _logger = logger;
    }

    /// <summary>
    /// Get all instances of a service.
    /// </summary>
    [HttpGet("instances")]
    public async Task<IActionResult> GetInstances(
        [FromQuery] string serviceName,
        [FromQuery] string group = "DEFAULT_GROUP",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var instances = await _namingService.GetAllInstancesAsync(
                serviceName, group, cancellationToken);

            return Ok(new
            {
                serviceName,
                group,
                count = instances.Count,
                instances = instances.Select(i => new
                {
                    i.InstanceId,
                    i.Ip,
                    i.Port,
                    i.Weight,
                    i.Healthy,
                    i.Enabled,
                    i.ClusterName,
                    i.Metadata
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting instances for {ServiceName}@{Group}", serviceName, group);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get healthy instances of a service.
    /// </summary>
    [HttpGet("instances/healthy")]
    public async Task<IActionResult> GetHealthyInstances(
        [FromQuery] string serviceName,
        [FromQuery] string group = "DEFAULT_GROUP",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var instances = await _namingService.SelectInstancesAsync(
                serviceName, group, true, cancellationToken);

            return Ok(new
            {
                serviceName,
                group,
                count = instances.Count,
                instances = instances.Select(i => new
                {
                    i.Ip,
                    i.Port,
                    i.Weight,
                    i.Healthy
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting healthy instances for {ServiceName}", serviceName);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get one healthy instance (weighted random selection).
    /// </summary>
    [HttpGet("instances/one")]
    public async Task<IActionResult> GetOneHealthyInstance(
        [FromQuery] string serviceName,
        [FromQuery] string group = "DEFAULT_GROUP",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var instance = await _namingService.SelectOneHealthyInstanceAsync(
                serviceName, group, new List<string>(), false, cancellationToken);

            if (instance == null)
            {
                return NotFound(new { message = $"No healthy instance found for {serviceName}" });
            }

            return Ok(new
            {
                serviceName,
                group,
                instance = new
                {
                    instance.Ip,
                    instance.Port,
                    instance.Weight,
                    instance.Healthy,
                    instance.Metadata
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting instance for {ServiceName}", serviceName);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Register a service instance.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterInstance(
        [FromBody] RegisterInstanceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var instance = new Instance
            {
                Ip = request.Ip,
                Port = request.Port,
                Weight = request.Weight ?? 1.0,
                Healthy = true,
                Enabled = true,
                Ephemeral = request.Ephemeral ?? true,
                ClusterName = request.ClusterName ?? "DEFAULT",
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            await _namingService.RegisterInstanceAsync(
                request.ServiceName,
                request.Group ?? "DEFAULT_GROUP",
                instance,
                cancellationToken);

            return Ok(new { success = true, message = "Instance registered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering instance for {ServiceName}", request.ServiceName);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deregister a service instance.
    /// </summary>
    [HttpPost("deregister")]
    public async Task<IActionResult> DeregisterInstance(
        [FromBody] DeregisterInstanceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _namingService.DeregisterInstanceAsync(
                request.ServiceName,
                request.Group ?? "DEFAULT_GROUP",
                request.Ip,
                request.Port,
                cancellationToken);

            return Ok(new { success = true, message = "Instance deregistered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deregistering instance for {ServiceName}", request.ServiceName);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get services list.
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServices(
        [FromQuery] int pageNo = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string group = "DEFAULT_GROUP",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var services = await _namingService.GetServicesOfServerAsync(
                pageNo, pageSize, group, cancellationToken);

            return Ok(new
            {
                group,
                pageNo,
                pageSize,
                count = services.Count,
                services = services.Data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting services list");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get server status.
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var status = _namingService.GetServerStatus();
        return Ok(new { status });
    }
}

public class RegisterInstanceRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public double? Weight { get; set; }
    public bool? Ephemeral { get; set; }
    public string? ClusterName { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class DeregisterInstanceRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
}
