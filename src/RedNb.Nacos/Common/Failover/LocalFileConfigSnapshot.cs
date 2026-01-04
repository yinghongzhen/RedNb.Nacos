namespace RedNb.Nacos.Common.Failover;

/// <summary>
/// 本地文件配置快照实现
/// </summary>
public sealed class LocalFileConfigSnapshot : IConfigSnapshot
{
    private readonly NacosOptions _options;
    private readonly ILogger<LocalFileConfigSnapshot> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public LocalFileConfigSnapshot(
        IOptions<NacosOptions> options,
        ILogger<LocalFileConfigSnapshot> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveSnapshotAsync(
        string dataId,
        string group,
        string tenant,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Config.EnableSnapshot)
        {
            return;
        }

        var filePath = GetSnapshotFilePath(dataId, group, tenant);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var snapshot = new ConfigSnapshotData
            {
                DataId = dataId,
                Group = group,
                Tenant = tenant,
                Content = content,
                Md5 = ComputeMd5(content),
                LastModified = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogDebug("保存配置快照: {DataId}, {Group}", dataId, group);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存配置快照失败: {DataId}, {Group}", dataId, group);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetSnapshotAsync(
        string dataId,
        string group,
        string tenant,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Config.EnableSnapshot)
        {
            return null;
        }

        var filePath = GetSnapshotFilePath(dataId, group, tenant);

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var snapshot = JsonSerializer.Deserialize<ConfigSnapshotData>(json, JsonOptions);

            _logger.LogDebug("读取配置快照: {DataId}, {Group}", dataId, group);
            return snapshot?.Content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取配置快照失败: {DataId}, {Group}", dataId, group);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task DeleteSnapshotAsync(
        string dataId,
        string group,
        string tenant,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetSnapshotFilePath(dataId, group, tenant);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("删除配置快照: {DataId}, {Group}", dataId, group);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "删除配置快照失败: {DataId}, {Group}", dataId, group);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetMd5Async(
        string dataId,
        string group,
        string tenant,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Config.EnableSnapshot)
        {
            return null;
        }

        var filePath = GetSnapshotFilePath(dataId, group, tenant);

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var snapshot = JsonSerializer.Deserialize<ConfigSnapshotData>(json, JsonOptions);
            return snapshot?.Md5;
        }
        catch
        {
            return null;
        }
    }

    private string GetSnapshotFilePath(string dataId, string group, string tenant)
    {
        var basePath = _options.Config.SnapshotPath;
        var safeDataId = SanitizeFileName(dataId);
        var safeGroup = SanitizeFileName(group);
        var safeTenant = string.IsNullOrEmpty(tenant) ? "public" : SanitizeFileName(tenant);

        return Path.Combine(basePath, safeTenant, safeGroup, $"{safeDataId}.json");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(fileName);

        foreach (var c in invalidChars)
        {
            sanitized.Replace(c, '_');
        }

        return sanitized.ToString();
    }

    private static string ComputeMd5(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// 配置快照数据
/// </summary>
internal sealed class ConfigSnapshotData
{
    public string DataId { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Tenant { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Md5 { get; set; }
    public DateTime LastModified { get; set; }
}
