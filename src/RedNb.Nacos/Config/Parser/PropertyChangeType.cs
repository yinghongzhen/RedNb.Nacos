namespace RedNb.Nacos.Config.Parser;

/// <summary>
/// 配置属性变更类型
/// </summary>
public enum PropertyChangeType
{
    /// <summary>
    /// 新增属性
    /// </summary>
    Added,

    /// <summary>
    /// 修改属性
    /// </summary>
    Modified,

    /// <summary>
    /// 删除属性
    /// </summary>
    Deleted
}
