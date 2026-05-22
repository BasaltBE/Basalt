namespace Basalt.World;

public interface Tickable
{
    ulong TickValue { get; set; }
    double TickWork { get; set; }
}
