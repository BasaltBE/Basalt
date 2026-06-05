namespace Basalt.Core.Events;

using Basalt.Core.Player;

public abstract class PlayerSignal : EntitySignal
{
    public Player Player { get; }

    protected PlayerSignal(Player player)
    {
        Player = player;
    }
}






