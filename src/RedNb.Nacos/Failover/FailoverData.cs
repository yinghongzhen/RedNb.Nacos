namespace RedNb.Nacos.Failover;

/// <summary>
/// 故障转移数据实体
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class FailoverData<T> where T : class
{
    /// <summary>
    /// 数据类型
    /// </summary>
    public FailoverDataType DataType { get; }

    /// <summary>
    /// 数据内容
    /// </summary>
    public T Data { get; }

    /// <summary>
    /// 数据键
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public FailoverData(FailoverDataType dataType, string key, T data)
    {
        DataType = dataType;
        Key = key;
        Data = data;
    }

    /// <summary>
    /// 创建命名服务故障转移数据
    /// </summary>
    public static FailoverData<T> CreateForNaming(string key, T data)
    {
        return new FailoverData<T>(FailoverDataType.Naming, key, data);
    }

    /// <summary>
    /// 创建配置服务故障转移数据
    /// </summary>
    public static FailoverData<T> CreateForConfig(string key, T data)
    {
        return new FailoverData<T>(FailoverDataType.Config, key, data);
    }
}
