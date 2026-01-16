using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RedNb.Nacos.Ability;

/// <summary>
/// 客户端能力控制管理器
/// </summary>
public class ClientAbilityControlManager
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<AbilityKey, AbilityStatus> _abilities = new();
    private readonly object _lockObj = new();

    /// <summary>
    /// 当能力状态变更时触发
    /// </summary>
    public event EventHandler<AbilityChangedEventArgs>? AbilityChanged;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ClientAbilityControlManager(ILogger<ClientAbilityControlManager> logger)
    {
        _logger = logger;
        InitializeDefaultAbilities();
    }

    /// <summary>
    /// 初始化默认能力
    /// </summary>
    private void InitializeDefaultAbilities()
    {
        // 初始化所有 SDK 客户端能力，默认启用
        foreach (var key in AbilityKeyExtensions.GetKeysForMode(AbilityMode.SdkClient))
        {
            _abilities[key] = new AbilityStatus(key, enabled: true);
            _logger.LogDebug("Initialized ability: {Key}", key.GetKeyName());
        }
    }

    /// <summary>
    /// 获取能力状态
    /// </summary>
    public AbilityStatus? GetAbilityStatus(AbilityKey key)
    {
        return _abilities.TryGetValue(key, out var status) ? status : null;
    }

    /// <summary>
    /// 判断能力是否启用
    /// </summary>
    public bool IsAbilityEnabled(AbilityKey key)
    {
        return _abilities.TryGetValue(key, out var status) && status.Enabled;
    }

    /// <summary>
    /// 判断能力是否可用（客户端启用且服务端支持）
    /// </summary>
    public bool IsAbilityAvailable(AbilityKey key)
    {
        return _abilities.TryGetValue(key, out var status) && status.IsAvailable;
    }

    /// <summary>
    /// 设置能力启用状态
    /// </summary>
    public void SetAbilityEnabled(AbilityKey key, bool enabled)
    {
        lock (_lockObj)
        {
            if (_abilities.TryGetValue(key, out var status))
            {
                var oldEnabled = status.Enabled;
                status.SetEnabled(enabled);

                if (oldEnabled != enabled)
                {
                    _logger.LogInformation("Ability {Key} changed: Enabled={Enabled}", key.GetKeyName(), enabled);
                    OnAbilityChanged(new AbilityChangedEventArgs(key, oldEnabled, enabled));
                }
            }
            else
            {
                _abilities[key] = new AbilityStatus(key, enabled);
                _logger.LogInformation("Ability {Key} added: Enabled={Enabled}", key.GetKeyName(), enabled);
            }
        }
    }

    /// <summary>
    /// 更新服务端能力协商结果
    /// </summary>
    public void UpdateServerAbilities(Dictionary<string, bool> serverAbilities)
    {
        lock (_lockObj)
        {
            foreach (var kvp in serverAbilities)
            {
                var abilityKey = AbilityKeyExtensions.FromKeyName(kvp.Key);
                if (abilityKey.HasValue && _abilities.TryGetValue(abilityKey.Value, out var status))
                {
                    status.MarkNegotiated(kvp.Value);
                    _logger.LogDebug("Ability {Key} negotiated: ServerSupported={Supported}",
                        abilityKey.Value.GetKeyName(), kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// 重置所有能力的协商状态
    /// </summary>
    public void ResetNegotiation()
    {
        lock (_lockObj)
        {
            foreach (var status in _abilities.Values)
            {
                status.ResetNegotiation();
            }
            _logger.LogDebug("Reset all ability negotiations");
        }
    }

    /// <summary>
    /// 获取所有能力状态
    /// </summary>
    public IReadOnlyDictionary<AbilityKey, AbilityStatus> GetAllAbilities()
    {
        return _abilities;
    }

    /// <summary>
    /// 获取客户端能力表（用于发送给服务端）
    /// </summary>
    public Dictionary<string, bool> GetClientAbilityTable()
    {
        var result = new Dictionary<string, bool>();

        foreach (var kvp in _abilities)
        {
            result[kvp.Key.GetKeyName()] = kvp.Value.Enabled;
        }

        return result;
    }

    /// <summary>
    /// 触发能力变更事件
    /// </summary>
    protected virtual void OnAbilityChanged(AbilityChangedEventArgs e)
    {
        AbilityChanged?.Invoke(this, e);
    }
}

/// <summary>
/// 能力变更事件参数
/// </summary>
public class AbilityChangedEventArgs : EventArgs
{
    /// <summary>
    /// 能力键
    /// </summary>
    public AbilityKey Key { get; }

    /// <summary>
    /// 旧启用状态
    /// </summary>
    public bool OldEnabled { get; }

    /// <summary>
    /// 新启用状态
    /// </summary>
    public bool NewEnabled { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public AbilityChangedEventArgs(AbilityKey key, bool oldEnabled, bool newEnabled)
    {
        Key = key;
        OldEnabled = oldEnabled;
        NewEnabled = newEnabled;
    }
}
