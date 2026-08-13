namespace Basalt.Core.Network.Handlers;

using Basalt.Core.Containers;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
using Basalt.Core.Player.Traits;
using Basalt.RakNet;

using BedrockProtocol.Packets;
using BedrockProtocol.Types;
using BedrockProtocol.Enums;

public static class ItemStackRequest {
    public static void Handle(Server server, NetworkConnection connection, ItemStackRequestPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player) || packet.Requests.Count == 0) {
            return;
        }

        List<ItemStackResponseInfo> responses = new(packet.Requests.Count);

        foreach (BedrockProtocol.Types.ItemStackRequest request in packet.Requests) {
            try {
                responses.Add(ProcessRequest(player, request));
            }
            catch (Exception ex) {
                Logger.Err(string.Format("Exception on ItemStackRequest: {0} {1}", request.ClientRequestId, ex));
                responses.Add(ErrorResponse(request.ClientRequestId));
            }
        }

        server.Network.QueuePacket(connection, new ItemStackResponsePacket { Responses = responses });
    }

    /// <summary>
    /// Maps a creative item network ID to the server-assigned stack network ID for the current request.
    /// Populated by CraftCreative so that subsequent Transfer actions can match the item.
    /// </summary>
    [ThreadStatic]
    private static int _pendingCreativeStackId;

    [ThreadStatic]
    private static ItemStack? _pendingCreativeItem;

    [ThreadStatic]
    private static ItemStack? _pendingCraftResult;

    /// <summary>
    /// Entry point for processing ItemStackRequests embedded in PlayerAuthInput packets.
    /// </summary>
    internal static ItemStackResponseInfo ProcessRequestFromAuthInput(Player.Player player, BedrockProtocol.Types.ItemStackRequest request) {
        return ProcessRequest(player, request);
    }

    private static ItemStackResponseInfo ProcessRequest(Player.Player player, BedrockProtocol.Types.ItemStackRequest request) {
        Dictionary<string, ItemStackResponseContainerInfo> changed = [];
        _pendingCreativeStackId = 0;
        _pendingCreativeItem = null;
        _pendingCraftResult = null;

        foreach (ItemStackRequestActionVariant action in request.Actions) {
            ItemStackNetResult status = HandleAction(player, action, changed);

            if (status == ItemStackNetResult.Success) {
                continue;
            }

            ResyncContainers(player);

            return ErrorResponse(request.ClientRequestId, status);
        }

        return new ItemStackResponseInfo {
            Result = ItemStackNetResult.Success,
            ClientRequestId = request.ClientRequestId,
            Containers = changed.Count > 0 ? [.. changed.Values] : []
        };
    }

    private static ItemStackNetResult HandleAction(
        Player.Player player,
        ItemStackRequestActionVariant action,
        Dictionary<string, ItemStackResponseContainerInfo> changed) {
        return action switch {
            ItemStackRequestTakeAction take => HandleTransfer(player, take.Amount, take.Source, take.Destination, changed),
            ItemStackRequestPlaceAction place => HandleTransfer(player, place.Amount, place.Source, place.Destination, changed),
            ItemStackRequestSwapAction swap => HandleSwap(player, swap, changed),
            ItemStackRequestDropAction drop => HandleDrop(player, drop, changed),
            ItemStackRequestDestroyAction destroy => HandleDestroy(player, destroy, changed),
            ItemStackRequestConsumeAction consume => HandleConsume(player, consume, changed),
            ItemStackRequestCraftCreativeAction creative => HandleCraftCreative(player, creative, changed),
            ItemStackRequestCraftRecipeAction craft => HandleCraftRecipe(player, craft, changed),
            ItemStackRequestCraftRecipeAutoAction autoCraft => HandleCraftRecipe(
                player,
                autoCraft.RecipeNetId,
                autoCraft.NumberOfRequestedCrafts,
                changed),

            ItemStackRequestCreateAction => ItemStackNetResult.Success,
            ItemStackRequestCraftResultsDeprecatedAction => ItemStackNetResult.Success,
            ItemStackRequestCraftNonImplementedDeprecatedAction => ItemStackNetResult.Success,

            _ => ItemStackNetResult.InvalidRequestActionType
        };
    }

    private static ItemStackNetResult HandleTransfer(
        Player.Player player,
        byte requestedAmount,
        ItemStackRequestSlotInfo source,
        ItemStackRequestSlotInfo destination,
        Dictionary<string, ItemStackResponseContainerInfo> changed) {
        if (source.FullContainerName.ContainerName == ContainerEnumName.CreatedOutputContainer && _pendingCreativeItem is not null) {
            if (!TryResolveSlot(player, destination, out Container creativeDst, out int creativeDstSlot)) {
                return ItemStackNetResult.InvalidSourceContainer;
            }

            ItemStack item = _pendingCreativeItem;
            creativeDst.SetItem(creativeDstSlot, item);
            _pendingCreativeItem = null;
            RecordChange(changed, destination.FullContainerName, creativeDst, destination.Slot, creativeDstSlot);
            return ItemStackNetResult.Success;
        }

        if (source.FullContainerName.ContainerName == ContainerEnumName.CreatedOutputContainer && _pendingCraftResult is not null) {
            if (!TryResolveSlot(player, destination, out Container craftDst, out int craftDstSlot)) {
                return ItemStackNetResult.InvalidSourceContainer;
            }

            ItemStack item = _pendingCraftResult;
            ItemStack? existing = craftDst.GetItem(craftDstSlot);
            if (existing is not null) {
                if (!existing.CanStackWith(item)) {
                    int altSlot = ResolveDestinationSlot(craftDst, item, craftDstSlot);
                    if (altSlot < 0) {
                        return ItemStackNetResult.CannotPlaceItem;
                    }
                    craftDstSlot = altSlot;
                    existing = craftDst.GetItem(craftDstSlot);
                }

                if (existing is not null) {
                    int available = existing.Type.MaxStackSize - existing.StackSize;
                    if (available <= 0) {
                        int altSlot = ResolveDestinationSlot(craftDst, item, craftDstSlot);
                        if (altSlot < 0) {
                            return ItemStackNetResult.CannotPlaceItem;
                        }
                        craftDstSlot = altSlot;
                        existing = craftDst.GetItem(craftDstSlot);
                    }
                }

                if (existing is not null) {
                    int available = existing.Type.MaxStackSize - existing.StackSize;
                    if (available < item.StackSize) {
                        return ItemStackNetResult.CannotPlaceItem;
                    }

                    existing.IncrementStack(item.StackSize);
                    craftDst.UpdateSlot(craftDstSlot);
                }
                else {
                    craftDst.SetItem(craftDstSlot, item);
                }
            }
            else {
                craftDst.SetItem(craftDstSlot, item);
            }

            _pendingCraftResult = null;
            RecordChange(changed, destination.FullContainerName, craftDst, destination.Slot, craftDstSlot);
            return ItemStackNetResult.Success;
        }

        bool sourceResolved = TryResolveSlot(player, source, out Container srcContainer, out int srcSlot);
        bool destinationResolved = TryResolveSlot(player, destination, out Container dstContainer, out int dstSlot);
        if (!sourceResolved || !destinationResolved) {
            return ItemStackNetResult.InvalidSourceContainer;
        }

        ItemStack? srcItem = srcContainer.GetItem(srcSlot);

        if (srcItem is null && source.NetIdVariant != 0 &&
            TryFindSlotByStackNetworkId(srcContainer, source.NetIdVariant, out int correctedSlot)) {
            srcSlot = correctedSlot;
            srcItem = srcContainer.GetItem(srcSlot);
        }

        if (srcItem is null) {
            return ItemStackNetResult.FailedToMatchExpectedSlotConsumedItem;
        }

        int amount = Math.Clamp((int)requestedAmount, 1, srcItem.StackSize);

        if (destination.NetIdVariant == 0) {
            int resolved = ResolveDestinationSlot(dstContainer, srcItem, dstSlot);
            if (resolved >= 0) {
                dstSlot = resolved;
            }
        }

        ItemStack? dstItem = dstContainer.GetItem(dstSlot);

        if (dstItem is null) {
            // Move into empty slot
            ItemStack? taken = srcContainer.TakeItem(srcSlot, amount);
            if (taken is null || taken.StackSize == 0) {
                return ItemStackNetResult.CannotRemoveItem;
            }

            dstContainer.SetItem(dstSlot, taken);
        }
        else {
            if (!srcItem.CanStackWith(dstItem)) {
                return ItemStackNetResult.CannotPlaceItem;
            }

            int available = dstItem.Type.MaxStackSize - dstItem.StackSize;
            if (available <= 0) {
                return ItemStackNetResult.CannotPlaceItem;
            }

            amount = Math.Min(amount, available);
            srcItem.DecrementStack((ushort)amount);
            dstItem.IncrementStack((ushort)amount);

            if (srcItem.StackSize == 0) {
                srcContainer.ClearSlot(srcSlot);
            }
            else {
                srcContainer.UpdateSlot(srcSlot);
            }

            dstContainer.UpdateSlot(dstSlot);
        }

        RecordChange(changed, source.FullContainerName, srcContainer, source.Slot, srcSlot);
        RecordChange(changed, destination.FullContainerName, dstContainer, destination.Slot, dstSlot);
        return ItemStackNetResult.Success;
    }

    private static ItemStackNetResult HandleSwap(
        Player.Player player,
        ItemStackRequestSwapAction action,
        Dictionary<string, ItemStackResponseContainerInfo> changed) {
        if (!TryResolveSlot(player, action.Source, out Container srcContainer, out int srcSlot) ||
            !TryResolveSlot(player, action.Destination, out Container dstContainer, out int dstSlot)) {
            return ItemStackNetResult.InvalidSourceContainer;
        }

        srcContainer.SwapItems(srcSlot, dstSlot, dstContainer);

        RecordChange(changed, action.Source.FullContainerName, srcContainer, action.Source.Slot, srcSlot);
        RecordChange(changed, action.Destination.FullContainerName, dstContainer, action.Destination.Slot, dstSlot);
        return ItemStackNetResult.Success;
    }

    private static ItemStackNetResult HandleDrop(
        Player.Player player,
        ItemStackRequestDropAction action,
        Dictionary<string, ItemStackResponseContainerInfo> changed) {
        if (!TryResolveSlot(player, action.Source, out Container container, out int slot)) {
            return ItemStackNetResult.InvalidSourceContainer;
        }

        int amount = Math.Max(1, (int)action.Amount);
        ItemStack? item = container.GetItem(slot);
        if (item is null) {
            return ItemStackNetResult.CannotDropItem;
        }

        int count = Math.Min(amount, item.StackSize);
        ItemStack dropped = item.Clone((ushort)count);
        if (!player.DropItem(dropped)) {
            return ItemStackNetResult.CannotDropItem;
        }

        _ = container.TakeItem(slot, count);

        RecordChange(changed, action.Source.FullContainerName, container, action.Source.Slot, slot);
        return ItemStackNetResult.Success;
    }

    private static ItemStackNetResult HandleDestroy(
        Player.Player player,
        ItemStackRequestDestroyAction action,
        Dictionary<string, ItemStackResponseContainerInfo> changed) {
        if (!TryResolveSlot(player, action.Source, out Container container, out int slot)) {
            return ItemStackNetResult.InvalidSourceContainer;
        }

        int amount = Math.Max(1, (int)action.Amount);
        ItemStack? removed = container.TakeItem(slot, amount);
        if (removed is null) {
            return ItemStackNetResult.CannotDestroyItem;
        }

        RecordChange(changed, action.Source.FullContainerName, container, action.Source.Slot, slot);
        return ItemStackNetResult.Success;
    }

    private static ItemStackNetResult HandleConsume(
        Player.Player player,
        ItemStackRequestConsumeAction action,
        Dictionary<string, ItemStackResponseContainerInfo> changed) {
        if (!TryResolveSlot(player, action.Source, out Container container, out int slot)) {
            return ItemStackNetResult.InvalidSourceContainer;
        }

        ItemStack? item = container.GetItem(slot);
        if (item is null && action.Source.NetIdVariant != 0 &&
            TryFindSlotByStackNetworkId(container, action.Source.NetIdVariant, out int correctedSlot)) {
            slot = correctedSlot;
            item = container.GetItem(slot);
        }

        if (item is null || item.StackSize < action.Amount) {
            return ItemStackNetResult.FailedToMatchExpectedSlotConsumedItem;
        }

        int before = item.StackSize;
        if (action.Amount == item.StackSize) {
            container.ClearSlot(slot);
        }
        else {
            item.DecrementStack((ushort)action.Amount);
            container.UpdateSlot(slot);
        }

        RecordChange(changed, action.Source.FullContainerName, container, action.Source.Slot, slot);
        return ItemStackNetResult.Success;
    }

    private static ItemStackNetResult HandleCraftCreative(
        Player.Player player,
        ItemStackRequestCraftCreativeAction action,
        Dictionary<string, ItemStackResponseContainerInfo> changed) {
        if (player.Gamemode != GameType.Creative) {
            return ItemStackNetResult.PlayerNotInCreativeMode;
        }

        ItemStack? item = ItemPalette.GetCreativeItem(action.CreativeItemNetId);
        if (item is null) {
            return ItemStackNetResult.FailedToCraftCreative;
        }

        _pendingCreativeItem = item;
        _pendingCreativeStackId = item.NetworkStackId;
        return ItemStackNetResult.Success;
    }

    private static ItemStackNetResult HandleCraftRecipe(
        Player.Player player,
        ItemStackRequestCraftRecipeAction action,
        Dictionary<string, ItemStackResponseContainerInfo> changed) {
        return ProduceCraftResult(player, action.RecipeNetId, action.NumberOfRequestedCrafts);
    }

    private static ItemStackNetResult HandleCraftRecipe(
        Player.Player player,
        RecipeNetId recipeNetworkId,
        byte numberOfCrafts,
        Dictionary<string, ItemStackResponseContainerInfo> changed) {
        return ProduceCraftResult(player, recipeNetworkId, numberOfCrafts);
    }

    private static ItemStackNetResult ProduceCraftResult(
        Player.Player player,
        RecipeNetId recipeNetworkId,
        byte numberOfCrafts) {
        Crafting.CraftingRecipe? recipe = Crafting.CraftingRegistry.Instance.GetByNetworkId(GetRecipeNetworkId(recipeNetworkId));
        if (recipe is null) {
            Logger.Warn("Could not Find a Recipe for " + GetRecipeNetworkId(recipeNetworkId));
            return ItemStackNetResult.Error;
        }

        ItemType? resultType = ItemType.Get(recipe.Result.Item);
        if (resultType is null && !recipe.Result.Item.Contains(':')) {
            resultType = ItemType.Get("minecraft:" + recipe.Result.Item);
        }
        if (resultType is null) {
            Logger.Info("Result item for crafting was not found: " + recipe.Result.Item);
            return ItemStackNetResult.Error;
        }

        int craftCount = Math.Max(1, (int)numberOfCrafts);

        int totalCount = recipe.Result.Count * craftCount;
        ushort stackSize = (ushort)Math.Min(totalCount, resultType.MaxStackSize);

        _pendingCraftResult = new ItemStack(resultType, stackSize);
        return ItemStackNetResult.Success;
    }

    private static uint GetRecipeNetworkId(RecipeNetId value) {
        return value.RawId;
    }

    private static bool TryResolveSlot(Player.Player player, ItemStackRequestSlotInfo requestSlot, out Container container, out int slot) {
        container = null!;
        slot = -1;

        Container? resolved = ResolveContainer(player, requestSlot.FullContainerName, requestSlot.Slot);
        if (resolved is null) {
            return false;
        }

        int resolvedSlot = ResolveSlotIndex(player, requestSlot.FullContainerName, resolved, requestSlot.Slot);
        if (resolvedSlot < 0 || resolvedSlot >= resolved.GetSize()) {
            return false;
        }

        container = resolved;
        slot = resolvedSlot;
        return true;
    }

    private static Container? ResolveContainer(Player.Player player, FullContainerName name, int slot) {
        if (TryGetOpenedDynamicContainer(player, name, out Container openedContainer)) {
            if (slot < openedContainer.GetSize()) {
                return openedContainer;
            }

            return player.GetTrait<EntityInventoryTrait>()?.Container;
        }

        if (name.ContainerName == ContainerEnumName.DynamicContainer) {
            return null;
        }

        if (name.ContainerName is ContainerEnumName.RecipeEquipmentContainer
            or ContainerEnumName.RecipeBookContainer
            or ContainerEnumName.EnchantingInputContainer
            or ContainerEnumName.EnchantingMaterialContainer
            or ContainerEnumName.FurnaceFuelContainer
            or ContainerEnumName.FurnaceIngredientContainer
            or ContainerEnumName.BlastFurnaceIngredientContainer
            or ContainerEnumName.SmokerIngredientContainer
            or ContainerEnumName.FurnaceResultContainer) {
            foreach ((_, Container opened) in player.openedContainers) {
                if (opened.Type is ContainerType.FURNACE or ContainerType.BLAST_FURNACE or ContainerType.SMOKER) {
                    return opened;
                }
            }
        }

        return player.GetContainer(name);
    }

    private static int ResolveSlotIndex(Player.Player player, FullContainerName containerName, Container container, int slot) {
        if (containerName.ContainerName == ContainerEnumName.CreatedOutputContainer) {
            return 0;
        }

        if (containerName.ContainerName == ContainerEnumName.CraftingInputContainer) {
            if (container.Type == ContainerType.WORKBENCH) {
                return slot >= 32 ? slot - 32 : slot;
            }

            if (slot >= PlayerCraftingGridTrait.SlotOffset
                && slot < PlayerCraftingGridTrait.SlotOffset + PlayerCraftingGridTrait.GridSize) {
                return PlayerCraftingGridTrait.MapSlot(slot);
            }

            if (slot >= 0 && slot < PlayerCraftingGridTrait.GridSize) {
                return slot;
            }

            return -1;
        }

        if (containerName.ContainerName == ContainerEnumName.ArmorContainer) {
            return slot;
        }

        if (containerName.ContainerName == ContainerEnumName.OffhandContainer) {
            return slot;
        }

        if (containerName.ContainerName is ContainerEnumName.CombinedHotbarAndInventoryContainer
            or ContainerEnumName.InventoryContainer or ContainerEnumName.HotbarContainer) {
            return NormalizeInventorySlot(slot);
        }

        // Furnace and crafting UI slot IDs pass through directly.
        if (containerName.ContainerName is ContainerEnumName.RecipeEquipmentContainer
            or ContainerEnumName.RecipeBookContainer
            or ContainerEnumName.EnchantingInputContainer
            or ContainerEnumName.EnchantingMaterialContainer
            or ContainerEnumName.FurnaceFuelContainer
            or ContainerEnumName.FurnaceIngredientContainer
            or ContainerEnumName.BlastFurnaceIngredientContainer
            or ContainerEnumName.SmokerIngredientContainer
            or ContainerEnumName.FurnaceResultContainer) {
            return slot;
        }

        if (containerName.ContainerName is ContainerEnumName.DynamicContainer
            or ContainerEnumName.BarrelContainer
            or ContainerEnumName.LevelEntityContainer) {
            if (container.Type != ContainerType.INVENTORY) {
                if (slot >= 0 && slot < container.GetSize()) {
                    return slot;
                }

                // Some clients send offset slots for 27-slot containers (chests)
                if (container.GetSize() == 27 && slot is >= 27 and <= 53) {
                    return slot - 27;
                }
            }

            return NormalizeInventorySlot(slot);
        }

        return slot;
    }

    /// <summary>
    /// Converts client-side hotbar slot indices (36-44) back to storage-relative indices (0-8).
    /// </summary>
    private static int NormalizeInventorySlot(int slot) {
        return slot is >= 36 and <= 44 ? slot - 36 : slot;
    }

    private static int ResolveDestinationSlot(Container container, ItemStack sourceItem, int preferredSlot) {
        if (preferredSlot >= 0 && preferredSlot < container.GetSize()) {
            ItemStack? preferred = container.GetItem(preferredSlot);
            if (preferred is null) {
                return preferredSlot;
            }

            if (preferred.CanStackWith(sourceItem) && preferred.StackSize < preferred.Type.MaxStackSize) {
                return preferredSlot;
            }
        }

        for (int i = 0; i < container.GetSize(); i++) {
            ItemStack? item = container.GetItem(i);
            if (item is not null && item.CanStackWith(sourceItem) && item.StackSize < item.Type.MaxStackSize) {
                return i;
            }
        }

        for (int i = 0; i < container.GetSize(); i++) {
            if (container.GetItem(i) is null) {
                return i;
            }
        }

        return -1;
    }

    private static bool TryGetOpenedDynamicContainer(Player.Player player, FullContainerName name, out Container container) {
        container = null!;
        if (name.ContainerName != ContainerEnumName.DynamicContainer) {
            return false;
        }

        if (name.DynamicID is uint dynamicId && dynamicId != 0) {
            if (!player.TryGetOpenContainer((ContainerID)unchecked((sbyte)(byte)dynamicId), out Container? opened) ||
                opened is null || opened.Type == ContainerType.INVENTORY) {
                return false;
            }

            container = opened;
            return true;
        }

        Container? single = null;
        foreach ((_, Container opened) in player.openedContainers) {
            if (opened.Type == ContainerType.INVENTORY) {
                continue;
            }

            if (single is not null) {
                return false;
            }

            single = opened;
        }

        if (single is null) {
            return false;
        }

        container = single;
        return true;
    }

    private static bool TryFindSlotByStackNetworkId(Container container, int stackNetworkId, out int slot) {
        slot = -1;
        if (stackNetworkId == 0) {
            return false;
        }

        int targetId = stackNetworkId < 0 && _pendingCreativeStackId != 0
            ? _pendingCreativeStackId
            : stackNetworkId;

        for (int i = 0; i < container.GetSize(); i++) {
            ItemStack? item = container.GetItem(i);
            if (item?.NetworkStackId == targetId) {
                slot = i;
                return true;
            }
        }

        return false;
    }

    private static void RecordChange(
        Dictionary<string, ItemStackResponseContainerInfo> changed,
        FullContainerName containerName,
        Container container,
        int responseSlot,
        int storageSlot) {
        string key = containerName.DynamicID is uint dynamicId && dynamicId != 0
            ? $"{containerName.ContainerName}:{dynamicId}"
            : containerName.ContainerName.ToString();

        if (!changed.TryGetValue(key, out ItemStackResponseContainerInfo? info)) {
            info = new ItemStackResponseContainerInfo {
                FullContainerName = new FullContainerName {
                    ContainerName = containerName.ContainerName,
                    DynamicID = containerName.DynamicID
                },
                Slots = []
            };
            changed[key] = info;
        }

        ItemStack? item = container.GetItem(storageSlot);

        int durability = 0;
        if (item?.GetTrait<Basalt.Core.Item.Traits.ItemStackDurabilityTrait>() is { } durabilityTrait) {
            durability = durabilityTrait.GetCurrentDamage();
        }

        info.Slots.RemoveAll(s => s.Slot == responseSlot);
        info.Slots.Add(new ItemStackResponseSlotInfo {
            RequestedSlot = (byte)responseSlot,
            Slot = (byte)storageSlot,
            Amount = (byte)(item?.StackSize ?? 0),
            ItemStackNetId = new ItemStackNetId() { ID = item?.NetworkStackId ?? 0 },
            CustomName = new RedactableString {
                Unredacted = string.Empty,
                Redacted = null
            },
            DurabilityCorrection = durability
        });
    }

    private static ItemStackResponseInfo ErrorResponse(int requestId, ItemStackNetResult status = ItemStackNetResult.Error) {
        return new ItemStackResponseInfo {
            Result = status,
            ClientRequestId = requestId,
            Containers = []
        };
    }

    private static void ResyncContainers(Player.Player player) {
        foreach (Container container in player.openedContainers.Values.Distinct()) {
            container.Update();
        }

        player.GetTrait<PlayerCursorTrait>()?.Container.UpdateSlot(0);
        player.GetTrait<PlayerCraftingGridTrait>()?.Container.Update();
    }

    private static string DescribeAction(ItemStackRequestActionVariant action) {
        return action switch {
            ItemStackRequestTakeAction take =>
                $"Take(amount: {take.Amount}, src: {Slot(take.Source)}, dst: {Slot(take.Destination)})",
            ItemStackRequestPlaceAction place =>
                $"Place(amount: {place.Amount}, src: {Slot(place.Source)}, dst: {Slot(place.Destination)})",
            ItemStackRequestSwapAction swap =>
                $"Swap(src: {Slot(swap.Source)}, dst: {Slot(swap.Destination)})",
            ItemStackRequestDropAction drop =>
                $"Drop(amount: {drop.Amount}, src: {Slot(drop.Source)})",
            ItemStackRequestDestroyAction destroy =>
                $"Destroy(amount: {destroy.Amount}, src: {Slot(destroy.Source)})",
            ItemStackRequestConsumeAction consume =>
                $"Consume(amount: {consume.Amount}, src: {Slot(consume.Source)})",
            ItemStackRequestCraftCreativeAction creative =>
                $"CraftCreative(id: {creative.CreativeItemNetId}, crafts: {creative.NumberOfRequestedCrafts})",
            ItemStackRequestCraftRecipeAction craft =>
                $"CraftRecipe(crafts: {craft.NumberOfRequestedCrafts})",
            ItemStackRequestCraftRecipeAutoAction autoCraft =>
                $"CraftRecipeAuto(crafts: {autoCraft.NumberOfRequestedCrafts})",
            ItemStackRequestCreateAction create =>
                $"Create(resultsIndex: {create.ResultsIndex})",
            _ => action.GetType().Name
        };
    }

    private static string Item(ItemStack? item) {
        return item is null
            ? "empty"
            : $"{item.Type.Identifier} x{item.StackSize} nid={item.NetworkStackId}";
    }

    private static string Slot(ItemStackRequestSlotInfo slot) {
        return string.Format("[cid: {0}, dyn: {1}, slot: {2}, nid: {3}]",
            slot.FullContainerName.ContainerName,
            slot.FullContainerName.DynamicID == 0 ? "_" : slot.FullContainerName.DynamicID.ToString(),
            slot.Slot,
            slot.NetIdVariant);
    }

}
