using BasaltBE.Basalt.Items.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Items.Containers;

public sealed class BlockContainer : Container {
    public readonly BlockPos Position;

    public BlockContainer(
        BlockPos position,
        int size,
        ContainerId containerId = ContainerId.First,
        ContainerEnumName containerName = ContainerEnumName.LevelEntity,
        ContainerType type = ContainerType.Container,
        uint? dynamicId = null,
        int slotOffset = 0,
        IReadOnlySet<ItemType>? bannedItems = null,
        ContainerEnumName[]? slotNames = null
    ) : base(size, containerId, containerName, type, dynamicId, slotOffset, bannedItems, slotNames) {
        ArgumentNullException.ThrowIfNull(position);
        Position = position;
    }

    protected override BlockPos GetPosition() => Position;
}
