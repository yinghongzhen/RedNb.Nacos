using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core.Config;
using RedNb.Nacos.Client.Config;

namespace RedNb.Nacos.AspNetCore.Configuration;

/// <summary>
/// Nacos configuration provider for Microsoft.Extensions.Configuration.
/// Supports dynamic reload when configuration changes in Nacos.
/// </summary>
public class NacosConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly NacosConfigurationSource _source;
    private readonly IConfigService _configService;
    private readonly Dictionary<string, ConfigChangeListener> _listeners = new();
    private readonly ILogger? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NacosConfigurationProvider"/> class.
    /// </summary>
    /// <param name="source">The configuration source.</param>
    public NacosConfigurationProvider(NacosConfigurationSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _configService = new NacosConfigService(_source.Options, null);
    }

    /// <inheritdoc />
    public override void Load()
    {
        LoadAsync().GetAwaiter().GetResult();
    }

    private async Task LoadAsync()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in _source.ConfigItems)
        {
            try
            {
                var config = await _configService.GetConfigAsync(item.DataId, item.Group, _source.Options.DefaultTimeout);

                if (string.IsNullOrEmpty(config))
                {
                    if (!item.Optional && !_source.Optional)
                    {
                        throw new InvalidOperationException(
                            $"Configuration not found: DataId={item.DataId}, Group={item.Group}");
                    }
                    continue;
                }

                ParseConfiguration(data, config, item.ConfigType);

                // Setup listener for reload if enabled
                if (_source.ReloadOnChange)
                {
                    await SetupListenerAsync(item);
                }
            }
            catch (Exception ex) when (item.Optional || _source.Optional)
            {
                _logger?.LogWarning(ex, "Failed to load optional configuration: DataId={DataId}, Group={Group}",
                    item.DataId, item.Group);
            }
        }

        Data = data;
    }

    private void ParseConfiguration(Dictionary<string, string?> data, string content, string configType)
    {
        switch (configType.ToLowerInvariant())
        {
            case "json":
                ParseJsonConfiguration(data, content);
                break;
            case "properties":
            case "text":
                ParsePropertiesConfiguration(data, content);
                break;
            default:
                // For other types, store as raw content
                data["RawContent"] = content;
                break;
        }
    }

    private void ParseJsonConfiguration(Dictionary<string, string?> data, string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            ParseJsonElement(data, doc.RootElement, string.Empty);
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "Failed to parse JSON configuration");
        }
    }

    private void ParseJsonElement(Dictionary<string, string?> data, JsonElement element, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
                    ParseJsonElement(data, property.Value, key);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}:{index}";
                    ParseJsonElement(data, item, key);
                    index++;
                }
                break;

            default:
                data[prefix] = element.ToString();
                break;
        }
    }

    private void ParsePropertiesConfiguration(Dictionary<string, string?> data, string content)
    {
        foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#') || trimmedLine.StartsWith("//"))
                continue;

            var separatorIndex = trimmedLine.IndexOf('=');
            if (separatorIndex > 0)
            {
                var key = trimmedLine[..separatorIndex].Trim().Replace(".", ":");
                var value = trimmedLine[(separatorIndex + 1)..].Trim();
                data[key] = value;
            }
        }
    }

    private async Task SetupListenerAsync(NacosConfigurationItem item)
    {
        var key = $"{item.DataId}@@{item.Group}";
        if (_listeners.ContainsKey(key))
            return;

        var listener = new ConfigChangeListener(item.ConfigType, newConfig =>
        {
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            ParseConfiguration(data, newConfig, item.ConfigType);

            // Merge with existing data
            foreach (var kvp in data)
            {
                Data[kvp.Key] = kvp.Value;
            }

            OnReload();
        });

        await _configService.AddListenerAsync(item.DataId, item.Group, listener);
        _listeners[key] = listener;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var kvp in _listeners)
        {
            var parts = kvp.Key.Split("@@");
            if (parts.Length == 2)
            {
                _configService.RemoveListener(parts[0], parts[1], kvp.Value);
            }
        }

        _listeners.Clear();

        if (_configService is IAsyncDisposable asyncDisposable)
        {
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        else if (_configService is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }

    private class ConfigChangeListener : IConfigChangeListener
    {
        private readonly string _configType;
        private readonly Action<string> _onChange;

        public ConfigChangeListener(string configType, Action<string> onChange)
        {
            _configType = configType;
            _onChange = onChange;
        }

        public void OnReceiveConfigInfo(ConfigInfo configInfo)
        {
            if (configInfo.Content != null)
            {
                _onChange(configInfo.Content);
            }
        }
    }
}
