namespace Basalt.Core.Blocks.Container;

using Basalt.Core.Containers;
using Basalt.Core.Worlds.Dimensions;


using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class BlockContainer : Container {
    public Action<BlockContainer, Basalt.Core.Player.Player>? OnViewerAddedEvent { get; set; }
    public Action<BlockContainer, Basalt.Core.Player.Player>? OnViewerRemovedEvent { get; set; }
    public Dimension? Dimension { get; set; }
    public BlockPos Position { get; set; }
    public Action<BlockContainer>? OnContainerUpdated { get; set; }

    public BlockContainer(Dimension? dimension, BlockPos position, ContainerType type, int size) : base(type, size) {
        Dimension = dimension;
        Position = position;
    }

    public override void Update() {
        OnContainerUpdated?.Invoke(this);
        if (Type is ContainerType.WORKBENCH or ContainerType.ANVIL) {
            if (Type == ContainerType.ANVIL) return;

            foreach ((Basalt.Core.Player.Player player, ContainerId _) in occupants) {
                if (!player.Spawned) {
                    continue;
                }

                for (int slot = 0; slot < GetSize(); slot++) {
                    player.Send(new InventorySlotPacket {
                        ContainerId = ContainerId.PlayerOnlyUi,
                        Slot = (uint)(slot + 32),
                        Item = GetItem(slot)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()
                    });
                }
            }
            return;
        }

        base.Update();
    }

    public override void UpdateSlot(int slot) {
        OnContainerUpdated?.Invoke(this);
        if (Type is ContainerType.WORKBENCH or ContainerType.ANVIL) {
            if (Type == ContainerType.ANVIL) return;

            if (slot < 0 || slot >= GetSize()) {
                return;
            }

            foreach ((Basalt.Core.Player.Player player, ContainerId _) in occupants) {
                if (!player.Spawned) {
                    continue;
                }

                player.Send(new InventorySlotPacket {
                    ContainerId = ContainerId.PlayerOnlyUi,
                    Slot = (uint)(slot + 32),
                    Item = GetItem(slot)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()
                });
            }
            return;
        }

        base.UpdateSlot(slot);
    }

    protected override BlockPos GetContainerPosition() {
        return Position;
    }

    protected override ContainerEnumName GetFullContainerId() {
        return Type == ContainerType.WORKBENCH
            ? ContainerEnumName.CraftingInputContainer
            : base.GetFullContainerId();
    }

    protected override int GetNetworkSlot(int slot) {
        return Type == ContainerType.WORKBENCH ? slot + 32 : slot;
    }

    protected override void OnViewerAdded(Basalt.Core.Player.Player player, ContainerId containerId) {
        OnViewerAddedEvent?.Invoke(this, player);
    }

    protected override void OnViewerRemoved(Basalt.Core.Player.Player player, ContainerId containerId) {
        OnViewerRemovedEvent?.Invoke(this, player);
    }
}







