namespace RedNb.Nacos.Ability;

/// <summary>
/// 客户端能力键枚举
/// </summary>
public enum AbilityKey
{
    /// <summary>
    /// SDK 客户端模糊监听能力
    /// </summary>
    SdkClientFuzzyWatch,

    /// <summary>
    /// SDK 客户端分布式锁能力
    /// </summary>
    SdkClientDistributedLock,

    /// <summary>
    /// SDK MCP 注册能力
    /// </summary>
    SdkMcpRegistry,

    /// <summary>
    /// SDK Agent 注册能力
    /// </summary>
    SdkAgentRegistry
}

/// <summary>
/// 能力键扩展方法
/// </summary>
public static class AbilityKeyExtensions
{
    /// <summary>
    /// 获取能力键的字符串名称
    /// </summary>
    public static string GetKeyName(this AbilityKey key)
    {
        return key switch
        {
            AbilityKey.SdkClientFuzzyWatch => "fuzzyWatch",
            AbilityKey.SdkClientDistributedLock => "lock",
            AbilityKey.SdkMcpRegistry => "mcp",
            AbilityKey.SdkAgentRegistry => "agent",
            _ => key.ToString().ToLowerInvariant()
        };
    }

    /// <summary>
    /// 获取能力键的描述
    /// </summary>
    public static string GetDescription(this AbilityKey key)
    {
        return key switch
        {
            AbilityKey.SdkClientFuzzyWatch => "客户端模糊监听能力，用于配置服务的模糊订阅",
            AbilityKey.SdkClientDistributedLock => "客户端分布式锁能力，用于分布式锁服务",
            AbilityKey.SdkMcpRegistry => "MCP 注册能力，用于模型上下文协议服务注册",
            AbilityKey.SdkAgentRegistry => "Agent 注册能力，用于智能代理服务注册",
            _ => string.Empty
        };
    }

    /// <summary>
    /// 获取能力键对应的能力模式
    /// </summary>
    public static AbilityMode GetMode(this AbilityKey key)
    {
        return key switch
        {
            AbilityKey.SdkClientFuzzyWatch => AbilityMode.SdkClient,
            AbilityKey.SdkClientDistributedLock => AbilityMode.SdkClient,
            AbilityKey.SdkMcpRegistry => AbilityMode.SdkClient,
            AbilityKey.SdkAgentRegistry => AbilityMode.SdkClient,
            _ => AbilityMode.SdkClient
        };
    }

    /// <summary>
    /// 从字符串名称解析能力键
    /// </summary>
    public static AbilityKey? FromKeyName(string keyName)
    {
        return keyName.ToLowerInvariant() switch
        {
            "fuzzywatch" => AbilityKey.SdkClientFuzzyWatch,
            "lock" => AbilityKey.SdkClientDistributedLock,
            "mcp" => AbilityKey.SdkMcpRegistry,
            "agent" => AbilityKey.SdkAgentRegistry,
            _ => null
        };
    }

    /// <summary>
    /// 获取指定模式的所有能力键
    /// </summary>
    public static IEnumerable<AbilityKey> GetKeysForMode(AbilityMode mode)
    {
        return Enum.GetValues<AbilityKey>().Where(k => k.GetMode() == mode);
    }
}
