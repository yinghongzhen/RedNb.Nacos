using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Naming.Redo.Data;

/// <summary>
/// 服务实例注册 Redo 数据
/// 参考 Java SDK: com.alibaba.nacos.client.naming.remote.gprc.redo.data.InstanceRedoData
/// </summary>
public class InstanceRedoData : NamingRedoData<Instance>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    protected InstanceRedoData(string serviceName, string groupName)
        : base(serviceName, groupName)
    {
    }

    /// <summary>
    /// 构建服务实例注册 Redo 数据
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="instance">实例信息</param>
    /// <returns>新的 InstanceRedoData</returns>
    public static InstanceRedoData Build(string serviceName, string groupName, Instance instance)
    {
        var result = new InstanceRedoData(serviceName, groupName);
        result.Set(instance);
        return result;
    }
}
