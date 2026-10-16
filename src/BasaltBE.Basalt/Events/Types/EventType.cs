namespace BasaltBE.Basalt.Events.Types;

internal static class EventType
{
    internal static int NextId;
}

internal static class EventType<TEvent> where TEvent : Event
{
    internal static readonly int Id = Interlocked.Increment(ref EventType.NextId) - 1;
}
