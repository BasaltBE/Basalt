using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items.Containers;

namespace BasaltBE.Basalt.Events.Players;

public sealed class PlayerCloseContainerEvent(Player player, Container container, bool serverInitiated) : Event
{
    public readonly Player Player = player;
    public readonly Container Container = container;
    public readonly bool ServerInitiated = serverInitiated;
}
