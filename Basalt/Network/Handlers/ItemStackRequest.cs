namespace Basalt.Core.Network.Handlers;

using Basalt.Core.Containers;
using Basalt.Core.Crafting;
using Basalt.Core.Item;
using Basalt.Core.Entities.Traits;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public static class ItemStackRequest {
    private const byte Success = 0;
    private const byte Error = 1;

    public static void Handle(Server server, NetworkConnection connection, ItemStackRequestPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player) ||
            player.Dimension is not { } dimension ||
            !dimension.TryEnqueue(player, () => Process(server, connection, player, packet))) {
            return;
        }
    }

    private static void Process(
        Server server,
        NetworkConnection connection,
        Player.Player player,
        ItemStackRequestPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? current) ||
            !ReferenceEquals(current, player)) {
            return;
        }

        ItemStackResponseInfo[] responses = new ItemStackResponseInfo[packet.Requests.Length];
        for (int index = 0; index < packet.Requests.Length; index++) {
            responses[index] = ProcessRequest(player, packet.Requests[index]);
        }

        server.Network.QueuePacket(connection, new ItemStackResponsePacket { Responses = responses });
    }

    internal static ItemStackResponseInfo ProcessRequestFromAuthInput(Player.Player player, ItemStackRequestData request) =>
        ProcessRequest(player, request);

    private static ItemStackResponseInfo ProcessRequest(Player.Player player, ItemStackRequestData request) {
        Dictionary<ContainerEnumName, ItemStackResponseContainerInfo> changes = [];
        byte result = Success;

        foreach (ItemStackRequestAction action in request.Actions) {
            if (!HandleAction(player, action, changes)) {
                Logger.Warn(
                    "ItemStackRequest rejected player:{0} action:{1} source:{2}:{3} destination:{4}:{5}",
                    player.Username,
                    action.Type,
                    action.Source.Container.ContainerName,
                    action.Source.Slot,
                    action.Destination.Container.ContainerName,
                    action.Destination.Slot);
                result = Error;
                break;
            }
        }

        return new ItemStackResponseInfo {
            Result = result,
            ClientRequestId = request.ClientRequestId,
            Containers = changes.Count == 0 ? [] : [.. changes.Values]
        };
    }

    private static bool HandleAction(Player.Player player, ItemStackRequestAction action,
        Dictionary<ContainerEnumName, ItemStackResponseContainerInfo> changes) => action.Type switch {
        ItemStackRequestActionType.Take or ItemStackRequestActionType.Place or
        ItemStackRequestActionType.PlaceInItemContainer or ItemStackRequestActionType.TakeFromItemContainer =>
            Transfer(player, action, changes),
        ItemStackRequestActionType.Swap => Swap(player, action, changes),
        ItemStackRequestActionType.Drop => Drop(player, action, changes),
        ItemStackRequestActionType.Destroy or ItemStackRequestActionType.Consume => Remove(player, action, changes),
        ItemStackRequestActionType.CraftCreative => CreateCreativeItem(player, action),
        ItemStackRequestActionType.Create or ItemStackRequestActionType.CraftResults or
        ItemStackRequestActionType.CraftNonImplemented => true,
        ItemStackRequestActionType.CraftRecipe or ItemStackRequestActionType.CraftRecipeAuto =>
            PrepareRecipeOutput(player, action),
        _ => false
    };

    private static bool CreateCreativeItem(Player.Player player, ItemStackRequestAction action) {
        if (player.Gamemode != GameType.Creative ||
            player.GetTrait<Basalt.Core.Player.Traits.PlayerCursorTrait>() is not { } cursor ||
            cursor.Container.GetItem(0) is not null ||
            ItemPalette.GetCreativeItem(action.CreativeItemNetId) is not { } item) {
            return false;
        }

        cursor.Container.SetItem(0, item);
        return true;
    }

    private static bool Transfer(Player.Player player, ItemStackRequestAction action,
        Dictionary<ContainerEnumName, ItemStackResponseContainerInfo> changes) {
        if (!ResolveSlot(player, action.Source, out Container source, out int sourceSlot) ||
            !ResolveSlot(player, action.Destination, out Container destination, out int destinationSlot)) {
            return false;
        }

        ItemStack? sourceItem = source.GetItem(sourceSlot);
        if (sourceItem is null) {
            return false;
        }

        int amount = Math.Min((int)sourceItem.StackSize, Math.Max(1, (int)action.Amount));
        ItemStack? destinationItem = destination.GetItem(destinationSlot);
        if (destinationItem is null) {
            ItemStack? moved = source.TakeItem(sourceSlot, amount);
            if (moved is null) {
                return false;
            }

            destination.SetItem(destinationSlot, moved);
        }
        else {
            if (!sourceItem.CanStackWith(destinationItem)) {
                return false;
            }

            int available = destinationItem.Type.MaxStackSize - destinationItem.StackSize;
            if (available <= 0) {
                return false;
            }

            amount = Math.Min(amount, available);
            sourceItem.DecrementStack((ushort)amount);
            destinationItem.IncrementStack((ushort)amount);
            if (sourceItem.StackSize == 0) {
                source.ClearSlot(sourceSlot);
            }
            else {
                source.UpdateSlot(sourceSlot);
            }

            destination.UpdateSlot(destinationSlot);
        }

        Record(changes, action.Source, source, sourceSlot);
        Record(changes, action.Destination, destination, destinationSlot);
        return true;
    }

    private static bool Swap(Player.Player player, ItemStackRequestAction action,
        Dictionary<ContainerEnumName, ItemStackResponseContainerInfo> changes) {
        if (!ResolveSlot(player, action.Source, out Container source, out int sourceSlot) ||
            !ResolveSlot(player, action.Destination, out Container destination, out int destinationSlot)) {
            return false;
        }

        source.SwapItems(sourceSlot, destinationSlot, destination);
        Record(changes, action.Source, source, sourceSlot);
        Record(changes, action.Destination, destination, destinationSlot);
        return true;
    }

    private static bool Drop(Player.Player player, ItemStackRequestAction action,
        Dictionary<ContainerEnumName, ItemStackResponseContainerInfo> changes) {
        if (!ResolveSlot(player, action.Source, out Container container, out int slot)) {
            return false;
        }

        ItemStack? item = container.GetItem(slot);
        int amount = Math.Min(action.Amount, item?.StackSize ?? 0);
        if (item is null || amount == 0 || !player.DropItem(item.Clone((ushort)amount))) {
            return false;
        }

        container.TakeItem(slot, amount);
        Record(changes, action.Source, container, slot);
        return true;
    }

    private static bool Remove(Player.Player player, ItemStackRequestAction action,
        Dictionary<ContainerEnumName, ItemStackResponseContainerInfo> changes) {
        if (!ResolveSlot(player, action.Source, out Container container, out int slot) ||
            container.GetItem(slot) is not ItemStack item) {
            return false;
        }

        container.TakeItem(slot, Math.Min(action.Amount, item.StackSize));
        Record(changes, action.Source, container, slot);
        return true;
    }

    private static bool ResolveSlot(Player.Player player, SlotInfoData slotInfo,
        out Container container, out int slot) {
        container = player.GetContainer(slotInfo.Container)!;
        slot = slotInfo.Slot;
        if (container is null) {
            return false;
        }

        if (slotInfo.Container.ContainerName == ContainerEnumName.CraftingInputContainer &&
            container.Type == ContainerType.NONE) {
            slot = Basalt.Core.Player.Traits.PlayerCraftingGridTrait.MapSlot(slotInfo.Slot);
        }
        else if (slotInfo.Container.ContainerName == ContainerEnumName.CraftingInputContainer &&
                 container.Type == ContainerType.WORKBENCH && slotInfo.Slot >= 32) {
            slot = slotInfo.Slot - 32;
        }

        if (slotInfo.Container.ContainerName == ContainerEnumName.CreatedOutputContainer) {
            slot = 0;
        }

        return slot >= 0 && slot < container.GetSize();
    }

    private static bool PrepareRecipeOutput(Player.Player player, ItemStackRequestAction action) {
        CraftingRecipe? recipe = CraftingRegistry.Instance.GetByNetworkId(action.RecipeNetId);
        if (recipe is null || recipe.Result.Count <= 0) {
            Logger.Warn("Crafting recipe unavailable player:{0} action:{1} recipe:{2} crafts:{3}",
                player.Username,
                action.Type,
                action.RecipeNetId,
                action.NumberOfRequestedCrafts);
            return false;
        }

        try {
            Basalt.Core.Player.Traits.PlayerCursorTrait? cursor =
                player.GetTrait<Basalt.Core.Player.Traits.PlayerCursorTrait>();
            if (cursor is null) {
                Logger.Warn("Crafting cursor unavailable player:{0} action:{1}", player.Username, action.Type);
                return false;
            }

            int count = recipe.Result.Count * Math.Max(1, (int)action.NumberOfRequestedCrafts);
            ItemType? resultType = ItemType.Get(recipe.Result.Item);
            if (resultType is null && !recipe.Result.Item.Contains(':')) {
                resultType = ItemType.Get("minecraft:" + recipe.Result.Item);
            }

            if (resultType is null) {
                return false;
            }

            ItemStack result = new(resultType, (ushort)count, (uint)recipe.Result.Data);
            ItemStack? existing = cursor.Container.GetItem(0);
            if (existing is null) {
                cursor.Container.SetItem(0, result);
            }
            else if (existing.CanStackWith(result)) {
                existing.IncrementStack(result.StackSize);
                cursor.Container.UpdateSlot(0);
            }
            else {
                Logger.Warn("Crafting cursor occupied player:{0} item:{1} result:{2}",
                    player.Username,
                    existing.Identifier,
                    result.Identifier);
                return false;
            }

            return true;
        }
        catch (InvalidOperationException) {
            return false;
        }
    }

    private static void Record(Dictionary<ContainerEnumName, ItemStackResponseContainerInfo> changes,
        SlotInfoData slotInfo, Container container, int slot) {
        ItemStack? item = container.GetItem(slot);
        if (!changes.TryGetValue(slotInfo.Container.ContainerName, out ItemStackResponseContainerInfo? response)) {
            response = new ItemStackResponseContainerInfo { Container = slotInfo.Container };
            changes.Add(slotInfo.Container.ContainerName, response);
        }

        response.Slots = [new ItemStackResponseSlotInfo {
            Slot = (byte)slotInfo.Slot,
            HotbarSlot = slotInfo.Slot,
            Count = (byte)(item?.StackSize ?? 0),
            ItemStackId = item?.NetworkStackId,
            CustomName = string.Empty,
            FilteredCustomName = string.Empty,
            DurabilityCorrection = 0
        }];
    }
}
