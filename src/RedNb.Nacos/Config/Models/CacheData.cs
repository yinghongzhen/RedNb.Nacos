namespace RedNb.Nacos.Config.Models;

/// <summary>
/// 配置项缓存
/// </summary>
internal sealed class CacheData
{
    /// <summary>
    /// 配置 ID
    /// </summary>
    public required string DataId { get; set; }

    /// <summary>
    /// 分组名称
    /// </summary>
    public required string Group { get; set; }

    /// <summary>
    /// 命名空间
    /// </summary>
    public required string Namespace { get; set; }

    /// <summary>
    /// 配置内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 配置 MD5
    /// </summary>
    public string Md5 { get; set; } = string.Empty;

    /// <summary>
    /// 配置类型
    /// </summary>
    public string? ConfigType { get; set; }

    /// <summary>
    /// 监听器列表
    /// </summary>
    public List<IConfigListener> Listeners { get; } = new();

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 生成缓存 Key
    /// </summary>
    public string GetCacheKey() => $"{Namespace}+{Group}+{DataId}";

    /// <summary>
    /// 更新内容并检查是否变更
    /// </summary>
    public bool UpdateContent(string? newContent)
    {
        var newMd5 = Md5Utils.GetConfigMd5(newContent);
        if (Md5 == newMd5)
        {
            return false;
        }

        Content = newContent;
        Md5 = newMd5;
        LastModified = DateTimeOffset.UtcNow;
        return true;
    }
}
