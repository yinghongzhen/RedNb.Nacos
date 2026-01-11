using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Client.Naming;

/// <summary>
/// Handles heartbeat sending for registered instances.
/// </summary>
public class BeatReactor : IDisposable
{
    private readonly NacosNamingService _namingService;
    private readonly NacosClientOptions _options;
    private readonly ILogger? _logger;
    private readonly ConcurrentDictionary<string, BeatInfo> _beatInfoMap = new();
    private readonly CancellationTokenSource _cts;
    private bool _disposed;

    public BeatReactor(NacosNamingService namingService, NacosClientOptions options, ILogger? logger = null)
    {
        _namingService = namingService;
        _options = options;
        _logger = logger;
        _cts = new CancellationTokenSource();

        // Start the beat task
        _ = BeatTaskAsync(_cts.Token);
    }

    /// <summary>
    /// Adds beat info for an instance.
    /// </summary>
    public void AddBeatInfo(string serviceName, string groupName, Instance instance)
    {
        var key = GetKey(serviceName, groupName, instance);
        var beatInfo = new BeatInfo
        {
            ServiceName = serviceName,
            GroupName = groupName,
            Instance = instance,
            Period = instance.GetInstanceHeartBeatInterval()
        };

        _beatInfoMap[key] = beatInfo;
        _logger?.LogDebug("Added beat info for {Key}", key);
    }

    /// <summary>
    /// Removes beat info for an instance.
    /// </summary>
    public void RemoveBeatInfo(string serviceName, string groupName, Instance instance)
    {
        var key = GetKey(serviceName, groupName, instance);
        _beatInfoMap.TryRemove(key, out _);
        _logger?.LogDebug("Removed beat info for {Key}", key);
    }

    private async Task BeatTaskAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting beat reactor");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, cancellationToken);

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (var kvp in _beatInfoMap)
                {
                    var beatInfo = kvp.Value;
                    
                    if (now - beatInfo.LastBeatTime >= beatInfo.Period)
                    {
                        try
                        {
                            await _namingService.SendBeatAsync(
                                beatInfo.ServiceName, 
                                beatInfo.GroupName, 
                                beatInfo.Instance, 
                                cancellationToken);
                            
                            beatInfo.LastBeatTime = now;
                            _logger?.LogDebug("Sent heartbeat for {Key}", kvp.Key);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to send heartbeat for {Key}", kvp.Key);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in beat task");
            }
        }

        _logger?.LogInformation("Beat reactor stopped");
    }

    private static string GetKey(string serviceName, string groupName, Instance instance)
    {
        return $"{groupName}@@{serviceName}@@{instance.Ip}:{instance.Port}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _cts.Cancel();
        _cts.Dispose();
        _disposed = true;
    }

    private class BeatInfo
    {
        public string ServiceName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public Instance Instance { get; set; } = new();
        public long Period { get; set; }
        public long LastBeatTime { get; set; }
    }
}
