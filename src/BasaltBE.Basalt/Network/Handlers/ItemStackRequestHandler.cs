using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BasaltBE.Logging;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class ItemStackRequestHandler {
    public void Handle(NethernetPeer peer, Player? player, ItemStackRequestPacket packet) {
        if (player is null)
            throw new InvalidDataException("Item stack request arrived before player login.");
        if (player.Disposed)
            return;

        ItemStackResponseInfo[] responses = new ItemStackResponseInfo[packet.Requests.Length];
        for (int i = 0; i < packet.Requests.Length; i++) {
            responses[i] = ProcessRequest(player, packet.Requests[i]);
            ItemStackResponseInfo response = responses[i];
            if (response.Result != ItemStackNetResult.Success)
                Logger.Scoped("Network").Warn(
                    $"Rejected item stack request {response.ClientRequestId} from {player.Username}: {response.Result}."
                );
        }

        player.Network.SendPacket(player, new ItemStackResponsePacket {
            Responses = responses
        });
    }

    internal static ItemStackResponseInfo ProcessRequest(Player player, ItemStackRequestData request) {
        lock (player.Sync) {
            PlayerInventoryTrait? inventory = player.GetTrait<PlayerInventoryTrait>();
            if (!player.Spawned || inventory is null)
                return ErrorResponse(request.ClientRequestId);

            ItemStackResponseInfo response = ProcessRequest(inventory, request);
            if (response.Result != ItemStackNetResult.Success) {
                inventory.Inventory.Update();
                inventory.Cursor.Update();
            }

            return response;
        }
    }

    internal static ItemStackResponseInfo ProcessRequest(
        Container inventory,
        Container cursor,
        ItemStackRequestData request
    ) {
        return ProcessRequest(inventory, cursor, request, null);
    }

    private static ItemStackResponseInfo ProcessRequest(PlayerInventoryTrait inventory, ItemStackRequestData request) =>
        ProcessRequest(inventory.Inventory, inventory.Cursor, request, inventory);

    private static ItemStackResponseInfo ProcessRequest(
        Container inventory,
        Container cursor,
        ItemStackRequestData request,
        PlayerInventoryTrait? inventoryTrait
    ) {
        int clientRequestNetId = (request.ClientRequestId >> 1) ^ -(request.ClientRequestId & 1);
        RequestState state = new(inventory, cursor, clientRequestNetId, inventoryTrait);
        foreach (ItemStackRequestActionData action in request.Actions) {
            if (!Apply(state, action)) {
                return new ItemStackResponseInfo {
                    Result = ItemStackNetResult.Error,
                    ClientRequestId = request.ClientRequestId
                };
            }
        }

        foreach (SlotKey key in state.ChangedSlots) {
            key.Container.SetItem(key.Slot, state.Staged[key]);
            inventoryTrait?.RememberClientRequestNetId(
                key.Container,
                key.Slot,
                clientRequestNetId,
                state.Staged[key]
            );
        }

        Dictionary<ContainerNameKey, List<ItemStackResponseSlotInfo>> grouped = [];
        foreach (ResponseSlotKey responseKey in state.ResponseSlots) {
            ContainerNameKey name = new(responseKey.ContainerName, responseKey.DynamicId);
            if (!grouped.TryGetValue(name, out List<ItemStackResponseSlotInfo>? slots)) {
                slots = [];
                grouped.Add(name, slots);
            }

            ItemStack? item = state.Staged[responseKey.SlotKey];
            slots.Add(new ItemStackResponseSlotInfo {
                RequestedSlot = checked((byte)responseKey.SlotKey.Slot),
                Slot = checked((byte)responseKey.SlotKey.Slot),
                Amount = checked((byte)(item?.StackSize ?? 0)),
                ItemStackNetId = item?.NetId,
                CustomName = new RedactableString(),
                DurabilityCorrection = 0
            });
        }

        ItemStackResponseContainerInfo[] containers = new ItemStackResponseContainerInfo[grouped.Count];
        int index = 0;
        foreach (KeyValuePair<ContainerNameKey, List<ItemStackResponseSlotInfo>> entry in grouped) {
            containers[index++] = new ItemStackResponseContainerInfo {
                ContainerName = new FullContainerName {
                    ContainerName = entry.Key.Name,
                    DynamicId = entry.Key.DynamicId
                },
                Slots = [.. entry.Value]
            };
        }

        return new ItemStackResponseInfo {
            Result = ItemStackNetResult.Success,
            ClientRequestId = request.ClientRequestId,
            Containers = containers
        };
    }

    private static bool Apply(RequestState state, ItemStackRequestActionData action) => action switch {
        TakeActionData take => Transfer(state, take.Amount, take.Source, take.Destination),
        PlaceActionData place => Transfer(state, place.Amount, place.Source, place.Destination),
        SwapActionData swap => Swap(state, swap.Source, swap.Destination),
        DestroyActionData destroy => Remove(state, destroy.Amount, destroy.Source),
        ConsumeActionData consume => Remove(state, consume.Amount, consume.Source),
        _ => false
    };

    private static bool Transfer(
        RequestState state,
        byte amount,
        ItemStackSlotInfoData sourceInfo,
        ItemStackSlotInfoData destinationInfo
    ) {
        if (amount == 0 ||
            !TryGetItem(state, sourceInfo, out SlotKey sourceKey, out ItemStack? sourceItem) ||
            !TryGetItem(state, destinationInfo, out SlotKey destinationKey, out ItemStack? destinationItem)) {
            return false;
        }

        if (sourceKey == destinationKey)
            return true;
        if (sourceItem is null || amount > sourceItem.StackSize)
            return false;

        if (destinationItem is null) {
            ItemStack moved = sourceItem.Clone(amount, cloneNbt: amount != sourceItem.StackSize);
            SetItem(state, destinationKey, moved, destinationInfo.ContainerName.ContainerName, destinationInfo.ContainerName.DynamicId);
            SetItem(state, sourceKey, amount == sourceItem.StackSize
                ? null
                : sourceItem.Clone((ushort)(sourceItem.StackSize - amount)),
                sourceInfo.ContainerName.ContainerName,
                sourceInfo.ContainerName.DynamicId);
            return true;
        }

        if (!sourceItem.CanStackWith(destinationItem))
            return false;

        int available = destinationItem.Type.MaxAmount - destinationItem.StackSize;
        if (available < amount)
            return false;

        destinationItem.SetStackSize((ushort)(destinationItem.StackSize + amount));
        sourceItem.SetStackSize((ushort)(sourceItem.StackSize - amount));
        SetItem(
            state,
            destinationKey,
            destinationItem,
            destinationInfo.ContainerName.ContainerName,
            destinationInfo.ContainerName.DynamicId
        );
        SetItem(
            state,
            sourceKey,
            sourceItem.StackSize == 0 ? null : sourceItem,
            sourceInfo.ContainerName.ContainerName,
            sourceInfo.ContainerName.DynamicId
        );
        return true;
    }

    private static bool Swap(
        RequestState state,
        ItemStackSlotInfoData sourceInfo,
        ItemStackSlotInfoData destinationInfo
    ) {
        if (!TryGetItem(state, sourceInfo, out SlotKey sourceKey, out ItemStack? sourceItem) ||
            !TryGetItem(state, destinationInfo, out SlotKey destinationKey, out ItemStack? destinationItem)) {
            return false;
        }

        if (sourceKey == destinationKey)
            return true;

        SetItem(state, sourceKey, destinationItem, sourceInfo.ContainerName.ContainerName, sourceInfo.ContainerName.DynamicId);
        SetItem(state, destinationKey, sourceItem, destinationInfo.ContainerName.ContainerName, destinationInfo.ContainerName.DynamicId);
        return true;
    }

    private static bool Remove(RequestState state, byte amount, ItemStackSlotInfoData sourceInfo) {
        if (amount == 0 ||
            !TryGetItem(state, sourceInfo, out SlotKey sourceKey, out ItemStack? sourceItem) ||
            sourceItem is null || amount > sourceItem.StackSize) {
            return false;
        }

        ushort remaining = (ushort)(sourceItem.StackSize - amount);
        SetItem(
            state,
            sourceKey,
            remaining == 0 ? null : sourceItem.Clone(remaining),
            sourceInfo.ContainerName.ContainerName,
            sourceInfo.ContainerName.DynamicId
        );
        return true;
    }

    private static bool TryGetItem(
        RequestState state,
        ItemStackSlotInfoData info,
        out SlotKey key,
        out ItemStack? item
    ) {
        if (!ResolveContainer(state, info.ContainerName, out Container? container) ||
            container is null ||
            (info.ContainerName.ContainerName == ContainerEnumName.Hotbar && info.Slot >= 9) ||
            info.Slot >= container.ContainerSize) {
            key = default;
            item = null;
            return false;
        }

        key = new SlotKey(container, info.Slot);
        if (!state.Staged.TryGetValue(key, out item)) {
            item = container.GetItem(info.Slot);
            if ((item?.NetId ?? 0) != info.NetIdVariant &&
                !(state.InventoryTrait?.MatchesClientRequestNetId(container, info.Slot, info.NetIdVariant, item) ?? false)) {
                return false;
            }
            item = item?.Clone();
            state.Staged.Add(key, item);
            return true;
        }

        return (item?.NetId ?? 0) == info.NetIdVariant ||
            state.StagedClientRequestNetIds.TryGetValue(key, out int clientRequestNetId) &&
            clientRequestNetId == info.NetIdVariant;
    }

    private static bool ResolveContainer(RequestState state, FullContainerName name, out Container? container) {
        container = null;
        if (name.DynamicId is uint dynamicId && dynamicId != 0)
            return false;

        container = name.ContainerName switch {
            ContainerEnumName.Inventory or ContainerEnumName.Hotbar or ContainerEnumName.CombinedHotbarAndInventory => state.Inventory,
            ContainerEnumName.Cursor => state.Cursor,
            _ => null
        };
        return container is not null;
    }

    private static void SetItem(RequestState state, SlotKey key, ItemStack? item, ContainerEnumName name, uint? dynamicId) {
        state.Staged[key] = item;
        state.StagedClientRequestNetIds[key] = state.ClientRequestNetId;
        if (state.ChangedSlotSet.Add(key))
            state.ChangedSlots.Add(key);

        ResponseSlotKey responseKey = new(key, name, dynamicId);
        if (state.ResponseSlotSet.Add(responseKey))
            state.ResponseSlots.Add(responseKey);
    }

    private static ItemStackResponseInfo ErrorResponse(int requestId) => new() {
        Result = ItemStackNetResult.Error,
        ClientRequestId = requestId
    };

    private sealed class RequestState(
        Container inventory,
        Container cursor,
        int clientRequestNetId,
        PlayerInventoryTrait? inventoryTrait
    ) {
        internal readonly Container Inventory = inventory;
        internal readonly Container Cursor = cursor;
        internal readonly int ClientRequestNetId = clientRequestNetId;
        internal readonly PlayerInventoryTrait? InventoryTrait = inventoryTrait;
        internal readonly Dictionary<SlotKey, ItemStack?> Staged = [];
        internal readonly Dictionary<SlotKey, int> StagedClientRequestNetIds = [];
        internal readonly List<SlotKey> ChangedSlots = [];
        internal readonly HashSet<SlotKey> ChangedSlotSet = [];
        internal readonly List<ResponseSlotKey> ResponseSlots = [];
        internal readonly HashSet<ResponseSlotKey> ResponseSlotSet = [];
    }

    private readonly record struct SlotKey(Container Container, int Slot);
    private readonly record struct ResponseSlotKey(SlotKey SlotKey, ContainerEnumName ContainerName, uint? DynamicId);
    private readonly record struct ContainerNameKey(ContainerEnumName Name, uint? DynamicId);
}
