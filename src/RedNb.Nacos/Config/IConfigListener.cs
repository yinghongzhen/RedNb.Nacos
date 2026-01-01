namespace RedNb.Nacos.Config;

/// <summary>
/// 配置监听器接口
/// </summary>
public interface IConfigListener
{
    /// <summary>
    /// 接收配置变更
    /// </summary>
    /// <param name="configInfo">配置变更事件参数</param>
    Task ReceiveConfigInfoAsync(ConfigChangedEventArgs configInfo);
}

/// <summary>
/// 配置变更事件参数
/// </summary>
public sealed record ConfigChangedEventArgs
{
    /// <summary>
    /// 配置 ID
    /// </summary>
    public required string DataId { get; init; }

    /// <summary>
    /// 分组名称
    /// </summary>
    public required string Group { get; init; }

    /// <summary>
    /// 命名空间
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// 新配置内容
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// 旧配置内容
    /// </summary>
    public string? OldContent { get; init; }

    /// <summary>
    /// 变更类型
    /// </summary>
    public ConfigChangeType ChangeType { get; init; }
}

/// <summary>
/// 配置变更类型
/// </summary>
public enum ConfigChangeType
{
    /// <summary>
    /// 新增
    /// </summary>
    Added,

    /// <summary>
    /// 修改
    /// </summary>
    Modified,

    /// <summary>
    /// 删除
    /// </summary>
    Deleted
}

/// <summary>
/// Action 配置监听器包装器
/// </summary>
internal sealed class ActionConfigListener : IConfigListener
{
    private readonly Action<string?> _callback;

    public ActionConfigListener(Action<string?> callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public Task ReceiveConfigInfoAsync(ConfigChangedEventArgs configInfo)
    {
        _callback(configInfo.Content);
        return Task.CompletedTask;
    }
}
