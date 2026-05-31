namespace Basalt.Server.Events;

public abstract class ServerSignal : ISignal
{
    public abstract ServerEvent Event { get; }
}






