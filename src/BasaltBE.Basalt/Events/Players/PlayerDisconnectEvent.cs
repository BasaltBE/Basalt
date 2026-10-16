using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Events.Players;

public sealed class PlayerDisconnectEvent(Player player) : Event
{
    public readonly Player Player = player;
}
