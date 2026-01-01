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
