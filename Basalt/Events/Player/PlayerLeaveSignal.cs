using Basalt.Core;
using Basalt.Entity.Traits.Types;

namespace Basalt.Events;

public sealed class PlayerLeaveSignal : PlayerSignal
{
    public override ServerEvent Event => ServerEvent.PlayerLeave;
    public EntityDespawnOptions Options { get; }

    public PlayerLeaveSignal(Player player, EntityDespawnOptions options) : base(player)
    {
        Options = options;
    }
}
