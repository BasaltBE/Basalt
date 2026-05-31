namespace Basalt.Server.Events;

public abstract class EntitySignal : ISignal
{
    public abstract ServerEvent Event { get; }
}






