using BasaltBE.Basalt.Events.Types;

namespace BasaltBE.Basalt.Events;

public sealed class EventEmitter {
    private readonly Lock sync = new();
    private object?[] channels = [];

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : Event {
        ArgumentNullException.ThrowIfNull(handler);
        int id = EventType<TEvent>.Id;
        lock (sync) {
            object?[] current = channels;
            if (id >= current.Length) {
                object?[] expanded = new object?[Math.Max(id + 1, Math.Max(8, current.Length * 2))];
                current.CopyTo(expanded, 0);
                expanded[id] = new EventChannel<TEvent>();
                Volatile.Write(ref channels, expanded);
                current = expanded;
            }

            EventChannel<TEvent>? channel = (EventChannel<TEvent>?)current[id];
            if (channel is null) {
                channel = new();
                Volatile.Write(ref current[id], channel);
            }
            return channel.Subscribe(handler);
        }
    }

    public bool HasSubscribers<TEvent>() where TEvent : Event {
        object?[] current = Volatile.Read(ref channels);
        int id = EventType<TEvent>.Id;
        return id < current.Length && Volatile.Read(ref current[id]) is EventChannel<TEvent> channel && channel.Subscribed;
    }

    public void Emit<TEvent>(TEvent value) where TEvent : Event {
        ArgumentNullException.ThrowIfNull(value);
        object?[] current = Volatile.Read(ref channels);
        int id = EventType<TEvent>.Id;
        if (id < current.Length && Volatile.Read(ref current[id]) is EventChannel<TEvent> channel)
            channel.Emit(value);
    }

    public void Clear() {
        lock (sync)
            Volatile.Write(ref channels, []);
    }
}
