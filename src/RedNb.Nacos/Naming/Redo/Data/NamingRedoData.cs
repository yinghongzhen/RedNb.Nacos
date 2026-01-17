using RedNb.Nacos.Redo;

namespace RedNb.Nacos.Naming.Redo.Data;

/// <summary>
/// Nacos 命名服务 Redo 数据基类
/// 参考 Java SDK: com.alibaba.nacos.client.naming.remote.gprc.redo.data.NamingRedoData
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public abstract class NamingRedoData<T> : RedoData<T>
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// 分组名称
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    protected NamingRedoData(string serviceName, string groupName)
    {
        ServiceName = serviceName;
        GroupName = groupName;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (this == obj) return true;
        if (obj == null || GetType() != obj.GetType()) return false;
        if (!base.Equals(obj)) return false;
        var redoData = (NamingRedoData<T>)obj;
        return ServiceName == redoData.ServiceName && GroupName == redoData.GroupName;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), ServiceName, GroupName);
    }
}
