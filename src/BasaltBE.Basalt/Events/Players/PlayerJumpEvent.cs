using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Events.Players;

public sealed class PlayerJumpEvent(Player player) : Event {
    public readonly Player Player = player;
}
