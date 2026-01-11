namespace RedNb.Nacos.Core.Config.FuzzyWatch;

/// <summary>
/// Config change type for fuzzy watch.
/// </summary>
public static class ConfigChangedType
{
    /// <summary>
    /// Configuration was added.
    /// </summary>
    public const string AddConfig = "ADD_CONFIG";

    /// <summary>
    /// Configuration was deleted.
    /// </summary>
    public const string DeleteConfig = "DELETE_CONFIG";

    /// <summary>
    /// Configuration was modified.
    /// </summary>
    public const string ModifyConfig = "MODIFY_CONFIG";
}

/// <summary>
/// Sync type that triggered the fuzzy watch change.
/// </summary>
public static class FuzzyWatchSyncType
{
    /// <summary>
    /// Initial notification when watch is first established.
    /// </summary>
    public const string InitNotify = "FUZZY_WATCH_INIT_NOTIFY";

    /// <summary>
    /// Resource was changed on server.
    /// </summary>
    public const string ResourceChanged = "FUZZY_WATCH_RESOURCE_CHANGED";

    /// <summary>
    /// Diff sync notification.
    /// </summary>
    public const string DiffSyncNotify = "FUZZY_WATCH_DIFF_SYNC_NOTIFY";
}

/// <summary>
/// Represents a fuzzy watch configuration change event.
/// </summary>
public class ConfigFuzzyWatchChangeEvent
{
    /// <summary>
    /// Gets the namespace of the configuration.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Gets the group of the configuration.
    /// </summary>
    public string Group { get; }

    /// <summary>
    /// Gets the data ID of the configuration.
    /// </summary>
    public string DataId { get; }

    /// <summary>
    /// Gets the change type (ADD_CONFIG, DELETE_CONFIG, MODIFY_CONFIG).
    /// </summary>
    public string ChangedType { get; }

    /// <summary>
    /// Gets the sync type that triggered this change.
    /// </summary>
    public string SyncType { get; }

    /// <summary>
    /// Creates a new ConfigFuzzyWatchChangeEvent.
    /// </summary>
    public ConfigFuzzyWatchChangeEvent(string ns, string group, string dataId, string changedType, string syncType)
    {
        Namespace = ns;
        Group = group;
        DataId = dataId;
        ChangedType = changedType;
        SyncType = syncType;
    }

    /// <summary>
    /// Creates a new ConfigFuzzyWatchChangeEvent using factory method.
    /// </summary>
    public static ConfigFuzzyWatchChangeEvent Build(string ns, string group, string dataId, string changedType, string syncType)
    {
        return new ConfigFuzzyWatchChangeEvent(ns, group, dataId, changedType, syncType);
    }

    /// <summary>
    /// Gets the group key (dataId@@group@@namespace).
    /// </summary>
    public string GroupKey => $"{DataId}@@{Group}@@{Namespace}";

    public override string ToString()
    {
        return $"ConfigFuzzyWatchChangeEvent{{namespace='{Namespace}', group='{Group}', dataId='{DataId}', changedType='{ChangedType}', syncType='{SyncType}'}}";
    }
}
