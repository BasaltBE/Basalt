namespace Basalt.Core.Events;

public abstract class EntitySignal : ISignal
{
    public abstract ServerEvent Event { get; }
}






