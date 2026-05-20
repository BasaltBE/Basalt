using Basalt.Containers;
using Basalt.Core;
using Basalt.Entity.Traits;
using Basalt.Item;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class ItemStackRequest
{
    // TODO:  The damn ahh InventorySlotPacket is giving an errror
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ItemStackRequestPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet.Deserialize(reader);

        if (!server.Players.TryGetValue(connection, out Player? player) || packet.Requests.Count == 0)
        {
            return;
        }

        List<ItemStackResponse> responses = new(packet.Requests.Count);

        foreach (Protocol.Types.ItemStackRequest request in packet.Requests)
        {
            try
            {
                responses.Add(ProcessRequest(player, request));
            }
            catch (Exception exception)
            {
                Console.WriteLine($"ItemStackRequest exception: request: {request.RequestId} {exception}");

                responses.Add(new ItemStackResponse
                {
                    Status = ItemStackResponseStatus.Error,
                    RequestId = request.RequestId,
                    ContainerInfo = []
                });
            }
        }

        server.Network.SendPacket(connection, new ItemStackResponsePacket
        {
            Responses = responses
        });
    }

    private static ItemStackResponse ProcessRequest(Player player, Protocol.Types.ItemStackRequest request)
    {
        Dictionary<string, StackResponseContainerInfo> changedContainers = [];

        foreach (IStackRequestAction action in request.Actions)
        {
            ItemStackResponseStatus status = action switch
            {
                TransferStackRequestAction transfer => TransferItem(player, transfer, changedContainers),
                SwapStackRequestAction swap => SwapItems(player, swap, changedContainers),
                DropStackRequestAction drop => RemoveDroppedItem(player, drop, changedContainers),
                DestroyStackRequestAction destroy => RemoveDestroyedItem(player, destroy, changedContainers),

                EmptyStackRequestAction => ItemStackResponseStatus.Ok,
                CraftResultsDeprecatedStackRequestAction => ItemStackResponseStatus.Ok,

                _ => ItemStackResponseStatus.InvalidRequestActionType
            };

            if (status == ItemStackResponseStatus.Ok)
            {
                continue;
            }

            Console.WriteLine($"ItemStackRequest failed: request: {request.RequestId} status={status} action={DescribeAction(action)}");

            return new ItemStackResponse
            {
                Status = status,
                RequestId = request.RequestId,
                ContainerInfo = []
            };
        }

        return new ItemStackResponse
        {
            Status = ItemStackResponseStatus.Ok,
            RequestId = request.RequestId,
            ContainerInfo = changedContainers.Count > 0
                ? [.. changedContainers.Values]
                : []
        };
    }

    private static ItemStackResponseStatus TransferItem(
        Player player,
        TransferStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        Container? sourceContainer = GetContainer(player, action.Source.Container, action.Source.Slot);
        Container? destinationContainer = GetContainer(player, action.Destination.Container, action.Destination.Slot);
        int sourceSlot = StorageSlot(player, action.Source.Container, action.Source.Slot);
        int destinationSlot = StorageSlot(player, action.Destination.Container, action.Destination.Slot);

        if (sourceContainer is null || destinationContainer is null)
        {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        if (sourceSlot < 0 || sourceSlot >= sourceContainer.GetSize() ||
            destinationSlot < 0 || destinationSlot >= destinationContainer.GetSize())
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        ItemStack? sourceItem = sourceContainer.GetItem(sourceSlot);
        if (sourceItem is null)
        {
            return ItemStackResponseStatus.FailedToMatchExpectedSlotConsumedItem;
        }

        int amount = Math.Min(Math.Max(1, (int)action.Count), sourceItem.StackSize);
        ItemStack? destinationItem = destinationContainer.GetItem(destinationSlot);
        if (destinationItem is not null &&
            action.Destination.Container.ContainerId == (byte)ContainerId.InventoryUi &&
            action.Destination.StackNetworkId == 0)
        {
            destinationContainer.ClearSlot(destinationSlot);
            destinationItem = null;
        }

        if (destinationItem is null)
        {
            ItemStack movedItem = sourceContainer.TakeItem(sourceSlot, amount) ?? ItemStack.Empty();

            if (movedItem.Type == ItemType.Air || movedItem.StackSize == 0)
            {
                return ItemStackResponseStatus.CannotRemoveItem;
            }

            destinationContainer.SetItem(destinationSlot, movedItem);
        }
        else
        {
            if (!sourceItem.CanStackWith(destinationItem))
            {
                return ItemStackResponseStatus.CannotPlaceItem;
            }

            int availableSpace = destinationItem.Type.MaxStackSize - destinationItem.StackSize;
            if (availableSpace <= 0)
            {
                return ItemStackResponseStatus.CannotPlaceItem;
            }

            amount = Math.Min(amount, availableSpace);

            destinationItem.IncrementStack((ushort)amount);
            sourceItem.DecrementStack((ushort)amount);

            if (sourceItem.StackSize == 0)
            {
                sourceContainer.ClearSlot(sourceSlot);
            }
            else
            {
                sourceContainer.UpdateSlot(sourceSlot);
            }

            destinationContainer.UpdateSlot(destinationSlot);
        }

        AddChangedSlot(changedContainers, action.Source.Container, sourceContainer, action.Source.Slot, sourceSlot);
        AddChangedSlot(changedContainers, action.Destination.Container, destinationContainer, action.Destination.Slot, destinationSlot);

        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus SwapItems(
        Player player,
        SwapStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        Container? sourceContainer = GetContainer(player, action.Source.Container, action.Source.Slot);
        Container? destinationContainer = GetContainer(player, action.Destination.Container, action.Destination.Slot);
        int sourceSlot = StorageSlot(player, action.Source.Container, action.Source.Slot);
        int destinationSlot = StorageSlot(player, action.Destination.Container, action.Destination.Slot);

        if (sourceContainer is null || destinationContainer is null)
        {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        if (sourceSlot < 0 || sourceSlot >= sourceContainer.GetSize() ||
            destinationSlot < 0 || destinationSlot >= destinationContainer.GetSize())
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        sourceContainer.SwapItems(sourceSlot, destinationSlot, destinationContainer);

        AddChangedSlot(changedContainers, action.Source.Container, sourceContainer, action.Source.Slot, sourceSlot);
        AddChangedSlot(changedContainers, action.Destination.Container, destinationContainer, action.Destination.Slot, destinationSlot);

        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus RemoveDroppedItem(
        Player player,
        DropStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        Container? container = GetContainer(player, action.Source.Container, action.Source.Slot);
        int slot = StorageSlot(player, action.Source.Container, action.Source.Slot);
        if (container is null)
        {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        if (slot < 0 || slot >= container.GetSize())
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        int amount = Math.Max(1, (int)action.Count);
        ItemStack? removedItem = container.TakeItem(slot, amount);

        if (removedItem is null)
        {
            return ItemStackResponseStatus.CannotDropItem;
        }

        AddChangedSlot(changedContainers, action.Source.Container, container, action.Source.Slot, slot);

        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus RemoveDestroyedItem(
        Player player,
        DestroyStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        Container? container = GetContainer(player, action.Source.Container, action.Source.Slot);
        int slot = StorageSlot(player, action.Source.Container, action.Source.Slot);
        if (container is null)
        {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        if (slot < 0 || slot >= container.GetSize())
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        int amount = Math.Max(1, (int)action.Count);
        ItemStack? removedItem = container.TakeItem(slot, amount);

        if (removedItem is null)
        {
            return ItemStackResponseStatus.CannotDestroyItem;
        }

        AddChangedSlot(changedContainers, action.Source.Container, container, action.Source.Slot, slot);

        return ItemStackResponseStatus.Ok;
    }

    private static void AddChangedSlot(
        Dictionary<string, StackResponseContainerInfo> changedContainers,
        FullContainerName containerName,
        Container container,
        int responseSlot,
        int storageSlot)
    {
        string containerKey = containerName.DynamicContainerId.HasValue
            ? $"{containerName.ContainerId}:{containerName.DynamicContainerId.Value}"
            : containerName.ContainerId.ToString();

        if (!changedContainers.TryGetValue(containerKey, out StackResponseContainerInfo? containerInfo))
        {
            containerInfo = new StackResponseContainerInfo
            {
                Container = new FullContainerName
                {
                    ContainerId = containerName.ContainerId,
                    DynamicContainerId = containerName.DynamicContainerId
                },
                SlotInfo = []
            };

            changedContainers[containerKey] = containerInfo;
        }

        ItemStack? item = container.GetItem(storageSlot);

        containerInfo.SlotInfo.RemoveAll(slot => slot.Slot == responseSlot);
        containerInfo.SlotInfo.Add(new StackResponseSlotInfo
        {
            Slot = (byte)responseSlot,
            HotbarSlot = (byte)responseSlot,
            Count = (byte)(item?.StackSize ?? 0),
            StackNetworkId = item?.NetworkStackId ?? 0,
            CustomName = string.Empty,
            FilteredCustomName = string.Empty,
            DurabilityCorrection = 0
        });
    }

    private static int StorageSlot(Player player, FullContainerName container, int slot)
    {
        bool openContainerInventory = container.ContainerId == (byte)ContainerId.DynamicContainer && HasOpenInventoryView(player) && slot is >= 27 and <= 62;
        if (!openContainerInventory && container.ContainerId is not ((byte)ContainerId.Armor or 12 or (byte)ContainerId.Inventory or (byte)ContainerId.Hotbar or (byte)ContainerId.FixedInventory or (byte)ContainerId.Offhand))
        {
            return slot;
        }

        if (HasOpenInventoryView(player) && slot is >= 54 and <= 62)
        {
            return slot - 54;
        }

        if (slot is >= 36 and <= 44)
        {
            return slot - 36;
        }

        if (HasOpenInventoryView(player) && slot is >= 27 and <= 53)
        {
            return slot - 18;
        }

        return slot;
    }

    private static Container? GetContainer(Player player, FullContainerName name, int slot)
    {
        if (name.ContainerId == (byte)ContainerId.DynamicContainer && HasOpenInventoryView(player) && slot is >= 27 and <= 62)
        {
            return player.GetTrait<EntityInventoryTrait>()?.Container;
        }

        return player.GetContainer(name);
    }

    private static bool HasOpenInventoryView(Player player)
    {
        foreach ((int _, Container container) in player.openedContainers)
        {
            if (container.Type != ContainerType.Inventory)
            {
                return true;
            }
        }

        return false;
    }


    // These are temp cause it's a mess
    private static string DescribeAction(IStackRequestAction action)
    {
        return action switch
        {
            TransferStackRequestAction transfer =>
                $"transfer count={transfer.Count} src={DescribeSlot(transfer.Source)} dst={DescribeSlot(transfer.Destination)}",
            SwapStackRequestAction swap =>
                $"swap src={DescribeSlot(swap.Source)} dst={DescribeSlot(swap.Destination)}",
            DropStackRequestAction drop =>
                $"drop count={drop.Count} src={DescribeSlot(drop.Source)}",
            DestroyStackRequestAction destroy =>
                $"destroy count={destroy.Count} src={DescribeSlot(destroy.Source)}",
            _ => action.GetType().Name
        };
    }
    // These are temp cause it's a mess

    private static string DescribeSlot(StackRequestSlotInfo slot)
    {
        string dynamicId = slot.Container.DynamicContainerId.HasValue
            ? slot.Container.DynamicContainerId.Value.ToString()
            : "none";

        return $"container={slot.Container.ContainerId} dynamic={dynamicId} slot={slot.Slot} stack={slot.StackNetworkId}";
    }
}
