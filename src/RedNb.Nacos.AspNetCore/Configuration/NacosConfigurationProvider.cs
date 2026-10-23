using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Config;
using RedNb.Nacos.Grpc.Config;

namespace RedNb.Nacos.AspNetCore.Configuration;

/// <summary>
/// Nacos configuration provider for Microsoft.Extensions.Configuration.
/// Supports dynamic reload when configuration changes in Nacos.
/// </summary>
public class NacosConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly NacosConfigurationSource _source;
    private readonly IConfigService _configService;
    private readonly Dictionary<(string DataId, string Group), ConfigChangeListener> _listeners = new();
    private readonly ILogger? _logger;
    private bool _disposed;
    private bool _loaded;
    private readonly object _loadLock = new();
    private readonly Dictionary<(string DataId, string Group), long> _revisions = new();
    private readonly object _dataLock = new();
    private readonly Dictionary<(string DataId, string Group), Dictionary<string, string?>> _sourceData = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NacosConfigurationProvider"/> class.
    /// </summary>
    /// <param name="source">The configuration source.</param>
    public NacosConfigurationProvider(NacosConfigurationSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _logger = source.LoggerFactory?.CreateLogger<NacosConfigurationProvider>();
        _source.Options.Validate();
        _configService = new NacosGrpcConfigService(_source.Options, null);
    }

    /// <inheritdoc />
    public override void Load()
    {
        lock (_loadLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var previousListeners = _listeners.Keys.ToHashSet();
            try { LoadAsync().GetAwaiter().GetResult(); _loaded = true; }
            catch
            {
                if (!_loaded)
                {
                    try { Dispose(); }
                    catch (Exception ex) { _logger?.LogWarning(ex, "Failed to dispose configuration client after initialization failure"); }
                }
                else
                    foreach (var key in _listeners.Keys.Except(previousListeners).ToArray())
                    {
                        try { _configService.RemoveListener(key.DataId, key.Group, _listeners[key]); }
                        catch (Exception ex) { _logger?.LogWarning(ex, "Failed to remove listener after failed reload"); }
                        _listeners.Remove(key);
                    }
                throw;
            }
        }
    }

    private async Task LoadAsync()
    {
        // Stage all sources. A failed required source must not partly overwrite the last good snapshot.
        var pending = new Dictionary<(string DataId, string Group), (Dictionary<string, string?> Data, long Revision)>();
        foreach (var item in _source.ConfigItems)
        {
            var key = (item.DataId, item.Group);
            long revision;
            lock (_dataLock) revision = _revisions.GetValueOrDefault(key);
            try
            {
                var config = await _configService.GetConfigAsync(item.DataId, item.Group, _source.Options.DefaultTimeout).ConfigureAwait(false);
                var itemData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                if (string.IsNullOrEmpty(config))
                {
                    if (!item.Optional && !_source.Optional)
                        throw new InvalidOperationException($"Configuration not found: DataId={item.DataId}, Group={item.Group}");
                }
                else ParseConfiguration(itemData, config, item.ConfigType);

                // Listen even for an absent optional item so that later creation can be observed.
                if (_source.ReloadOnChange) await SetupListenerAsync(item).ConfigureAwait(false);
                pending[key] = (itemData, revision);
            }
            catch (Exception ex) when (item.Optional || _source.Optional)
            {
                // A fetch/parse error preserves the last good value. A confirmed absence stages an empty source.
                _logger?.LogWarning(ex, "Failed to load optional configuration: DataId={DataId}, Group={Group}", item.DataId, item.Group);
            }
        }
        lock (_dataLock)
        {
            foreach (var entry in pending)
                if (_revisions.GetValueOrDefault(entry.Key) == entry.Value.Revision)
                    _sourceData[entry.Key] = entry.Value.Data;
            RebuildData();
        }
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
        using var doc = JsonDocument.Parse(json);
        ParseJsonElement(data, doc.RootElement, string.Empty);
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
        var key = (item.DataId, item.Group);
        if (_listeners.ContainsKey(key))
            return;

        var listener = new ConfigChangeListener(item.ConfigType, newConfig =>
        {
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            try { if (newConfig != null) ParseConfiguration(data, newConfig, item.ConfigType); }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Rejected invalid configuration update: DataId={DataId}, Group={Group}", item.DataId, item.Group);
                return;
            }
            lock (_dataLock)
            {
                if (_disposed) return;
                _sourceData[key] = data;
                _revisions[key] = _revisions.GetValueOrDefault(key) + 1;
                RebuildData();
            }

            OnReload();
        });

        _listeners[key] = listener;
        await _configService.AddListenerAsync(item.DataId, item.Group, listener).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_loadLock)
        {
            lock (_dataLock)
            {
                if (_disposed) return;
                _disposed = true;
            }
            foreach (var entry in _listeners)
            {
                try { _configService.RemoveListener(entry.Key.DataId, entry.Key.Group, entry.Value); }
                catch (Exception ex) { _logger?.LogWarning(ex, "Failed to remove configuration listener during disposal"); }
            }
            _listeners.Clear();
            if (_configService is IAsyncDisposable asyncDisposable)
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            else if (_configService is IDisposable disposable) disposable.Dispose();
        }
    }

    private class ConfigChangeListener : IConfigChangeListener
    {
        private readonly string _configType;
        private readonly Action<string?> _onChange;

        public ConfigChangeListener(string configType, Action<string?> onChange)
        {
            _configType = configType;
            _onChange = onChange;
        }

        public void OnReceiveConfigInfo(ConfigInfo configInfo)
        {
            _onChange(configInfo.Content);
        }
    }

    private void RebuildData()
    {
        lock (_dataLock)
        {
            var merged = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in _source.ConfigItems)
                if (_sourceData.TryGetValue((item.DataId, item.Group), out var values))
                    foreach (var pair in values) merged[pair.Key] = pair.Value;
            Data = merged;
        }
    }
}
