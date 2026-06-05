namespace Basalt.Core.Network.Handlers;

using Basalt.Core.Containers;
using Basalt.Core;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;
using Basalt.Core.Player.Traits;


public static class ItemStackRequest
{
    // TODO:  The damn ahh InventorySlotPacket is giving an errror
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ItemStackRequestPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (ItemStackRequestPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!server.Players.TryGetValue(connection, out Player.Player? player) || packet.Requests.Count == 0)
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

    private static ItemStackResponse ProcessRequest(Player.Player player, Protocol.Types.ItemStackRequest request)
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
                CraftCreativeStackRequestAction craftCreative => CreateCreativeItem(player, craftCreative, changedContainers),

                EmptyStackRequestAction => ItemStackResponseStatus.Ok,
                CraftResultsDeprecatedStackRequestAction => ItemStackResponseStatus.Ok,

                _ => ItemStackResponseStatus.InvalidRequestActionType
            };

            if (status == ItemStackResponseStatus.Ok)
            {
                continue;
            }

            Console.WriteLine($"ItemStackRequest failed: request: {request.RequestId} status={status} action={DescribeAction(action)}");
            foreach (Container container in player.openedContainers.Values.Distinct())
            {
                container.Update();
            }

            player.GetTrait<PlayerCursorTrait>()?.Container.UpdateSlot(0);

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
        Player.Player player,
        TransferStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        if (!TryResolveSlot(player, action.Source, out Container sourceContainer, out int sourceSlot) ||
            !TryResolveSlot(player, action.Destination, out Container destinationContainer, out int destinationSlot))
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
        if (action.Destination.Slot >= 0 &&
            action.Destination.StackNetworkId == 0 &&
            destinationItem is not null &&
            sourceItem is not null)
        {
            int resolvedSlot = ResolveDestinationSlot(destinationContainer, sourceItem, destinationSlot);
            if (resolvedSlot >= 0)
            {
                destinationSlot = resolvedSlot;
                destinationItem = destinationContainer.GetItem(destinationSlot);
            }
        }

        if (destinationItem is null)
        {
            ItemStack movedItem = sourceContainer.TakeItem(sourceSlot, amount) ?? ItemStack.Empty();
            if ((movedItem.Type == ItemType.Air || movedItem.StackSize == 0) &&
                action.Source.StackNetworkId != 0 &&
                TryFindSlotByStackNetworkId(sourceContainer, action.Source.StackNetworkId, out int actualSourceSlot))
            {
                sourceSlot = actualSourceSlot;
                sourceItem = sourceContainer.GetItem(sourceSlot);
                if (sourceItem is null)
                {
                    return ItemStackResponseStatus.CannotRemoveItem;
                }

                amount = Math.Min(Math.Max(1, (int)action.Count), sourceItem.StackSize);
                movedItem = sourceContainer.TakeItem(sourceSlot, amount) ?? ItemStack.Empty();
            }

            if (movedItem.Type == ItemType.Air || movedItem.StackSize == 0)
            {
                return ItemStackResponseStatus.CannotRemoveItem;
            }

            destinationContainer.SetItem(destinationSlot, movedItem);
        }
        else
        {
            if (destinationItem is null)
            {
                return ItemStackResponseStatus.CannotPlaceItem;
            }

            ItemStack destinationExisting = destinationItem;
            ItemStack sourceExisting = sourceItem ?? ItemStack.Empty();
            if (sourceExisting.Type == ItemType.Air || sourceExisting.StackSize == 0)
            {
                return ItemStackResponseStatus.CannotRemoveItem;
            }
            if (!sourceExisting.CanStackWith(destinationExisting))
            {
                if (action.Destination.StackNetworkId == 0 && sourceItem is not null)
                {
                    int resolvedSlot = ResolveDestinationSlot(destinationContainer, sourceItem, destinationSlot);
                    if (resolvedSlot >= 0 && resolvedSlot != destinationSlot)
                    {
                        destinationSlot = resolvedSlot;
                        destinationItem = destinationContainer.GetItem(destinationSlot);
                        if (destinationItem is null)
                        {
                            ItemStack movedItem = sourceContainer.TakeItem(sourceSlot, amount) ?? ItemStack.Empty();
                            if (movedItem.Type == ItemType.Air || movedItem.StackSize == 0)
                            {
                                return ItemStackResponseStatus.CannotRemoveItem;
                            }

                            destinationContainer.SetItem(destinationSlot, movedItem);
                            AddChangedSlot(changedContainers, action.Source.Container, sourceContainer, action.Source.Slot, sourceSlot);
                            AddChangedSlot(changedContainers, action.Destination.Container, destinationContainer, action.Destination.Slot, destinationSlot);
                            return ItemStackResponseStatus.Ok;
                        }

                        destinationExisting = destinationItem;
                    }
                }

                return ItemStackResponseStatus.CannotPlaceItem;
            }

            int availableSpace = destinationExisting.Type.MaxStackSize - destinationExisting.StackSize;
            if (availableSpace <= 0)
            {
                return ItemStackResponseStatus.CannotPlaceItem;
            }

            amount = Math.Min(amount, availableSpace);

            destinationExisting.IncrementStack((ushort)amount);
            sourceExisting.DecrementStack((ushort)amount);

            if (sourceExisting.StackSize == 0)
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

    private static Container? GetContainer(Player.Player player, FullContainerName name, int slot)
    {
        if (TryGetOpenedDynamicContainer(player, name, out Container openedContainer))
        {
            if (slot < openedContainer.GetSize())
            {
                return openedContainer;
            }

            return player.GetTrait<EntityInventoryTrait>()?.Container;
        }

        if (name.ContainerId == (byte)ContainerId.DynamicContainer)
        {
            return null;
        }

        return player.GetContainer(name);
    }

    private static int StorageSlot(Player.Player player, FullContainerName container, int slot)
    {
        if (container.ContainerId is not ((byte)ContainerId.Armor or 12 or (byte)ContainerId.Inventory or (byte)ContainerId.Hotbar or (byte)ContainerId.FixedInventory or (byte)ContainerId.Offhand))
        {
            return slot;
        }

        if (slot is >= 36 and <= 44)
        {
            return slot - 36;
        }

        return slot;
    }

    private static int ResolveDestinationSlot(Container container, ItemStack sourceItem, int preferredSlot)
    {
        if (preferredSlot >= 0 && preferredSlot < container.GetSize())
        {
            ItemStack? preferred = container.GetItem(preferredSlot);
            if (preferred is null)
            {
                return preferredSlot;
            }

            if (preferred.CanStackWith(sourceItem) && preferred.StackSize < preferred.Type.MaxStackSize)
            {
                return preferredSlot;
            }
        }

        for (int i = 0; i < container.GetSize(); i++)
        {
            ItemStack? item = container.GetItem(i);
            if (item is null)
            {
                continue;
            }

            if (item.CanStackWith(sourceItem) && item.StackSize < item.Type.MaxStackSize)
            {
                return i;
            }
        }

        for (int i = 0; i < container.GetSize(); i++)
        {
            if (container.GetItem(i) is null)
            {
                return i;
            }
        }

        return -1;
    }

    private static ItemStackResponseStatus CreateCreativeItem(
        Player.Player player,
        CraftCreativeStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        if (player.Gamemode != Gamemode.Creative)
        {
            return ItemStackResponseStatus.PlayerNotInCreativeMode;
        }

        Container? cursor = player.GetContainer(new FullContainerName { ContainerId = (byte)ContainerId.Cursor });
        if (cursor is null)
        {
            return ItemStackResponseStatus.MissingCreatedOutputContainer;
        }

        ItemStack? item = ItemPalette.GetCreativeItem(action.CreativeItemNetworkId);
        if (item is null)
        {
            return ItemStackResponseStatus.FailedToCraftCreative;
        }

        cursor.SetItem(0, item);
        AddChangedSlot(
            changedContainers,
            new FullContainerName { ContainerId = (byte)ContainerId.Cursor },
            cursor,
            0,
            0);

        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus SwapItems(
        Player.Player player,
        SwapStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        if (!TryResolveSlot(player, action.Source, out Container sourceContainer, out int sourceSlot) ||
            !TryResolveSlot(player, action.Destination, out Container destinationContainer, out int destinationSlot))
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
        Player.Player player,
        DropStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        if (!TryResolveSlot(player, action.Source, out Container container, out int slot))
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

        _ = player.DropItem(removedItem);

        AddChangedSlot(changedContainers, action.Source.Container, container, action.Source.Slot, slot);

        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus RemoveDestroyedItem(
        Player.Player player,
        DestroyStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        if (!TryResolveSlot(player, action.Source, out Container container, out int slot))
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

    private static bool TryResolveSlot(Player.Player player, StackRequestSlotInfo requestSlot, out Container container, out int slot)
    {
        container = null!;
        slot = -1;
        FullContainerName containerName = requestSlot.Container;
        Container? resolved = GetContainer(player, containerName, requestSlot.Slot);
        if (resolved is null)
        {
            return false;
        }

        int resolvedSlot = ResolveSlotIndex(player, containerName, resolved, requestSlot.Slot);
        if (resolvedSlot < 0 || resolvedSlot >= resolved.GetSize())
        {
            return false;
        }

        container = resolved;
        slot = resolvedSlot;
        return true;
    }

    private static int ResolveSlotIndex(Player.Player player, FullContainerName containerName, Container container, int slot)
    {
        if (containerName.ContainerId is (byte)ContainerId.Armor or 12 or (byte)ContainerId.Inventory or (byte)ContainerId.Hotbar or (byte)ContainerId.FixedInventory or (byte)ContainerId.Offhand)
        {
            return StorageSlot(player, containerName, slot);
        }

        if (containerName.ContainerId == (byte)ContainerId.DynamicContainer || containerName.ContainerId == (byte)ContainerId.Barrel || containerName.ContainerId == (byte)ContainerId.InventoryUi)
        {
            if (container.Type != ContainerType.Inventory)
            {
                if (slot >= 0 && slot < container.GetSize())
                {
                    return slot;
                }

                if (container.GetSize() == 27 && slot is >= 27 and <= 53)
                {
                    return slot - 27;
                }
            }

            return StorageSlot(player, containerName, slot);
        }

        return slot;
    }

    private static bool TryFindSlotByStackNetworkId(Container container, int stackNetworkId, out int slot)
    {
        slot = -1;
        if (stackNetworkId == 0)
        {
            return false;
        }

        for (int i = 0; i < container.GetSize(); i++)
        {
            ItemStack? item = container.GetItem(i);
            if (item is null)
            {
                continue;
            }

            if (item.NetworkStackId == stackNetworkId)
            {
                slot = i;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetOpenedDynamicContainer(Player.Player player, FullContainerName name, out Container container)
    {
        container = null!;
        if (name.ContainerId != (byte)ContainerId.DynamicContainer)
        {
            return false;
        }

        if (name.DynamicContainerId.HasValue)
        {
            if (!player.TryGetOpenContainer((int)name.DynamicContainerId.Value, out Container? opened) || opened is null || opened.Type == ContainerType.Inventory)
            {
                return false;
            }

            container = opened;
            return true;
        }

        Container? singleOpened = null;
        foreach ((_, Container opened) in player.openedContainers)
        {
            if (opened.Type == ContainerType.Inventory)
            {
                continue;
            }

            if (singleOpened is not null)
            {
                return false;
            }

            singleOpened = opened;
        }

        if (singleOpened is null)
        {
            return false;
        }

        container = singleOpened;
        return true;
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
            CraftCreativeStackRequestAction craftCreative =>
                $"craft_creative creative={craftCreative.CreativeItemNetworkId} count={craftCreative.NumberOfCrafts}",
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










