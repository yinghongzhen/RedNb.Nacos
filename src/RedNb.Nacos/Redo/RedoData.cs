namespace RedNb.Nacos.Redo;

/// <summary>
/// Redo 数据实体，用于跟踪需要重做的操作状态
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class RedoData<T> where T : class
{
    /// <summary>
    /// 获取或设置数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 获取或设置命名空间
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置分组
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置服务名
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 是否已注册
    /// </summary>
    public bool Registered { get; private set; }

    /// <summary>
    /// 是否正在注销
    /// </summary>
    public bool Unregistering { get; private set; }

    /// <summary>
    /// 是否期望已注册状态
    /// </summary>
    public bool ExpectedRegistered { get; private set; } = true;

    /// <summary>
    /// 获取 redo key
    /// </summary>
    public string RedoKey => $"{Namespace}@@{Group}@@{ServiceName}";

    /// <summary>
    /// 私有构造函数
    /// </summary>
    private RedoData(string ns, string group, string serviceName)
    {
        Namespace = ns;
        Group = group;
        ServiceName = serviceName;
    }

    /// <summary>
    /// 创建注册 redo 数据
    /// </summary>
    public static RedoData<T> CreateForRegister(string ns, string group, string serviceName, T? data = null)
    {
        return new RedoData<T>(ns, group, serviceName)
        {
            Data = data,
            ExpectedRegistered = true
        };
    }

    /// <summary>
    /// 创建注销 redo 数据
    /// </summary>
    public static RedoData<T> CreateForUnregister(string ns, string group, string serviceName, T? data = null)
    {
        var result = new RedoData<T>(ns, group, serviceName)
        {
            Data = data,
            Registered = true
        };
        result.SetUnregistering();
        return result;
    }

    /// <summary>
    /// 标记为已注册
    /// </summary>
    public void SetRegistered()
    {
        Registered = true;
    }

    /// <summary>
    /// 标记为未注册
    /// </summary>
    public void SetUnregistered()
    {
        Registered = false;
    }

    /// <summary>
    /// 设置正在注销状态
    /// </summary>
    public void SetUnregistering()
    {
        ExpectedRegistered = false;
        Unregistering = true;
    }

    /// <summary>
    /// 获取 redo 类型
    /// </summary>
    public RedoType GetRedoType()
    {
        if (ExpectedRegistered && !Registered)
        {
            return RedoType.Register;
        }

        if (!ExpectedRegistered && Registered && Unregistering)
        {
            return RedoType.Unregister;
        }

        if (!ExpectedRegistered && !Registered && !Unregistering)
        {
            return RedoType.Remove;
        }

        return RedoType.None;
    }

    /// <summary>
    /// 判断是否需要 redo
    /// </summary>
    public bool IsNeedRedo()
    {
        return GetRedoType() != RedoType.None;
    }
}
