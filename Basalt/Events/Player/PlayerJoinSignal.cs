namespace Basalt.Server.Events;

using Basalt.Server.Player;

public sealed class PlayerJoinSignal : PlayerSignal
{
    public override ServerEvent Event => ServerEvent.PlayerJoin;
    public bool Cancelled;

    public PlayerJoinSignal(Player player) : base(player)
    {
    }

    public bool Emit()
    {
        return !Cancelled;
    }

    public void Cancel()
    {
        Cancelled = true;
    }
}






