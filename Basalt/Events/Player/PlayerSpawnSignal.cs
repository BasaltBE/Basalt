using Basalt.Core.Player;
using Basalt.Core.Entities.Traits.Types;

namespace Basalt.Core.Events;

public sealed class PlayerSpawnSignal : PlayerSignal {
    public override ServerEvent Event => ServerEvent.PlayerSpawn;
    public EntitySpawnOptions Options;
    public bool Cancelled;

    public PlayerSpawnSignal(Player.Player player, EntitySpawnOptions options) : base(player) {
        Options = options;
    }

    public bool Emit() {
        return !Cancelled;
    }

    public void Cancel() {
        Cancelled = true;
    }
}






