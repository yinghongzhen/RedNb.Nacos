using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Naming.Cache;

/// <summary>
/// 与上次回调相比，实例的差异
/// 参考 Java SDK: com.alibaba.nacos.client.naming.event.InstancesDiff
/// </summary>
public class InstancesDiff
{
    private readonly List<Instance> _addedInstances = new();
    private readonly List<Instance> _removedInstances = new();
    private readonly List<Instance> _modifiedInstances = new();

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public InstancesDiff()
    {
    }

    /// <summary>
    /// 带参构造函数
    /// </summary>
    public InstancesDiff(IEnumerable<Instance>? addedInstances, IEnumerable<Instance>? removedInstances, IEnumerable<Instance>? modifiedInstances)
    {
        SetAddedInstances(addedInstances);
        SetRemovedInstances(removedInstances);
        SetModifiedInstances(modifiedInstances);
    }

    /// <summary>
    /// 新增的实例
    /// </summary>
    public IReadOnlyList<Instance> AddedInstances => _addedInstances;

    /// <summary>
    /// 移除的实例
    /// </summary>
    public IReadOnlyList<Instance> RemovedInstances => _removedInstances;

    /// <summary>
    /// 修改的实例
    /// </summary>
    public IReadOnlyList<Instance> ModifiedInstances => _modifiedInstances;

    /// <summary>
    /// 设置新增的实例
    /// </summary>
    public void SetAddedInstances(IEnumerable<Instance>? instances)
    {
        _addedInstances.Clear();
        if (instances != null)
        {
            _addedInstances.AddRange(instances);
        }
    }

    /// <summary>
    /// 设置移除的实例
    /// </summary>
    public void SetRemovedInstances(IEnumerable<Instance>? instances)
    {
        _removedInstances.Clear();
        if (instances != null)
        {
            _removedInstances.AddRange(instances);
        }
    }

    /// <summary>
    /// 设置修改的实例
    /// </summary>
    public void SetModifiedInstances(IEnumerable<Instance>? instances)
    {
        _modifiedInstances.Clear();
        if (instances != null)
        {
            _modifiedInstances.AddRange(instances);
        }
    }

    /// <summary>
    /// 检查是否有任何实例发生变化
    /// </summary>
    /// <returns>如果有实例发生变化则返回 true</returns>
    public bool HasDifferent()
    {
        return IsAdded() || IsRemoved() || IsModified();
    }

    /// <summary>
    /// 检查是否有新增的实例
    /// </summary>
    /// <returns>如果有新增的实例则返回 true</returns>
    public bool IsAdded() => _addedInstances.Count > 0;

    /// <summary>
    /// 检查是否有移除的实例
    /// </summary>
    /// <returns>如果有移除的实例则返回 true</returns>
    public bool IsRemoved() => _removedInstances.Count > 0;

    /// <summary>
    /// 检查是否有修改的实例
    /// </summary>
    /// <returns>如果有修改的实例则返回 true</returns>
    public bool IsModified() => _modifiedInstances.Count > 0;
}
