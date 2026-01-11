namespace RedNb.Nacos.Core.Naming.FuzzyWatch;

/// <summary>
/// Interface for watching fuzzy naming service changes.
/// </summary>
public interface INamingFuzzyWatchEventWatcher
{
    /// <summary>
    /// Callback invoked when a fuzzy service change event occurs.
    /// </summary>
    /// <param name="event">The service change event</param>
    void OnEvent(NamingFuzzyWatchChangeEvent @event);

    /// <summary>
    /// Gets the task scheduler for executing the callback.
    /// If null, uses the default scheduler.
    /// </summary>
    TaskScheduler? Scheduler => null;
}

/// <summary>
/// Abstract base class for fuzzy watch event watchers.
/// </summary>
public abstract class AbstractNamingFuzzyWatchEventWatcher : INamingFuzzyWatchEventWatcher
{
    /// <inheritdoc />
    public abstract void OnEvent(NamingFuzzyWatchChangeEvent @event);

    /// <inheritdoc />
    public virtual TaskScheduler? Scheduler => null;
}

/// <summary>
/// Simple delegate-based fuzzy watch event watcher.
/// </summary>
public class NamingFuzzyWatchEventWatcher : INamingFuzzyWatchEventWatcher
{
    private readonly Action<NamingFuzzyWatchChangeEvent> _handler;
    private readonly TaskScheduler? _scheduler;

    public NamingFuzzyWatchEventWatcher(Action<NamingFuzzyWatchChangeEvent> handler, TaskScheduler? scheduler = null)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _scheduler = scheduler;
    }

    public void OnEvent(NamingFuzzyWatchChangeEvent @event)
    {
        _handler(@event);
    }

    public TaskScheduler? Scheduler => _scheduler;
}
