namespace RedNb.Nacos.Core.Config.FuzzyWatch;

/// <summary>
/// Interface for watching fuzzy configuration changes.
/// </summary>
public interface IConfigFuzzyWatchEventWatcher
{
    /// <summary>
    /// Callback invoked when a fuzzy configuration change event occurs.
    /// </summary>
    /// <param name="event">The configuration change event</param>
    void OnEvent(ConfigFuzzyWatchChangeEvent @event);

    /// <summary>
    /// Gets the task scheduler for executing the callback.
    /// If null, uses the default scheduler.
    /// </summary>
    TaskScheduler? Scheduler => null;
}

/// <summary>
/// Abstract base class for fuzzy watch event watchers.
/// </summary>
public abstract class AbstractConfigFuzzyWatchEventWatcher : IConfigFuzzyWatchEventWatcher
{
    /// <inheritdoc />
    public abstract void OnEvent(ConfigFuzzyWatchChangeEvent @event);

    /// <inheritdoc />
    public virtual TaskScheduler? Scheduler => null;
}

/// <summary>
/// Simple delegate-based fuzzy watch event watcher.
/// </summary>
public class ConfigFuzzyWatchEventWatcher : IConfigFuzzyWatchEventWatcher
{
    private readonly Action<ConfigFuzzyWatchChangeEvent> _handler;
    private readonly TaskScheduler? _scheduler;

    public ConfigFuzzyWatchEventWatcher(Action<ConfigFuzzyWatchChangeEvent> handler, TaskScheduler? scheduler = null)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _scheduler = scheduler;
    }

    public void OnEvent(ConfigFuzzyWatchChangeEvent @event)
    {
        _handler(@event);
    }

    public TaskScheduler? Scheduler => _scheduler;
}
