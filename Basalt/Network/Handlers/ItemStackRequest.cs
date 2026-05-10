using Basalt.Containers;
using Basalt.Core;
using Basalt.Item;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class ItemStackRequest
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ItemStackRequestPacket packet = new();
        packet.Deserialize(packetBuffer);

        if (!server.Players.TryGetValue(connection, out Player? player) || packet.Requests.Count == 0)
        {
            return;
        }

        List<ItemStackResponse> responses = new(packet.Requests.Count);
        using (Container.SuppressPackets())
        {
            foreach (Protocol.Types.ItemStackRequest request in packet.Requests)
            {
                try
                {
                    ItemStackResponse response = HandleRequest(player, request);
                    responses.Add(response);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"ItemStackRequest exception: request={request.RequestId} {exception}");
                    responses.Add(new ItemStackResponse
                    {
                        Status = ItemStackResponseStatus.Error,
                        RequestId = request.RequestId,
                        ContainerInfo = []
                    });
                }
            }
        }

        server.Network.SendPacket(connection, new ItemStackResponsePacket { Responses = responses });
    }

    private static ItemStackResponse HandleRequest(Player player, Protocol.Types.ItemStackRequest request)
    {
        Dictionary<string, StackResponseContainerInfo> containers = [];
        ItemStackResponseStatus status = ItemStackResponseStatus.Ok;
        if (request.Actions.Count > 0)
        {
            switch (request.Actions[0])
            {
                case TransferStackRequestAction transfer:
                    Console.WriteLine($"ItemStackRequest {request.RequestId}: transfer src={transfer.Source.Container.ContainerId} dst={transfer.Destination.Container.ContainerId}");
                    break;
                case SwapStackRequestAction swap:
                    Console.WriteLine($"ItemStackRequest {request.RequestId}: swap src={swap.Source.Container.ContainerId} dst={swap.Destination.Container.ContainerId}");
                    break;
                case DropStackRequestAction drop:
                    Console.WriteLine($"ItemStackRequest {request.RequestId}: drop src={drop.Source.Container.ContainerId}");
                    break;
            }
        }

        foreach (IStackRequestAction action in request.Actions)
        {
            status = action switch
            {
                TransferStackRequestAction transfer => HandleTransferAction(player, transfer, containers),
                SwapStackRequestAction swap => HandleSwapAction(player, swap, containers),
                DropStackRequestAction drop => HandleDropAction(player, drop, containers),
                DestroyStackRequestAction destroy => HandleDestroyAction(player, destroy, containers),
                EmptyStackRequestAction => ItemStackResponseStatus.Ok,
                CraftResultsDeprecatedStackRequestAction => ItemStackResponseStatus.Ok,
                _ => ItemStackResponseStatus.InvalidRequestActionType
            };

            if (status != ItemStackResponseStatus.Ok)
            {
                Console.WriteLine($"ItemStackRequest failed: request={request.RequestId} status={status}");
                return new ItemStackResponse
                {
                    Status = status,
                    RequestId = request.RequestId,
                    ContainerInfo = []
                };
            }
        }

        if (containers.Count == 0)
        {
            return new ItemStackResponse
            {
                Status = ItemStackResponseStatus.Error,
                RequestId = request.RequestId,
                ContainerInfo = []
            };
        }

        return new ItemStackResponse
        {
            Status = ItemStackResponseStatus.Ok,
            RequestId = request.RequestId,
            ContainerInfo = [.. containers.Values]
        };
    }

    private static ItemStackResponseStatus HandleTransferAction(Player player, TransferStackRequestAction action, Dictionary<string, StackResponseContainerInfo> containers)
    {
        if (!player.TryResolveContainerSlot(action.Source.Container, action.Source.Slot, out Container? source, out int sourceSlot) ||
            !player.TryResolveContainerSlot(action.Destination.Container, action.Destination.Slot, out Container? destination, out int destinationSlot) ||
            source is null || destination is null)
        {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        if (!IsValidSlot(source, sourceSlot) || !IsValidSlot(destination, destinationSlot))
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        ItemStack? sourceItem = source.GetItem(sourceSlot);
        if (sourceItem is null)
        {
            return ItemStackResponseStatus.FailedToMatchExpectedSlotConsumedItem;
        }

        int amount = Math.Max(1, (int)action.Count);
        amount = Math.Min(amount, sourceItem.StackSize);

        ItemStack? destinationItem = destination.GetItem(destinationSlot);
        if (destinationItem is not null)
        {
            if (!AreStackable(sourceItem, destinationItem))
            {
                return ItemStackResponseStatus.CannotPlaceItem;
            }

            int available = destinationItem.Type.MaxStackSize - destinationItem.StackSize;
            if (available <= 0)
            {
                return ItemStackResponseStatus.CannotPlaceItem;
            }

            amount = Math.Min(amount, available);
            destinationItem.IncrementStack((ushort)amount);
            sourceItem.DecrementStack((ushort)amount);
            if (sourceItem.StackSize == 0)
            {
                source.ClearSlot(sourceSlot);
            }
            else
            {
                source.UpdateSlot(sourceSlot);
            }

            destination.UpdateSlot(destinationSlot);
        }
        else
        {
            ItemStack moved = source.TakeItem(sourceSlot, amount) ?? ItemStack.Empty();
            if (moved.Type == ItemType.Air || moved.StackSize == 0)
            {
                return ItemStackResponseStatus.CannotRemoveItem;
            }

            destination.SetItem(destinationSlot, moved);
        }

        AddContainerSlotInfo(containers, action.Source.Container, source, action.Source.Slot, sourceSlot);
        AddContainerSlotInfo(containers, action.Destination.Container, destination, action.Destination.Slot, destinationSlot);
        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus HandleSwapAction(Player player, SwapStackRequestAction action, Dictionary<string, StackResponseContainerInfo> containers)
    {
        if (!player.TryResolveContainerSlot(action.Source.Container, action.Source.Slot, out Container? source, out int sourceSlot) ||
            !player.TryResolveContainerSlot(action.Destination.Container, action.Destination.Slot, out Container? destination, out int destinationSlot) ||
            source is null || destination is null)
        {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }
        if (!IsValidSlot(source, sourceSlot) || !IsValidSlot(destination, destinationSlot))
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        source.SwapItems(sourceSlot, destinationSlot, destination);
        AddContainerSlotInfo(containers, action.Source.Container, source, action.Source.Slot, sourceSlot);
        AddContainerSlotInfo(containers, action.Destination.Container, destination, action.Destination.Slot, destinationSlot);
        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus HandleDropAction(Player player, DropStackRequestAction action, Dictionary<string, StackResponseContainerInfo> containers)
    {
        if (!player.TryResolveContainerSlot(action.Source.Container, action.Source.Slot, out Container? source, out int slot) || source is null)
        {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }
        if (!IsValidSlot(source, slot))
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        int amount = Math.Max(1, (int)action.Count);
        ItemStack? removed = source.TakeItem(slot, amount);
        if (removed is null)
        {
            return ItemStackResponseStatus.CannotDropItem;
        }

        AddContainerSlotInfo(containers, action.Source.Container, source, action.Source.Slot, slot);
        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus HandleDestroyAction(Player player, DestroyStackRequestAction action, Dictionary<string, StackResponseContainerInfo> containers)
    {
        if (!player.TryResolveContainerSlot(action.Source.Container, action.Source.Slot, out Container? source, out int slot) || source is null)
        {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }
        if (!IsValidSlot(source, slot))
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        int amount = Math.Max(1, (int)action.Count);
        ItemStack? removed = source.TakeItem(slot, amount);
        if (removed is null)
        {
            return ItemStackResponseStatus.CannotDestroyItem;
        }

        AddContainerSlotInfo(containers, action.Source.Container, source, action.Source.Slot, slot);
        return ItemStackResponseStatus.Ok;
    }

    private static bool IsValidSlot(Container container, int slot)
    {
        return slot >= 0 && slot < container.GetSize();
    }

    private static bool AreStackable(ItemStack a, ItemStack b)
    {
        return a.CanStackWith(b);
    }

    private static void AddContainerSlotInfo(
        Dictionary<string, StackResponseContainerInfo> containers,
        FullContainerName containerName,
        Container container,
        int responseSlot,
        int storageSlot
    )
    {
        string key = containerName.DynamicContainerId.HasValue
            ? $"{containerName.ContainerId}:{containerName.DynamicContainerId.Value}"
            : containerName.ContainerId.ToString();

        if (!containers.TryGetValue(key, out StackResponseContainerInfo? info))
        {
            info = new StackResponseContainerInfo
            {
                Container = new FullContainerName
                {
                    ContainerId = containerName.ContainerId,
                    DynamicContainerId = new OptionalValue<uint>
                    {
                        HasValue = containerName.DynamicContainerId.HasValue,
                        Value = containerName.DynamicContainerId.Value
                    }
                },
                SlotInfo = []
            };
            containers[key] = info;
        }

        ItemStack? item = container.GetItem(storageSlot);
        info.SlotInfo.RemoveAll(entry => entry.Slot == responseSlot);
        info.SlotInfo.Add(new StackResponseSlotInfo
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
}
