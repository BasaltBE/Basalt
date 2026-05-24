using Basalt.Core;
using Basalt.Entity.Traits.Types;

namespace Basalt.Events;

public sealed class PlayerSpawnSignal : PlayerSignal
{
    public override ServerEvent Event => ServerEvent.PlayerSpawn;
    public EntitySpawnOptions Options;
    public bool Cancelled;

    public PlayerSpawnSignal(Player player, EntitySpawnOptions options) : base(player)
    {
        Options = options;
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
