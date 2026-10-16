using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Events.Players;

public sealed class PlayerSneakEvent(Player player, bool sneaking) : Event
{
    public readonly Player Player = player;
    public readonly bool Sneaking = sneaking;
}
