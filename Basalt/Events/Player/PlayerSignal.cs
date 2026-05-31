namespace Basalt.Server.Events;

using Basalt.Server.Player;

public abstract class PlayerSignal : EntitySignal
{
    public Player Player { get; }

    protected PlayerSignal(Player player)
    {
        Player = player;
    }
}






