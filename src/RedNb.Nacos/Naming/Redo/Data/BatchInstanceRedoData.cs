using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Naming.Redo.Data;

/// <summary>
/// 批量服务实例注册 Redo 数据
/// 参考 Java SDK: com.alibaba.nacos.client.naming.remote.gprc.redo.data.BatchInstanceRedoData
/// </summary>
public class BatchInstanceRedoData : InstanceRedoData
{
    /// <summary>
    /// 批量实例列表
    /// </summary>
    public List<Instance> Instances { get; set; } = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    protected BatchInstanceRedoData(string serviceName, string groupName)
        : base(serviceName, groupName)
    {
    }

    /// <summary>
    /// 构建批量服务实例注册 Redo 数据
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="instances">实例列表</param>
    /// <returns>新的 BatchInstanceRedoData</returns>
    public static BatchInstanceRedoData Build(string serviceName, string groupName, List<Instance> instances)
    {
        var result = new BatchInstanceRedoData(serviceName, groupName)
        {
            Instances = instances
        };
        return result;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (this == obj) return true;
        if (obj is not BatchInstanceRedoData other) return false;
        if (!base.Equals(obj)) return false;
        return Instances.SequenceEqual(other.Instances);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = base.GetHashCode();
        foreach (var instance in Instances)
        {
            hash = HashCode.Combine(hash, instance);
        }
        return hash;
    }
}
