using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RedNb.Nacos.Failover;

/// <summary>
/// 基于本地磁盘的故障转移数据源
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class LocalDiskFailoverDataSource<T> : IFailoverDataSource<T> where T : class
{
    private readonly ILogger _logger;
    private readonly string _cacheDir;
    private readonly string _switchFileName;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="cacheDir">缓存目录</param>
    /// <param name="switchFileName">开关文件名</param>
    public LocalDiskFailoverDataSource(ILogger logger, string cacheDir, string switchFileName = "failover-switch")
    {
        _logger = logger;
        _cacheDir = cacheDir;
        _switchFileName = switchFileName;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        EnsureDirectoryExists();
    }

    /// <summary>
    /// 获取故障转移开关
    /// </summary>
    public FailoverSwitch GetSwitch()
    {
        try
        {
            var switchFile = Path.Combine(_cacheDir, _switchFileName);
            if (!File.Exists(switchFile))
            {
                return FailoverSwitch.CreateDisabled();
            }

            var content = File.ReadAllText(switchFile).Trim();
            return new FailoverSwitch
            {
                Enabled = content.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                          content.Equals("true", StringComparison.OrdinalIgnoreCase)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read failover switch, using disabled");
            return FailoverSwitch.CreateDisabled();
        }
    }

    /// <summary>
    /// 获取故障转移数据
    /// </summary>
    public Dictionary<string, FailoverData<T>> GetFailoverData()
    {
        var result = new Dictionary<string, FailoverData<T>>();

        try
        {
            if (!Directory.Exists(_cacheDir))
            {
                return result;
            }

            var files = Directory.GetFiles(_cacheDir, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName == _switchFileName)
                    {
                        continue;
                    }

                    var content = File.ReadAllText(file);
                    var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                    if (data != null)
                    {
                        var key = DecodeKey(fileName);
                        result[key] = new FailoverData<T>(FailoverDataType.Naming, key, data);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read failover data from file: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failover data from directory: {Dir}", _cacheDir);
        }

        return result;
    }

    /// <summary>
    /// 保存故障转移数据到磁盘
    /// </summary>
    public void SaveFailoverData(string key, T data)
    {
        try
        {
            EnsureDirectoryExists();
            var fileName = EncodeKey(key) + ".json";
            var filePath = Path.Combine(_cacheDir, fileName);
            var content = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(filePath, content);
            _logger.LogDebug("Saved failover data to: {File}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save failover data for key: {Key}", key);
        }
    }

    /// <summary>
    /// 删除故障转移数据
    /// </summary>
    public void DeleteFailoverData(string key)
    {
        try
        {
            var fileName = EncodeKey(key) + ".json";
            var filePath = Path.Combine(_cacheDir, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted failover data: {File}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete failover data for key: {Key}", key);
        }
    }

    /// <summary>
    /// 设置故障转移开关
    /// </summary>
    public void SetSwitch(bool enabled)
    {
        try
        {
            EnsureDirectoryExists();
            var switchFile = Path.Combine(_cacheDir, _switchFileName);
            File.WriteAllText(switchFile, enabled ? "1" : "0");
            _logger.LogInformation("Set failover switch to: {Enabled}", enabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set failover switch");
        }
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }
    }

    /// <summary>
    /// 编码键名（用于文件名）
    /// </summary>
    private static string EncodeKey(string key)
    {
        // 将特殊字符替换为安全字符
        return key.Replace("@@", "__").Replace("/", "_").Replace("\\", "_");
    }

    /// <summary>
    /// 解码键名
    /// </summary>
    private static string DecodeKey(string encodedKey)
    {
        return encodedKey.Replace("__", "@@");
    }
}
