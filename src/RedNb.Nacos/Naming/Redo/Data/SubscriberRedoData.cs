namespace RedNb.Nacos.Naming.Redo.Data;

/// <summary>
/// 订阅者 Redo 数据
/// 参考 Java SDK: com.alibaba.nacos.client.naming.remote.gprc.redo.data.SubscriberRedoData
/// </summary>
public class SubscriberRedoData : NamingRedoData<string>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    private SubscriberRedoData(string serviceName, string groupName)
        : base(serviceName, groupName)
    {
    }

    /// <summary>
    /// 构建订阅者 Redo 数据
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="groupName">分组名称</param>
    /// <param name="clusters">集群信息</param>
    /// <returns>新的 SubscriberRedoData</returns>
    public static SubscriberRedoData Build(string serviceName, string groupName, string clusters)
    {
        var result = new SubscriberRedoData(serviceName, groupName);
        result.Set(clusters);
        return result;
    }
}
