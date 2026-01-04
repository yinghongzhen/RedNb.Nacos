namespace RedNb.Nacos.Naming;

/// <summary>
/// 事件监听器接口
/// </summary>
public interface IEventListener
{
    /// <summary>
    /// 接收服务变更事件
    /// </summary>
    Task OnEventAsync(ServiceInfo serviceInfo);
}

/// <summary>
/// 命名变更监听器接口（支持增量变更）
/// </summary>
public interface INamingChangeListener
{
    /// <summary>
    /// 接收服务变更事件（包含差异信息）
    /// </summary>
    Task OnChangeAsync(NamingChangeEvent changeEvent);
}

/// <summary>
/// 命名变更事件
/// </summary>
public sealed record NamingChangeEvent
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// 分组名称
    /// </summary>
    public required string GroupName { get; init; }

    /// <summary>
    /// 命名空间
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// 新增的实例
    /// </summary>
    public List<Instance> AddedInstances { get; init; } = new();

    /// <summary>
    /// 移除的实例
    /// </summary>
    public List<Instance> RemovedInstances { get; init; } = new();

    /// <summary>
    /// 修改的实例
    /// </summary>
    public List<Instance> ModifiedInstances { get; init; } = new();

    /// <summary>
    /// 当前所有实例
    /// </summary>
    public List<Instance> AllInstances { get; init; } = new();

    /// <summary>
    /// 是否有变更
    /// </summary>
    public bool HasChanges => AddedInstances.Count > 0 || RemovedInstances.Count > 0 || ModifiedInstances.Count > 0;
}

/// <summary>
/// Action 事件监听器包装器
/// </summary>
internal sealed class ActionEventListener : IEventListener
{
    private readonly Action<ServiceInfo> _callback;

    public ActionEventListener(Action<ServiceInfo> callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public Task OnEventAsync(ServiceInfo serviceInfo)
    {
        _callback(serviceInfo);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Action 命名变更监听器包装器
/// </summary>
internal sealed class ActionNamingChangeListener : INamingChangeListener
{
    private readonly Action<NamingChangeEvent> _callback;

    public ActionNamingChangeListener(Action<NamingChangeEvent> callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public Task OnChangeAsync(NamingChangeEvent changeEvent)
    {
        _callback(changeEvent);
        return Task.CompletedTask;
    }
}
