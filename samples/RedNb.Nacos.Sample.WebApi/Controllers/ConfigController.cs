using Microsoft.AspNetCore.Mvc;
using RedNb.Nacos.Core.Config;

namespace RedNb.Nacos.Sample.WebApi.Controllers;

/// <summary>
/// Controller for Config operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfigService _configService;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(IConfigService configService, ILogger<ConfigController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Get a config value.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConfig(
        [FromQuery] string dataId,
        [FromQuery] string group = "DEFAULT_GROUP",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await _configService.GetConfigAsync(dataId, group, 5000, cancellationToken);

            if (content == null)
            {
                return NotFound(new { message = $"Config not found: {dataId}@{group}" });
            }

            return Ok(new
            {
                dataId,
                group,
                content
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting config {DataId}@{Group}", dataId, group);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Publish a config value.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PublishConfig(
        [FromBody] PublishConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _configService.PublishConfigAsync(
                request.DataId,
                request.Group ?? "DEFAULT_GROUP",
                request.Content,
                request.Type ?? ConfigType.Default,
                cancellationToken);

            if (!result)
            {
                return StatusCode(500, new { message = "Failed to publish config" });
            }

            return Ok(new { success = true, message = "Config published successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing config");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a config.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteConfig(
        [FromQuery] string dataId,
        [FromQuery] string group = "DEFAULT_GROUP",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _configService.RemoveConfigAsync(dataId, group, cancellationToken);

            if (!result)
            {
                return StatusCode(500, new { message = "Failed to delete config" });
            }

            return Ok(new { success = true, message = "Config deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting config {DataId}@{Group}", dataId, group);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get server status.
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var status = _configService.GetServerStatus();
        return Ok(new { status });
    }
}

public class PublishConfigRequest
{
    public string DataId { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Type { get; set; }
}
