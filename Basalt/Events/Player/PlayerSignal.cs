using Basalt.Core;

namespace Basalt.Events;

public abstract class PlayerSignal : EntitySignal
{
    public Player Player { get; }

    protected PlayerSignal(Player player)
    {
        Player = player;
    }
}
