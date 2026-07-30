namespace Basalt.Core.Events;

using Basalt.Core.Entities;
using Basalt.Core.Player;

public sealed class PlayerAttackEntitySignal : PlayerSignal {
    public override ServerEvent Event => ServerEvent.PlayerAttackEntity;
    public Entity Target { get; }
    public bool Cancelled;

    public PlayerAttackEntitySignal(Player player, Entity target) : base(player) {
        Target = target;
    }

    public bool Emit() => !Cancelled;

    public void Cancel() {
        Cancelled = true;
    }
}
