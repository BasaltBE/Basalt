using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items.Containers;

namespace BasaltBE.Basalt.Events.Players;

public sealed class PlayerOpenContainerEvent(Player player, Container container) : Event
{
    public readonly Player Player = player;
    public readonly Container Container = container;
}
