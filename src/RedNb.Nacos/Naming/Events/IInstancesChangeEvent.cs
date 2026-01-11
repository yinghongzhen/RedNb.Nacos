namespace RedNb.Nacos.Core.Naming;

/// <summary>
/// Event for instances change notification.
/// </summary>
public interface IInstancesChangeEvent
{
    /// <summary>
    /// Gets the service name.
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Gets the group name.
    /// </summary>
    string GroupName { get; }

    /// <summary>
    /// Gets the clusters.
    /// </summary>
    string? Clusters { get; }

    /// <summary>
    /// Gets the current instances.
    /// </summary>
    List<Instance> Instances { get; }

    /// <summary>
    /// Gets the added instances.
    /// </summary>
    List<Instance>? AddedInstances { get; }

    /// <summary>
    /// Gets the removed instances.
    /// </summary>
    List<Instance>? RemovedInstances { get; }

    /// <summary>
    /// Gets the modified instances.
    /// </summary>
    List<Instance>? ModifiedInstances { get; }
}

/// <summary>
/// Implementation of instances change event.
/// </summary>
public class InstancesChangeEvent : IInstancesChangeEvent
{
    public string ServiceName { get; set; } = string.Empty;
    public string GroupName { get; set; } = NacosConstants.DefaultGroup;
    public string? Clusters { get; set; }
    public List<Instance> Instances { get; set; } = new();
    public List<Instance>? AddedInstances { get; set; }
    public List<Instance>? RemovedInstances { get; set; }
    public List<Instance>? ModifiedInstances { get; set; }
}
