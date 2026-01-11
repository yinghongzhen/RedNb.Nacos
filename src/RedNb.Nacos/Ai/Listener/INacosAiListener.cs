namespace RedNb.Nacos.Core.Ai.Listener;

/// <summary>
/// Nacos AI module event listener interface.
/// </summary>
/// <typeparam name="TEvent">The type of AI event.</typeparam>
public interface INacosAiListener<in TEvent> where TEvent : INacosAiEvent
{
    /// <summary>
    /// Callback when an event is received.
    /// </summary>
    /// <param name="event">The event.</param>
    void OnEvent(TEvent @event);
}
