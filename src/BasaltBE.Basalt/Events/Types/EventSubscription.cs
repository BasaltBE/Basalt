namespace BasaltBE.Basalt.Events.Types;

internal sealed class EventSubscription<TEvent>(EventChannel<TEvent> channel, Action<TEvent> handler) : IDisposable where TEvent : Event {
    private EventChannel<TEvent>? channel = channel;
    internal readonly Action<TEvent> Handler = handler;

    public void Dispose() => Interlocked.Exchange(ref channel, null)?.Remove(this);
}
