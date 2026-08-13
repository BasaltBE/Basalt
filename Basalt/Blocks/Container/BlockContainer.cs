namespace Basalt.Core.Blocks.Container;

using Basalt.Core.Containers;
using Basalt.Core.Worlds.Dimensions;


using BedrockProtocol.Enums;
using BedrockProtocol.Packets;
using BedrockProtocol.Types;

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
        if (Type == ContainerType.WORKBENCH) {
            foreach ((Basalt.Core.Player.Player player, ContainerID _) in occupants) {
                if (!player.Spawned) {
                    continue;
                }

                for (int slot = 0; slot < GetSize(); slot++) {
                    player.Send(new InventorySlotPacket {
                        ContainerId = (byte)ContainerID.CONTAINER_ID_PLAYER_ONLY_UI,
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
        if (Type == ContainerType.WORKBENCH) {
            if (slot < 0 || slot >= GetSize()) {
                return;
            }

            foreach ((Basalt.Core.Player.Player player, ContainerID _) in occupants) {
                if (!player.Spawned) {
                    continue;
                }

                player.Send(new InventorySlotPacket {
                    ContainerId = (byte)ContainerID.CONTAINER_ID_PLAYER_ONLY_UI,
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

    protected override ContainerEnumName GetFullContainerID() {
        return Type == ContainerType.WORKBENCH
            ? ContainerEnumName.CraftingInputContainer
            : base.GetFullContainerID();
    }

    protected override int GetNetworkSlot(int slot) {
        return Type == ContainerType.WORKBENCH ? slot + 32 : slot;
    }

    protected override void OnViewerAdded(Basalt.Core.Player.Player player, ContainerID containerId) {
        OnViewerAddedEvent?.Invoke(this, player);
    }

    protected override void OnViewerRemoved(Basalt.Core.Player.Player player, ContainerID containerId) {
        OnViewerRemovedEvent?.Invoke(this, player);
    }
}







