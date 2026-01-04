namespace RedNb.Nacos.Common.Failover;

/// <summary>
/// 本地文件服务快照实现
/// </summary>
public sealed class LocalFileServiceSnapshot : IServiceSnapshot
{
    private readonly NacosOptions _options;
    private readonly ILogger<LocalFileServiceSnapshot> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public LocalFileServiceSnapshot(
        IOptions<NacosOptions> options,
        ILogger<LocalFileServiceSnapshot> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveSnapshotAsync(
        string serviceName,
        string groupName,
        string tenant,
        ServiceInfo serviceInfo,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Config.EnableSnapshot)
        {
            return;
        }

        var filePath = GetSnapshotFilePath(serviceName, groupName, tenant);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var snapshot = new ServiceSnapshotData
            {
                ServiceName = serviceName,
                GroupName = groupName,
                Tenant = tenant,
                ServiceInfo = serviceInfo,
                LastModified = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogDebug("保存服务快照: {ServiceName}, {GroupName}", serviceName, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存服务快照失败: {ServiceName}, {GroupName}", serviceName, groupName);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ServiceInfo?> GetSnapshotAsync(
        string serviceName,
        string groupName,
        string tenant,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Config.EnableSnapshot)
        {
            return null;
        }

        var filePath = GetSnapshotFilePath(serviceName, groupName, tenant);

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var snapshot = JsonSerializer.Deserialize<ServiceSnapshotData>(json, JsonOptions);

            _logger.LogDebug("读取服务快照: {ServiceName}, {GroupName}", serviceName, groupName);
            return snapshot?.ServiceInfo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取服务快照失败: {ServiceName}, {GroupName}", serviceName, groupName);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task DeleteSnapshotAsync(
        string serviceName,
        string groupName,
        string tenant,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetSnapshotFilePath(serviceName, groupName, tenant);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("删除服务快照: {ServiceName}, {GroupName}", serviceName, groupName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "删除服务快照失败: {ServiceName}, {GroupName}", serviceName, groupName);
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetSnapshotFilePath(string serviceName, string groupName, string tenant)
    {
        // 使用命名服务专用的快照路径
        var basePath = Path.Combine(_options.Config.SnapshotPath, "naming");
        var safeServiceName = SanitizeFileName(serviceName);
        var safeGroupName = SanitizeFileName(groupName);
        var safeTenant = string.IsNullOrEmpty(tenant) ? "public" : SanitizeFileName(tenant);

        return Path.Combine(basePath, safeTenant, safeGroupName, $"{safeServiceName}.json");
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
}

/// <summary>
/// 服务快照数据
/// </summary>
internal sealed class ServiceSnapshotData
{
    public string ServiceName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Tenant { get; set; } = string.Empty;
    public ServiceInfo? ServiceInfo { get; set; }
    public DateTime LastModified { get; set; }
}
