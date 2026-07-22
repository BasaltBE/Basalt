namespace Basalt.Core.Network.Handlers;

using Basalt.Core.Containers;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
using Basalt.Core.Player.Traits;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;

public static class ItemStackRequest {
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer) {
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        ItemStackRequestPacket packet = (ItemStackRequestPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!server.Players.TryGetValue(connection, out Player.Player? player) || packet.Requests.Count == 0) {
            return;
        }

        List<ItemStackResponse> responses = new(packet.Requests.Count);

        foreach (Protocol.Types.ItemStackRequest request in packet.Requests) {
            try {
                responses.Add(ProcessRequest(player, request));
            }
            catch (Exception ex) {
                Console.WriteLine(string.Format("[ItemStackRequest] Exception on request: {0} {1}", request.RequestId, ex));
                responses.Add(ErrorResponse(request.RequestId));
            }
        }

        server.Network.SendPacket(connection, new ItemStackResponsePacket { Responses = responses });
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
    internal static ItemStackResponse ProcessRequestFromAuthInput(Player.Player player, Protocol.Types.ItemStackRequest request) {
        return ProcessRequest(player, request);
    }

    private static ItemStackResponse ProcessRequest(Player.Player player, Protocol.Types.ItemStackRequest request) {
        Dictionary<string, StackResponseContainerInfo> changed = [];
        _pendingCreativeStackId = 0;
        _pendingCreativeItem = null;
        _pendingCraftResult = null;

        foreach (IStackRequestAction action in request.Actions) {
            ItemStackResponseStatus status = HandleAction(player, action, changed);

            if (status == ItemStackResponseStatus.Ok) {
                continue;
            }

            Console.WriteLine(string.Format("[ItemStackRequest] Failed: request: {0} status: {1} action: {2}", request.RequestId, status, DescribeAction(action)));
            ResyncContainers(player);

            return ErrorResponse(request.RequestId, status);
        }

        return new ItemStackResponse {
            Status = ItemStackResponseStatus.Ok,
            RequestId = request.RequestId,
            ContainerInfo = changed.Count > 0 ? [.. changed.Values] : []
        };
    }

    private static ItemStackResponseStatus HandleAction(
        Player.Player player,
        IStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changed) {
        return action switch {
            TransferStackRequestAction transfer => HandleTransfer(player, transfer, changed),
            SwapStackRequestAction swap => HandleSwap(player, swap, changed),
            DropStackRequestAction drop => HandleDrop(player, drop, changed),
            DestroyStackRequestAction destroy => destroy.ActionType == 5
                ? HandleConsume(player, destroy, changed)
                : HandleDestroy(player, destroy, changed),
            CraftCreativeStackRequestAction creative => HandleCraftCreative(player, creative, changed),
            CraftRecipeStackRequestAction craft => HandleCraftRecipe(player, craft, changed),
            AutoCraftRecipeStackRequestAction autoCraft => HandleCraftRecipe(player, autoCraft.RecipeNetworkId, autoCraft.NumberOfCrafts, changed),

            // Actions that don't require server-side processing
            EmptyStackRequestAction => ItemStackResponseStatus.Ok,
            CraftResultsDeprecatedStackRequestAction => ItemStackResponseStatus.Ok,

            _ => ItemStackResponseStatus.InvalidRequestActionType
        };
    }

    private static ItemStackResponseStatus HandleTransfer(
        Player.Player player,
        TransferStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changed) {
        if (action.Source.Container.ContainerId == (byte)ContainerName.CreatedOutput && _pendingCreativeItem is not null) {
            if (!TryResolveSlot(player, action.Destination, out Container creativeDst, out int creativeDstSlot)) {
                return ItemStackResponseStatus.InvalidSourceContainer;
            }

            ItemStack item = _pendingCreativeItem;
            _pendingCreativeItem = null;

            creativeDst.SetItem(creativeDstSlot, item);
            RecordChange(changed, action.Destination.Container, creativeDst, action.Destination.Slot, creativeDstSlot);
            return ItemStackResponseStatus.Ok;
        }

        if (action.Source.Container.ContainerId == (byte)ContainerName.CreatedOutput && _pendingCraftResult is not null) {
            if (!TryResolveSlot(player, action.Destination, out Container craftDst, out int craftDstSlot)) {
                return ItemStackResponseStatus.InvalidSourceContainer;
            }

            ItemStack item = _pendingCraftResult;
            _pendingCraftResult = null;

            ItemStack? existing = craftDst.GetItem(craftDstSlot);
            if (existing is not null) {
                if (!existing.CanStackWith(item)) {
                    int altSlot = ResolveDestinationSlot(craftDst, item, craftDstSlot);
                    if (altSlot < 0) {
                        return ItemStackResponseStatus.CannotPlaceItem;
                    }
                    craftDstSlot = altSlot;
                    existing = craftDst.GetItem(craftDstSlot);
                }

                if (existing is not null) {
                    int available = existing.Type.MaxStackSize - existing.StackSize;
                    if (available <= 0) {
                        int altSlot = ResolveDestinationSlot(craftDst, item, craftDstSlot);
                        if (altSlot < 0) {
                            return ItemStackResponseStatus.CannotPlaceItem;
                        }
                        craftDstSlot = altSlot;
                        existing = craftDst.GetItem(craftDstSlot);
                    }
                }

                if (existing is not null) {
                    int available = existing.Type.MaxStackSize - existing.StackSize;
                    int toAdd = Math.Min(item.StackSize, available);
                    existing.IncrementStack((ushort)toAdd);
                    craftDst.UpdateSlot(craftDstSlot);
                }
                else {
                    craftDst.SetItem(craftDstSlot, item);
                }
            }
            else {
                craftDst.SetItem(craftDstSlot, item);
            }

            RecordChange(changed, action.Destination.Container, craftDst, action.Destination.Slot, craftDstSlot);
            return ItemStackResponseStatus.Ok;
        }

        if (!TryResolveSlot(player, action.Source, out Container srcContainer, out int srcSlot) ||
            !TryResolveSlot(player, action.Destination, out Container dstContainer, out int dstSlot)) {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        ItemStack? srcItem = srcContainer.GetItem(srcSlot);

        if (srcItem is null && action.Source.StackNetworkId != 0 &&
            TryFindSlotByStackNetworkId(srcContainer, action.Source.StackNetworkId, out int correctedSlot)) {
            srcSlot = correctedSlot;
            srcItem = srcContainer.GetItem(srcSlot);
        }

        if (srcItem is null) {
            return ItemStackResponseStatus.FailedToMatchExpectedSlotConsumedItem;
        }

        int amount = Math.Clamp((int)action.Count, 1, srcItem.StackSize);

        if (action.Destination.StackNetworkId == 0) {
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
                return ItemStackResponseStatus.CannotRemoveItem;
            }

            dstContainer.SetItem(dstSlot, taken);
        }
        else {
            if (!srcItem.CanStackWith(dstItem)) {
                return ItemStackResponseStatus.CannotPlaceItem;
            }

            int available = dstItem.Type.MaxStackSize - dstItem.StackSize;
            if (available <= 0) {
                return ItemStackResponseStatus.CannotPlaceItem;
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

        RecordChange(changed, action.Source.Container, srcContainer, action.Source.Slot, srcSlot);
        RecordChange(changed, action.Destination.Container, dstContainer, action.Destination.Slot, dstSlot);
        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus HandleSwap(
        Player.Player player,
        SwapStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changed) {
        if (!TryResolveSlot(player, action.Source, out Container srcContainer, out int srcSlot) ||
            !TryResolveSlot(player, action.Destination, out Container dstContainer, out int dstSlot)) {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        srcContainer.SwapItems(srcSlot, dstSlot, dstContainer);

        RecordChange(changed, action.Source.Container, srcContainer, action.Source.Slot, srcSlot);
        RecordChange(changed, action.Destination.Container, dstContainer, action.Destination.Slot, dstSlot);
        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus HandleDrop(
        Player.Player player,
        DropStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changed) {
        if (!TryResolveSlot(player, action.Source, out Container container, out int slot)) {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        int amount = Math.Max(1, (int)action.Count);
        ItemStack? removed = container.TakeItem(slot, amount);
        if (removed is null) {
            return ItemStackResponseStatus.CannotDropItem;
        }

        _ = player.DropItem(removed);

        RecordChange(changed, action.Source.Container, container, action.Source.Slot, slot);
        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus HandleDestroy(
        Player.Player player,
        DestroyStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changed) {
        if (!TryResolveSlot(player, action.Source, out Container container, out int slot)) {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        int amount = Math.Max(1, (int)action.Count);
        ItemStack? removed = container.TakeItem(slot, amount);
        if (removed is null) {
            return ItemStackResponseStatus.CannotDestroyItem;
        }

        RecordChange(changed, action.Source.Container, container, action.Source.Slot, slot);
        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus HandleConsume(
        Player.Player player,
        DestroyStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changed) {
        if (!TryResolveSlot(player, action.Source, out Container container, out int slot)) {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        RecordChange(changed, action.Source.Container, container, action.Source.Slot, slot);
        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus HandleCraftCreative(
        Player.Player player,
        CraftCreativeStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changed) {
        if (player.Gamemode != Gamemode.Creative) {
            return ItemStackResponseStatus.PlayerNotInCreativeMode;
        }

        ItemStack? item = ItemPalette.GetCreativeItem(action.CreativeItemNetworkId);
        if (item is null) {
            return ItemStackResponseStatus.FailedToCraftCreative;
        }

        _pendingCreativeItem = item;
        _pendingCreativeStackId = item.NetworkStackId;
        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus HandleCraftRecipe(
        Player.Player player,
        CraftRecipeStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changed) {
        return ProduceCraftResult(player, action.RecipeNetworkId, action.NumberOfCrafts, fromInventory: false);
    }

    private static ItemStackResponseStatus HandleCraftRecipe(
        Player.Player player,
        uint recipeNetworkId,
        byte numberOfCrafts,
        Dictionary<string, StackResponseContainerInfo> changed) {
        return ProduceCraftResult(player, recipeNetworkId, numberOfCrafts, fromInventory: true);
    }

    private static ItemStackResponseStatus ProduceCraftResult(
        Player.Player player,
        uint recipeNetworkId,
        byte numberOfCrafts,
        bool fromInventory) {
        Crafting.CraftingRecipe? recipe = Crafting.CraftingRegistry.Instance.GetByNetworkId(recipeNetworkId);
        if (recipe is null) {
            Logger.Warn("Could not Find a Recipe for " + recipeNetworkId);
            return ItemStackResponseStatus.Error;
        }

        ItemType? resultType = ItemType.Get(recipe.Result.Item);
        if (resultType is null && !recipe.Result.Item.Contains(':')) {
            resultType = ItemType.Get("minecraft:" + recipe.Result.Item);
        }
        if (resultType is null) {
            Logger.Info("Result item for crafting was not found: " + recipe.Result.Item);
            return ItemStackResponseStatus.Error;
        }

        int craftCount = Math.Max(1, (int)numberOfCrafts);

        List<(string? Item, string? Tag, int Count)> ingredients = GetFlatIngredients(recipe);
        if (ingredients.Count > 0) {
            Container? container = fromInventory
                ? player.GetTrait<EntityInventoryTrait>()?.Container
                : GetCraftingContainer(player);

            if (container is null) {
                Logger.Warn("HandleCraftRecipe: container is null");
                return ItemStackResponseStatus.Error;
            }

            if (!ConsumeIngredients(container, ingredients, craftCount)) {
                Logger.Warn($"HandleCraftRecipe: cannot consume ingredients for '{recipe.Identifier}' x{craftCount} ({ingredients.Count} ingredient slots)");
                return ItemStackResponseStatus.Error;
            }
        }

        int totalCount = recipe.Result.Count * craftCount;
        ushort stackSize = (ushort)Math.Min(totalCount, resultType.MaxStackSize);

        _pendingCraftResult = new ItemStack(resultType, stackSize);
        return ItemStackResponseStatus.Ok;
    }

    private static Container? GetCraftingContainer(Player.Player player) {
        foreach ((_, Container candidate) in player.openedContainers) {
            if (candidate.Type == ContainerType.Workbench) {
                return candidate;
            }
        }

        PlayerCraftingGridTrait? grid = player.GetTrait<PlayerCraftingGridTrait>();
        return grid?.Container;
    }

    private static List<(string? Item, string? Tag, int Count)> GetFlatIngredients(Crafting.CraftingRecipe recipe) {
        List<(string? Item, string? Tag, int Count)> result = [];

        if (recipe.Type == Crafting.RecipeType.Shaped) {
            foreach (string row in recipe.Pattern) {
                foreach (char symbol in row) {
                    if (symbol == ' ') {
                        continue;
                    }

                    if (recipe.Key.TryGetValue(symbol, out Crafting.RecipeIngredient? ingredient)) {
                        result.Add((ingredient.Item, ingredient.Tag, ingredient.Count));
                    }
                }
            }
        }
        else {
            foreach (Crafting.RecipeIngredient ingredient in recipe.Ingredients) {
                result.Add((ingredient.Item, ingredient.Tag, ingredient.Count));
            }
        }

        return result;
    }

    private static bool ConsumeIngredients(Container container, List<(string? Item, string? Tag, int Count)> ingredients, int craftCount) {
        // Verify all ingredients are available before consuming anything.
        // Track how much we need from each slot.
        int containerSize = container.GetSize();
        int[] slotConsumption = new int[containerSize];

        foreach ((string? item, string? tag, int count) in ingredients) {
            int totalNeeded = count * craftCount;
            int remaining = totalNeeded;

            for (int i = 0; i < containerSize && remaining > 0; i++) {
                ItemStack? slotItem = container.GetItem(i);
                if (slotItem is null || slotItem.StackSize == 0) {
                    continue;
                }

                if (!IngredientMatches(slotItem, item, tag)) {
                    continue;
                }

                int available = slotItem.StackSize - slotConsumption[i];
                if (available <= 0) {
                    continue;
                }

                int take = Math.Min(available, remaining);
                slotConsumption[i] += take;
                remaining -= take;
            }

            if (remaining > 0) {
                Logger.Warn($"ConsumeIngredients: missing ingredient item='{item}' tag='{tag}' needed={totalNeeded} remaining={remaining}");
                return false;
            }
        }

        // Actually consume.
        for (int i = 0; i < containerSize; i++) {
            if (slotConsumption[i] == 0) {
                continue;
            }

            ItemStack? slotItem = container.GetItem(i);
            if (slotItem is null) {
                continue;
            }

            if (slotConsumption[i] >= slotItem.StackSize) {
                container.ClearSlot(i);
            }
            else {
                slotItem.DecrementStack((ushort)slotConsumption[i]);
                container.UpdateSlot(i);
            }
        }

        return true;
    }

    private static bool IngredientMatches(ItemStack item, string? ingredientItem, string? ingredientTag) {
        if (ingredientTag is not null) {
            IReadOnlyList<string> tags = item.Type.Tags;
            for (int i = 0; i < tags.Count; i++) {
                if (string.Equals(tags[i], ingredientTag, StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }

        if (ingredientItem is null) {
            return false;
        }

        if (string.Equals(item.Type.Identifier, ingredientItem, StringComparison.Ordinal)) {
            return true;
        }

        if (!ingredientItem.Contains(':')) {
            return string.Equals(item.Type.Identifier, "minecraft:" + ingredientItem, StringComparison.Ordinal);
        }

        return false;
    }

    private static bool TryResolveSlot(Player.Player player, StackRequestSlotInfo requestSlot, out Container container, out int slot) {
        container = null!;
        slot = -1;

        Container? resolved = ResolveContainer(player, requestSlot.Container, requestSlot.Slot);
        if (resolved is null) {
            return false;
        }

        int resolvedSlot = ResolveSlotIndex(player, requestSlot.Container, resolved, requestSlot.Slot);
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

        if (name.ContainerId == (byte)ContainerName.DynamicContainer) {
            return null;
        }

        if (name.ContainerId is (byte)ContainerName.RecipeEquipment
            or (byte)ContainerName.RecipeBook
            or (byte)ContainerName.EnchantingInput
            or (byte)ContainerName.EnchantingMaterial
            or (byte)ContainerName.FurnaceFuel
            or (byte)ContainerName.FurnaceIngredient
            or (byte)ContainerName.FurnaceResult) {
            foreach ((_, Container opened) in player.openedContainers) {
                if (opened.Type is ContainerType.Furnace or ContainerType.BlastFurnace or ContainerType.Smoker) {
                    return opened;
                }
            }
        }

        return player.GetContainer(name);
    }

    private static int ResolveSlotIndex(Player.Player player, FullContainerName containerName, Container container, int slot) {
        if (containerName.ContainerId == (byte)ContainerName.CreatedOutput) {
            return 0;
        }

        if (containerName.ContainerId == (byte)ContainerName.CraftingInput) {
            if (slot >= 32) return slot - 32;
            return Player.Traits.PlayerCraftingGridTrait.MapSlot(slot);
        }

        if (containerName.ContainerId == (byte)ContainerName.Armor) {
            return slot;
        }

        if (containerName.ContainerId == (byte)ContainerName.Offhand) {
            return slot;
        }

        if (containerName.ContainerId is (byte)ContainerName.CombinedHotbarAndInventory
            or (byte)ContainerName.Inventory or (byte)ContainerName.Hotbar) {
            return NormalizeInventorySlot(slot);
        }

        // Furnace and crafting UI slot IDs pass through directly.
        if (containerName.ContainerId is (byte)ContainerName.RecipeEquipment
            or (byte)ContainerName.RecipeBook
            or (byte)ContainerName.EnchantingInput
            or (byte)ContainerName.EnchantingMaterial
            or (byte)ContainerName.FurnaceFuel
            or (byte)ContainerName.FurnaceIngredient
            or (byte)ContainerName.FurnaceResult) {
            return slot;
        }

        if (containerName.ContainerId is (byte)ContainerName.DynamicContainer
            or (byte)ContainerName.Barrel
            or (byte)ContainerName.LevelEntity) {
            if (container.Type != ContainerType.Inventory) {
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
        if (name.ContainerId != (byte)ContainerName.DynamicContainer) {
            return false;
        }

        if (name.DynamicContainerId.HasValue) {
            if (!player.TryGetOpenContainer((ContainerId)(sbyte)name.DynamicContainerId.Value, out Container? opened) ||
                opened is null || opened.Type == ContainerType.Inventory) {
                return false;
            }

            container = opened;
            return true;
        }

        Container? single = null;
        foreach ((_, Container opened) in player.openedContainers) {
            if (opened.Type == ContainerType.Inventory) {
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
        Dictionary<string, StackResponseContainerInfo> changed,
        FullContainerName containerName,
        Container container,
        int responseSlot,
        int storageSlot) {
        string key = containerName.DynamicContainerId.HasValue
            ? $"{containerName.ContainerId}:{containerName.DynamicContainerId.Value}"
            : containerName.ContainerId.ToString();

        if (!changed.TryGetValue(key, out StackResponseContainerInfo? info)) {
            info = new StackResponseContainerInfo {
                Container = new FullContainerName {
                    ContainerId = containerName.ContainerId,
                    DynamicContainerId = containerName.DynamicContainerId
                },
                SlotInfo = []
            };
            changed[key] = info;
        }

        ItemStack? item = container.GetItem(storageSlot);

        info.SlotInfo.RemoveAll(s => s.Slot == responseSlot);
        info.SlotInfo.Add(new StackResponseSlotInfo {
            Slot = (byte)responseSlot,
            HotbarSlot = (byte)responseSlot,
            Count = (byte)(item?.StackSize ?? 0),
            StackNetworkId = item?.NetworkStackId ?? 0,
            CustomName = string.Empty,
            FilteredCustomName = string.Empty,
            DurabilityCorrection = 0
        });
    }

    private static ItemStackResponse ErrorResponse(int requestId, ItemStackResponseStatus status = ItemStackResponseStatus.Error) {
        return new ItemStackResponse {
            Status = status,
            RequestId = requestId,
            ContainerInfo = []
        };
    }

    private static void ResyncContainers(Player.Player player) {
        foreach (Container container in player.openedContainers.Values.Distinct()) {
            container.Update();
        }

        player.GetTrait<PlayerCursorTrait>()?.Container.UpdateSlot(0);
    }

    private static string DescribeAction(IStackRequestAction action) {
        return action switch {
            TransferStackRequestAction t => string.Format("Transfer(count: {0}, src: {1}, dst: {2})", t.Count, Slot(t.Source), Slot(t.Destination)),
            SwapStackRequestAction s => string.Format("Swap(src: {0}, dst: {1})", Slot(s.Source), Slot(s.Destination)),
            DropStackRequestAction d => string.Format("Drop(count: {0}, src: {1})", d.Count, Slot(d.Source)),
            DestroyStackRequestAction d => string.Format("Destroy(count: {0}, src: {1})", d.Count, Slot(d.Source)),
            CraftCreativeStackRequestAction c => string.Format("CraftCreative(id: {0}, crafts: {1})", c.CreativeItemNetworkId, c.NumberOfCrafts),
            _ => action.GetType().Name
        };
    }

    private static string Slot(StackRequestSlotInfo slot) {
        return string.Format("[cid: {0}, dyn: {1}, slot: {2}, nid: {3}]",
            slot.Container.ContainerId,
            slot.Container.DynamicContainerId?.ToString() ?? "_",
            slot.Slot,
            slot.StackNetworkId);
    }

}
