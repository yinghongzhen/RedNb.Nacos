namespace RedNb.Nacos.Core.Naming.FuzzyWatch;

/// <summary>
/// Service change type for fuzzy watch.
/// </summary>
public static class ServiceChangedType
{
    /// <summary>
    /// Service was added.
    /// </summary>
    public const string AddService = "ADD_SERVICE";

    /// <summary>
    /// Service was deleted.
    /// </summary>
    public const string DeleteService = "DELETE_SERVICE";
}

/// <summary>
/// Sync type that triggered the fuzzy watch change.
/// </summary>
public static class NamingFuzzyWatchSyncType
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
/// Represents a fuzzy watch service change event.
/// </summary>
public class NamingFuzzyWatchChangeEvent
{
    /// <summary>
    /// Gets the namespace.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Gets the group name.
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// Gets the service name.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// Gets the change type (ADD_SERVICE, DELETE_SERVICE).
    /// </summary>
    public string ChangeType { get; }

    /// <summary>
    /// Gets the sync type that triggered this change.
    /// </summary>
    public string SyncType { get; }

    public NamingFuzzyWatchChangeEvent(string ns, string groupName, string serviceName, string changeType, string syncType)
    {
        Namespace = ns;
        GroupName = groupName;
        ServiceName = serviceName;
        ChangeType = changeType;
        SyncType = syncType;
    }

    /// <summary>
    /// Gets the service key (groupName@@serviceName).
    /// </summary>
    public string GetServiceKey()
    {
        return $"{GroupName}{NacosConstants.ServiceInfoSplitter}{ServiceName}";
    }

    public override string ToString()
    {
        return $"NamingFuzzyWatchChangeEvent{{namespace='{Namespace}', groupName='{GroupName}', serviceName='{ServiceName}', changeType='{ChangeType}', syncType='{SyncType}'}}";
    }
}
