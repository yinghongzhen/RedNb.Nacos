using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Failover;

/// <summary>
/// 命名服务故障转移数据
/// 参考 Java SDK: com.alibaba.nacos.client.naming.backups.NamingFailoverData
/// </summary>
public class NamingFailoverData : FailoverData<ServiceInfo>
{
    /// <summary>
    /// 私有构造函数
    /// </summary>
    private NamingFailoverData(string key, ServiceInfo serviceInfo)
        : base(FailoverDataType.Naming, key, serviceInfo)
    {
    }

    /// <summary>
    /// 创建命名服务故障转移数据
    /// </summary>
    /// <param name="serviceInfo">服务信息</param>
    /// <returns>新的 NamingFailoverData</returns>
    public static NamingFailoverData NewNamingFailoverData(ServiceInfo serviceInfo)
    {
        return new NamingFailoverData(serviceInfo.Key, serviceInfo);
    }

    /// <summary>
    /// 从 key 和服务信息创建命名服务故障转移数据
    /// </summary>
    /// <param name="key">数据键</param>
    /// <param name="serviceInfo">服务信息</param>
    /// <returns>新的 NamingFailoverData</returns>
    public static NamingFailoverData NewNamingFailoverData(string key, ServiceInfo serviceInfo)
    {
        return new NamingFailoverData(key, serviceInfo);
    }
}
