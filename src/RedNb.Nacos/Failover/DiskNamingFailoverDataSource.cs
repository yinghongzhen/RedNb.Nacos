using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Failover;

/// <summary>
/// 基于磁盘的命名服务故障转移数据源
/// 参考 Java SDK: com.alibaba.nacos.client.naming.backups.datasource.DiskFailoverDataSource
/// </summary>
public class DiskNamingFailoverDataSource : IFailoverDataSource<ServiceInfo>
{
    private const string FailoverDir = "failover";
    private const string IsFailoverMode = "1";
    private const string NoFailoverMode = "0";
    private const string FailoverModeParam = "failover-mode";
    private const string FailoverSwitchFileName = "00-00---000-NACOS_SWITCH_DOMAIN-000---00-00";

    private static readonly FailoverSwitch FailoverSwitchFalse = FailoverSwitch.CreateDisabled();
    private static readonly FailoverSwitch FailoverSwitchTrue = FailoverSwitch.CreateEnabled();

    private readonly ILogger _logger;
    private readonly string _failoverDir;
    private readonly Dictionary<string, string> _switchParams = new();
    private Dictionary<string, FailoverData<ServiceInfo>> _serviceMap = new();
    private long _lastModifiedMillis;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="cacheDir">缓存目录</param>
    public DiskNamingFailoverDataSource(ILogger logger, string cacheDir)
    {
        _logger = logger;
        _failoverDir = Path.Combine(cacheDir, FailoverDir);
        _switchParams[FailoverModeParam] = bool.FalseString;
        EnsureDirectoryExists();
    }

    /// <summary>
    /// 获取故障转移开关
    /// </summary>
    public FailoverSwitch GetSwitch()
    {
        try
        {
            var switchFile = Path.Combine(_failoverDir, FailoverSwitchFileName);
            if (!File.Exists(switchFile))
            {
                _logger.LogDebug("failover switch is not found, {FileName}", FailoverSwitchFileName);
                _switchParams[FailoverModeParam] = bool.FalseString;
                return FailoverSwitchFalse;
            }

            var fileInfo = new FileInfo(switchFile);
            var modified = fileInfo.LastWriteTimeUtc.Ticks;

            if (_lastModifiedMillis < modified)
            {
                _lastModifiedMillis = modified;
                var failover = File.ReadAllText(switchFile, Encoding.UTF8);

                if (!string.IsNullOrEmpty(failover))
                {
                    var lines = failover.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (trimmedLine == IsFailoverMode)
                        {
                            _switchParams[FailoverModeParam] = bool.TrueString;
                            _logger.LogInformation("failover-mode is on");
                            LoadFailoverData();
                            return FailoverSwitchTrue;
                        }
                        else if (trimmedLine == NoFailoverMode)
                        {
                            _switchParams[FailoverModeParam] = bool.FalseString;
                            _logger.LogInformation("failover-mode is off");
                            return FailoverSwitchFalse;
                        }
                    }
                }
            }

            return _switchParams[FailoverModeParam] == bool.TrueString ? FailoverSwitchTrue : FailoverSwitchFalse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NA] failed to read failover switch");
            _switchParams[FailoverModeParam] = bool.FalseString;
            return FailoverSwitchFalse;
        }
    }

    /// <summary>
    /// 获取故障转移数据
    /// </summary>
    public Dictionary<string, FailoverData<ServiceInfo>> GetFailoverData()
    {
        if (_switchParams.TryGetValue(FailoverModeParam, out var mode) && mode == bool.TrueString)
        {
            return _serviceMap;
        }
        return new Dictionary<string, FailoverData<ServiceInfo>>();
    }

    /// <summary>
    /// 加载故障转移数据
    /// </summary>
    private void LoadFailoverData()
    {
        var domMap = new Dictionary<string, FailoverData<ServiceInfo>>();

        try
        {
            EnsureDirectoryExists();

            var files = Directory.GetFiles(_failoverDir);
            if (files == null || files.Length == 0)
            {
                return;
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                // 跳过非文件和开关文件
                if (fileName == FailoverSwitchFileName)
                {
                    continue;
                }

                try
                {
                    var content = File.ReadAllText(file, Encoding.UTF8);
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        continue;
                    }

                    var serviceInfo = JsonSerializer.Deserialize<ServiceInfo>(content, _jsonOptions);
                    if (serviceInfo != null)
                    {
                        var key = serviceInfo.Key;
                        domMap[key] = NamingFailoverData.NewNamingFailoverData(serviceInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[NA] failed to read cache file: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NA] failed to read cache files");
        }

        if (domMap.Count > 0)
        {
            _serviceMap = domMap;
        }
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_failoverDir))
        {
            Directory.CreateDirectory(_failoverDir);
        }
    }

    /// <summary>
    /// 保存服务信息到故障转移目录
    /// </summary>
    /// <param name="serviceInfo">服务信息</param>
    public void SaveServiceInfo(ServiceInfo serviceInfo)
    {
        try
        {
            EnsureDirectoryExists();
            var fileName = EncodeFileName(serviceInfo.Key);
            var filePath = Path.Combine(_failoverDir, fileName);
            var content = JsonSerializer.Serialize(serviceInfo, _jsonOptions);
            File.WriteAllText(filePath, content, Encoding.UTF8);
            _logger.LogDebug("Saved failover data to: {File}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save failover data for service: {ServiceName}", serviceInfo.Name);
        }
    }

    /// <summary>
    /// 设置故障转移开关
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetSwitch(bool enabled)
    {
        try
        {
            EnsureDirectoryExists();
            var switchFile = Path.Combine(_failoverDir, FailoverSwitchFileName);
            File.WriteAllText(switchFile, enabled ? IsFailoverMode : NoFailoverMode, Encoding.UTF8);
            _logger.LogInformation("Set failover switch to: {Enabled}", enabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set failover switch");
        }
    }

    /// <summary>
    /// 编码文件名（处理特殊字符）
    /// </summary>
    private static string EncodeFileName(string key)
    {
        // 将特殊字符替换为安全字符
        return key.Replace(":", "_").Replace("/", "_").Replace("\\", "_").Replace("@@", "__");
    }
}
