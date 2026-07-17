using Basalt.Core.Player;

namespace Basalt.Core.Events;

public sealed class PlayerRespawnSignal : PlayerSignal
{
    public override ServerEvent Event => ServerEvent.PlayerRespawn;
    public bool Cancelled;

    public PlayerRespawnSignal(Player.Player player) : base(player)
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
