using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Events.Types;

namespace BasaltBE.Basalt.Events.Containers;

public sealed class ContainerTransactionEvent(Player player, int requestId, ReadOnlyMemory<ContainerSlotChange> changes) : Event {
    public readonly Player Player = player;
    public readonly int RequestId = requestId;
    public readonly ReadOnlyMemory<ContainerSlotChange> Changes = changes;
}
