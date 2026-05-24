namespace Basalt.Events;

public abstract class ServerSignal : ISignal
{
    public abstract ServerEvent Event { get; }
}
