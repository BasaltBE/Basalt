using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Events.Players;

public sealed class PlayerJoinEvent(Player player) : Event
{
    public readonly Player Player = player;
}
