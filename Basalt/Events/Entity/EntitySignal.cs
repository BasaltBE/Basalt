namespace Basalt.Events;

public abstract class EntitySignal : ISignal
{
    public abstract ServerEvent Event { get; }
}
