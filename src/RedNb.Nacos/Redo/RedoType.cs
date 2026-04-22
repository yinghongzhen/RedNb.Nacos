namespace RedNb.Nacos.Redo;

/// <summary>
/// Redo 操作类型枚举
/// </summary>
public enum RedoType
{
    /// <summary>
    /// 无操作
    /// </summary>
    None,

    /// <summary>
    /// 注册操作
    /// </summary>
    Register,

    /// <summary>
    /// 注销操作
    /// </summary>
    Unregister,

    /// <summary>
    /// 移除操作
    /// </summary>
    Remove
}
