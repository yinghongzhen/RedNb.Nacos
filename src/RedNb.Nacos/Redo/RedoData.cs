namespace RedNb.Nacos.Redo;

/// <summary>
/// Nacos 客户端 Redo 数据基类
/// 参考 Java SDK: com.alibaba.nacos.client.redo.data.RedoData
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public abstract class RedoData<T>
{
    /// <summary>
    /// 期望的最终状态
    /// true: 期望最终注册到服务器
    /// false: 期望从服务器注销
    /// </summary>
    public bool ExpectedRegistered { get; set; } = true;

    /// <summary>
    /// 是否已注册到服务器
    /// </summary>
    public bool Registered { get; set; }

    /// <summary>
    /// 是否正在注销中
    /// </summary>
    public bool Unregistering { get; set; }

    /// <summary>
    /// 数据
    /// </summary>
    public T? Data { get; private set; }

    /// <summary>
    /// 获取数据
    /// </summary>
    public T? Get() => Data;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void Set(T? data) => Data = data;

    /// <summary>
    /// 标记为已注册
    /// </summary>
    public void SetRegistered()
    {
        Registered = true;
        Unregistering = false;
    }

    /// <summary>
    /// 标记为已注销
    /// </summary>
    public void SetUnregistered()
    {
        Registered = false;
        Unregistering = true;
    }

    /// <summary>
    /// 获取 Redo 类型 (不考虑期望状态)
    /// <para>
    /// 状态说明:
    /// registered=true, unregistering=false: 已注册，不需要操作;
    /// registered=true, unregistering=true: 已注册，需要注销;
    /// registered=false, unregistering=false: 未注册，需要重新注册;
    /// registered=false, unregistering=true: 未注册且不再继续注册
    /// </para>
    /// </summary>
    public RedoType GetRedoType()
    {
        if (Registered && !Unregistering)
        {
            return ExpectedRegistered ? RedoType.None : RedoType.Unregister;
        }
        else if (Registered && Unregistering)
        {
            return RedoType.Unregister;
        }
        else if (!Registered && !Unregistering)
        {
            return RedoType.Register;
        }
        else
        {
            // !Registered && Unregistering
            return ExpectedRegistered ? RedoType.Register : RedoType.Remove;
        }
    }

    /// <summary>
    /// 判断是否需要 Redo
    /// </summary>
    public bool IsNeedRedo() => GetRedoType() != RedoType.None;

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (this == obj) return true;
        if (obj == null || GetType() != obj.GetType()) return false;
        var redoData = (RedoData<T>)obj;
        return Registered == redoData.Registered &&
               Unregistering == redoData.Unregistering &&
               Equals(Data, redoData.Data);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Registered, Unregistering, Data);
    }
}
