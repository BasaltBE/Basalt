namespace BasaltBE.Basalt.Events.Types;

internal sealed class EventChannel<TEvent> where TEvent : Event
{
    private readonly Lock sync = new();
    private EventSubscription<TEvent>[] subscribers = [];

    internal bool Subscribed => Volatile.Read(ref subscribers).Length != 0;

    internal IDisposable Subscribe(Action<TEvent> handler)
    {
        EventSubscription<TEvent> subscription = new(this, handler);
        lock (sync)
            Volatile.Write(ref subscribers, [.. subscribers, subscription]);
        return subscription;
    }

    internal void Remove(EventSubscription<TEvent> subscription)
    {
        lock (sync)
        {
            int index = Array.IndexOf(subscribers, subscription);
            if (index < 0)
                return;
            if (subscribers.Length == 1)
            {
                Volatile.Write(ref subscribers, []);
                return;
            }
            EventSubscription<TEvent>[] reduced = new EventSubscription<TEvent>[subscribers.Length - 1];
            subscribers.AsSpan(0, index).CopyTo(reduced);
            subscribers.AsSpan(index + 1).CopyTo(reduced.AsSpan(index));
            Volatile.Write(ref subscribers, reduced);
        }
    }

    internal void Emit(TEvent value)
    {
        foreach (EventSubscription<TEvent> subscriber in Volatile.Read(ref subscribers))
            subscriber.Handler(value);
    }
}
