namespace RedNb.Nacos.Failover;

/// <summary>
/// 故障转移数据源接口
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public interface IFailoverDataSource<T> where T : class
{
    /// <summary>
    /// 获取故障转移开关
    /// </summary>
    FailoverSwitch GetSwitch();

    /// <summary>
    /// 获取故障转移数据
    /// </summary>
    /// <returns>故障转移数据字典，key为数据标识，value为数据实体</returns>
    Dictionary<string, FailoverData<T>> GetFailoverData();
}
