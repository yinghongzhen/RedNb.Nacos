using System.Text.Json;
using RedNb.Nacos.Configuration.Parsers;

namespace RedNb.Nacos.Configuration;

/// <summary>
/// Nacos 配置源
/// </summary>
public class NacosConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Nacos 配置选项
    /// </summary>
    public required NacosOptions NacosOptions { get; set; }

    /// <summary>
    /// 配置文件列表
    /// </summary>
    public List<NacosConfigItem> ConfigItems { get; set; } = [];

    /// <summary>
    /// 是否可选（如果配置不存在是否抛出异常）
    /// </summary>
    public bool Optional { get; set; } = false;

    /// <summary>
    /// 配置文件解析器
    /// </summary>
    public IConfigurationParser? Parser { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new NacosConfigurationProvider(this);
    }
}

/// <summary>
/// Nacos 配置项
/// </summary>
public class NacosConfigItem
{
    /// <summary>
    /// 数据 ID
    /// </summary>
    public required string DataId { get; set; }

    /// <summary>
    /// 分组（默认 DEFAULT_GROUP）
    /// </summary>
    public string Group { get; set; } = NacosConstants.DefaultGroup;

    /// <summary>
    /// 是否可选
    /// </summary>
    public bool Optional { get; set; } = false;
}

/// <summary>
/// Nacos 配置提供程序
/// </summary>
public class NacosConfigurationProvider : ConfigurationProvider, IAsyncDisposable
{
    private readonly NacosConfigurationSource _source;
    private readonly IConfigurationParser _parser;
    private readonly Dictionary<string, ConfigListenerHandler> _listeners = new();
    private INacosConfigService? _configService;
    private bool _disposed;

    public NacosConfigurationProvider(NacosConfigurationSource source)
    {
        _source = source;
        _parser = source.Parser ?? new JsonConfigurationParser();
    }

    public override void Load()
    {
        // 同步加载使用 GetAwaiter().GetResult()
        LoadAsync().GetAwaiter().GetResult();
    }

    private async Task LoadAsync()
    {
        // 创建临时 ServiceProvider 来获取配置服务
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRedNbNacos(options =>
        {
            options.ServerAddresses = _source.NacosOptions.ServerAddresses;
            options.Namespace = _source.NacosOptions.Namespace;
            options.UserName = _source.NacosOptions.UserName;
            options.Password = _source.NacosOptions.Password;
            options.Auth = _source.NacosOptions.Auth;
        });

        var sp = services.BuildServiceProvider();
        _configService = sp.GetRequiredService<INacosConfigService>();

        var data = new Dictionary<string, string?>();

        foreach (var item in _source.ConfigItems)
        {
            try
            {
                var content = await _configService.GetConfigAsync(
                    item.DataId,
                    item.Group);

                if (!string.IsNullOrEmpty(content))
                {
                    var parsed = _parser.Parse(content);
                    foreach (var kv in parsed)
                    {
                        data[kv.Key] = kv.Value;
                    }
                }

                // 添加监听器
                var listener = new ConfigListenerHandler(
                    item.DataId,
                    item.Group,
                    OnConfigChanged);

                await _configService.AddListenerAsync(item.DataId, item.Group, listener);
                _listeners[$"{item.Group}@@{item.DataId}"] = listener;
            }
            catch (Exception ex)
            {
                if (!item.Optional && !_source.Optional)
                {
                    throw new InvalidOperationException(
                        $"无法从 Nacos 加载配置: dataId={item.DataId}, group={item.Group}",
                        ex);
                }
            }
        }

        Data = data;
    }

    private void OnConfigChanged(ConfigChangedEventArgs args)
    {
        // 解析新配置
        var data = new Dictionary<string, string?>(Data, StringComparer.OrdinalIgnoreCase);

        try
        {
            // 移除旧配置（该数据源的）
            // 注意：这里简化处理，实际应该只移除对应 dataId 的配置
            data.Clear();

            // 重新加载所有配置
            foreach (var item in _source.ConfigItems)
            {
                if (_configService == null) continue;

                var content = _configService.GetConfigAsync(item.DataId, item.Group)
                    .GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(content))
                {
                    var parsed = _parser.Parse(content);
                    foreach (var kv in parsed)
                    {
                        data[kv.Key] = kv.Value;
                    }
                }
            }

            Data = data;
            OnReload();
        }
        catch (Exception)
        {
            // 配置更新失败时保留旧配置
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_configService != null)
        {
            foreach (var (key, listener) in _listeners)
            {
                var parts = key.Split("@@");
                if (parts.Length == 2)
                {
                    try
                    {
                        await _configService.RemoveListenerAsync(parts[1], parts[0], listener);
                    }
                    catch
                    {
                        // 忽略清理错误
                    }
                }
            }
        }

        _listeners.Clear();
    }
}

/// <summary>
/// 配置监听处理器
/// </summary>
internal class ConfigListenerHandler : IConfigListener
{
    private readonly string _dataId;
    private readonly string _group;
    private readonly Action<ConfigChangedEventArgs> _callback;

    public ConfigListenerHandler(
        string dataId,
        string group,
        Action<ConfigChangedEventArgs> callback)
    {
        _dataId = dataId;
        _group = group;
        _callback = callback;
    }

    public Task ReceiveConfigInfoAsync(ConfigChangedEventArgs configInfo)
    {
        _callback(configInfo);
        return Task.CompletedTask;
    }
}
