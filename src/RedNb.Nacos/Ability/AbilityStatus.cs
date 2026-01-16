namespace RedNb.Nacos.Ability;

/// <summary>
/// 能力状态
/// </summary>
public class AbilityStatus
{
    /// <summary>
    /// 能力键
    /// </summary>
    public AbilityKey Key { get; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; private set; }

    /// <summary>
    /// 是否已协商（与服务端确认）
    /// </summary>
    public bool Negotiated { get; private set; }

    /// <summary>
    /// 服务端是否支持
    /// </summary>
    public bool ServerSupported { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public AbilityStatus(AbilityKey key, bool enabled = true)
    {
        Key = key;
        Enabled = enabled;
    }

    /// <summary>
    /// 设置启用状态
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
    }

    /// <summary>
    /// 标记为已协商
    /// </summary>
    public void MarkNegotiated(bool serverSupported)
    {
        Negotiated = true;
        ServerSupported = serverSupported;
    }

    /// <summary>
    /// 重置协商状态
    /// </summary>
    public void ResetNegotiation()
    {
        Negotiated = false;
        ServerSupported = false;
    }

    /// <summary>
    /// 判断能力是否可用（客户端启用且服务端支持）
    /// </summary>
    public bool IsAvailable => Enabled && (!Negotiated || ServerSupported);

    public override string ToString()
    {
        return $"{Key.GetKeyName()}: Enabled={Enabled}, Negotiated={Negotiated}, ServerSupported={ServerSupported}";
    }
}
