using BasaltBE.Basalt.Entities;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Items.Containers;

public sealed class EntityContainer : Container {
    public readonly Entity Entity;

    public EntityContainer(
        Entity entity,
        int size,
        ContainerId containerId = ContainerId.Inventory,
        ContainerEnumName containerName = ContainerEnumName.Inventory,
        ContainerType type = ContainerType.Inventory,
        uint? dynamicId = null,
        int slotOffset = 0
    ) : base(size, containerId, containerName, type, dynamicId, slotOffset) {
        ArgumentNullException.ThrowIfNull(entity);
        Entity = entity;
    }

    protected override long GetTargetActorId() => Entity.UniqueId;
}
