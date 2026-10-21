using BasaltBE.Basalt.Crafting;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Events.Containers;
using BasaltBE.Basalt.Events.Types;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BasaltBE.Logging;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class ItemStackRequestHandler {
    [ThreadStatic]
    private static RequestState? cachedState;

    public void Handle(NethernetPeer peer, Player? player, ItemStackRequestPacket packet) {
        if (player is null)
            throw new InvalidDataException("Item stack request arrived before player login.");
        if (player.Disposed)
            return;

        ItemStackResponseInfo[] responses = new ItemStackResponseInfo[packet.Requests.Length];
        for (int i = 0; i < packet.Requests.Length; i++) {
            responses[i] = ProcessRequest(player, packet.Requests[i], out string? failureReason);
            ItemStackResponseInfo response = responses[i];
            if (response.Result != ItemStackNetResult.Success)
                Logger.Scoped("Network").Warn(
                    $"Rejected item stack request {response.ClientRequestId} from {player.Username}: {response.Result}" +
                    (failureReason is null ? "." : $" ({failureReason}).")
                );
        }

        player.Network.SendPacket(player, new ItemStackResponsePacket {
            Responses = responses
        });
    }

    internal static ItemStackResponseInfo ProcessRequest(Player player, ItemStackRequestData request) =>
        ProcessRequest(player, request, null, false, out _);

    internal static ItemStackResponseInfo ProcessRequest(
        Player player,
        ItemStackRequestData request,
        int? mineBlockNetId,
        bool blockDestroyed
    ) {
        return ProcessRequest(player, request, mineBlockNetId, blockDestroyed, out _);
    }

    private static ItemStackResponseInfo ProcessRequest(
        Player player,
        ItemStackRequestData request,
        out string? failureReason
    ) => ProcessRequest(player, request, null, false, out failureReason);

    internal static ItemStackResponseInfo ProcessRequest(
        Player player,
        ItemStackRequestData request,
        int? mineBlockNetId,
        bool blockDestroyed,
        out string? failureReason
    ) {
        lock (player.Sync) {
            PlayerInventoryTrait? inventory = player.GetTrait<PlayerInventoryTrait>();
            if (!player.Spawned || inventory is null) {
                failureReason = "player inventory is unavailable";
                return ErrorResponse(request.ClientRequestId);
            }

            ItemStackResponseInfo response = ProcessRequest(inventory, request, mineBlockNetId, blockDestroyed, out failureReason);
            if (response.Result != ItemStackNetResult.Success) {
                inventory.Inventory.Update();
                inventory.Cursor.Update();
                if (TouchesArmor(request))
                    inventory.Entity.GetTrait<EntityArmorTrait>()?.Armor.Update();
            }

            return response;
        }
    }

    internal static ItemStackResponseInfo ProcessRequest(
        Container inventory,
        Container cursor,
        ItemStackRequestData request
    ) {
        return ProcessRequest(inventory, cursor, request, null, null, null, false, out _);
    }

    private static ItemStackResponseInfo ProcessRequest(PlayerInventoryTrait inventory, ItemStackRequestData request) =>
        ProcessRequest(inventory, request, out _);

    private static ItemStackResponseInfo ProcessRequest(
        PlayerInventoryTrait inventory,
        ItemStackRequestData request,
        out string? failureReason
    ) => ProcessRequest(inventory, request, null, false, out failureReason);

    private static ItemStackResponseInfo ProcessRequest(
        PlayerInventoryTrait inventory,
        ItemStackRequestData request,
        int? mineBlockNetId,
        bool blockDestroyed,
        out string? failureReason
    ) =>
        ProcessRequest(
            inventory.Inventory,
            inventory.Cursor,
            request,
            inventory,
            inventory.Entity is Player player ? player.OpenedContainer : null,
            mineBlockNetId,
            blockDestroyed,
            out failureReason
        );

    private static ItemStackResponseInfo ProcessRequest(
        Container inventory,
        Container cursor,
        ItemStackRequestData request,
        PlayerInventoryTrait? inventoryTrait,
        Container? openedContainer,
        int? mineBlockNetId,
        bool blockDestroyed,
        out string? failureReason
    ) {
        failureReason = null;
        int clientRequestNetId = (request.ClientRequestId >> 1) ^ -(request.ClientRequestId & 1);
        RequestState state = cachedState ?? new RequestState();
        cachedState = null;
        state.Inventory = inventory;
        state.Cursor = cursor;
        state.OpenedContainer = openedContainer;
        state.ClientRequestNetId = clientRequestNetId;
        state.InventoryTrait = inventoryTrait;
        state.MineBlockNetId = mineBlockNetId;
        state.BlockDestroyed = blockDestroyed;
        try {
            foreach (ItemStackRequestActionData action in request.Actions) {
                if (!Apply(state, action)) {
                    failureReason = state.FailureReason ?? action.ActionType.ToString();
                    return new ItemStackResponseInfo {
                        Result = ItemStackNetResult.Error,
                        ClientRequestId = request.ClientRequestId
                    };
                }
            }

            if (state.Recipe is not null && !ValidateCraft(state)) {
                failureReason = "craft validation";
                return ErrorResponse(request.ClientRequestId);
            }

            EntityArmorTrait? armor = inventoryTrait?.Entity.GetTrait<EntityArmorTrait>();
            foreach (SlotKey key in state.ChangedSlots) {
                if (!ReferenceEquals(key.Container, armor?.Armor) || state.Staged[key] is not { } item)
                    continue;
                if (item.Type.ArmorSlot != (ArmorSlot)key.Slot) {
                    failureReason = "item does not fit armor slot";
                    return ErrorResponse(request.ClientRequestId);
                }
            }

            if (inventoryTrait?.Entity is Player actor && actor.Network.Server.Events.HasSubscribers<ContainerTransactionEvent>()) {
                int changeCount = state.ChangedSlots.Count;
                if (state.CreatedOutput is { } output && state.ChangedSlotSet.Contains(new SlotKey(output, 0)))
                    changeCount--;
                ContainerSlotChange[] changes = new ContainerSlotChange[changeCount];
                int changeIndex = 0;
                foreach (SlotKey key in state.ChangedSlots) {
                    if (ReferenceEquals(key.Container, state.CreatedOutput))
                        continue;
                    changes[changeIndex++] = new(key.Container, key.Slot, key.Container.GetItem(key.Slot), state.Staged[key]);
                }
                ContainerTransactionEvent transaction = new(actor, request.ClientRequestId, changes);
                actor.Network.Server.Events.Emit(transaction);
                if (transaction.IsCanceled() || actor.Disposed)
                    return ErrorResponse(request.ClientRequestId);
            }

            bool armorChanged = false;
            foreach (SlotKey key in state.ChangedSlots) {
                if (ReferenceEquals(key.Container, state.CreatedOutput))
                    continue;
                key.Container.SetItem(key.Slot, state.Staged[key]);
                armorChanged |= ReferenceEquals(key.Container, armor?.Armor);
                inventoryTrait?.RememberClientRequestNetId(
                    key.Container,
                    key.Slot,
                    clientRequestNetId,
                    state.Staged[key]
                );
            }

            if (armorChanged)
                armor?.SyncEquipment();

            if (inventoryTrait?.Entity is Player player) {
                foreach (PendingDrop drop in state.DroppedItems)
                    new ItemEntity(drop.Item, player, drop.Randomly).Spawn(player.Dimension);
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
                    RequestedSlot = checked((byte)responseKey.SlotKey.Container.GetNetworkSlot(responseKey.SlotKey.Slot)),
                    Slot = checked((byte)responseKey.SlotKey.Container.GetNetworkSlot(responseKey.SlotKey.Slot)),
                    Amount = checked((byte)(item?.StackSize ?? 0)),
                    ItemStackNetId = item?.NetId,
                    DurabilityCorrection = responseKey.DurabilityCorrection
                });
            }

            ItemStackResponseContainerInfo[] containers = new ItemStackResponseContainerInfo[grouped.Count];
            int index = 0;
            foreach (KeyValuePair<ContainerNameKey, List<ItemStackResponseSlotInfo>> entry in grouped) {
                containers[index++] = new ItemStackResponseContainerInfo {
                    ContainerName = {
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
        finally {
            state.Inventory = null!;
            state.Cursor = null!;
            state.OpenedContainer = null;
            state.InventoryTrait = null;
            state.ClientRequestNetId = 0;
            state.MineBlockNetId = null;
            state.BlockDestroyed = false;
            state.Recipe = null;
            state.CraftCount = 0;
            state.CraftConsumption = null;
            state.CraftConsumed = null;
            state.CraftSources = null;
            state.AutoCraft = false;
            state.AutoCraftOutputSlot = null;
            state.CraftResults = null;
            state.CreatedOutput = null;
            state.Staged.Clear();
            state.StagedClientRequestNetIds.Clear();
            state.ChangedSlots.Clear();
            state.ChangedSlotSet.Clear();
            state.ResponseSlots.Clear();
            state.ResponseSlotSet.Clear();
            state.DroppedItems.Clear();
            state.FailureReason = null;
            cachedState ??= state;
        }
    }

    private static bool Apply(RequestState state, ItemStackRequestActionData action) => action switch {
        TakeActionData take => Transfer(state, take.Amount, take.Source, take.Destination),
        PlaceActionData place => Transfer(state, place.Amount, place.Source, place.Destination),
        SwapActionData swap => Swap(state, swap.Source, swap.Destination),
        DropActionData drop => Drop(state, drop),
        DestroyActionData destroy => Remove(state, destroy.Amount, destroy.Source, consume: false),
        ConsumeActionData consume => Remove(state, consume.Amount, consume.Source, consume: true),
        CraftRecipeActionData craft => SelectRecipe(state, craft.RecipeNetId, craft.NumberOfRequestedCrafts, autoCraft: false),
        CraftRecipeAutoActionData craft => SelectRecipe(state, craft.RecipeNetId, craft.NumberOfRequestedCrafts, autoCraft: true),
        CreateActionData create => CreateCraftResult(state, create.ResultsIndex),
        CraftResultsActionData results => RecordCraftResults(state, results),
        MineBlockActionData mineBlock => MineBlock(state, mineBlock),
        _ => false
    };

    private static bool MineBlock(RequestState state, MineBlockActionData action) {
        if (!state.BlockDestroyed || state.MineBlockNetId is not int expectedNetId ||
            (uint)action.Slot >= 9 || state.InventoryTrait?.SelectedSlot != action.Slot ||
            expectedNetId != action.NetIdVariant) {
            state.FailureReason = "block break or held item prediction did not match";
            return false;
        }

        SlotKey key = new(state.Inventory, action.Slot);
        ItemStack? item = state.Inventory.GetItem(action.Slot);
        int correction = item is null ? 0 : item.Damage - action.PredictedDurability;
        if (correction is < short.MinValue or > short.MaxValue) {
            state.FailureReason = "durability correction is out of range";
            return false;
        }

        state.Staged[key] = item?.Clone();
        ResponseSlotKey responseKey = new(key, ContainerEnumName.Hotbar, null, correction);
        if (state.ResponseSlotSet.Add(responseKey))
            state.ResponseSlots.Add(responseKey);
        return true;
    }

    private static bool SelectRecipe(RequestState state, int netId, byte crafts, bool autoCraft) {
        if (state.Recipe is not null) {
            state.FailureReason = "recipe already selected";
            return false;
        }
        if (crafts == 0) {
            state.FailureReason = "requested craft count is zero";
            return false;
        }
        if (CraftingPalette.FindRecipe(netId) is not { Tag: "crafting_table" } recipe) {
            state.FailureReason = $"recipe {netId} was not found for a crafting table";
            return false;
        }
        if ((long)recipe.ResultCount * crafts > recipe.Result.MaxAmount) {
            state.FailureReason = "requested result exceeds the item stack limit";
            return false;
        }

        Container[] sources;
        int[] consumption;
        if (autoCraft) {
            Container? workbench = state.OpenedContainer is { Type: ContainerType.Workbench, ContainerName: ContainerEnumName.CraftingInput } table
                ? table
                : null;
            if (workbench is not null && state.InventoryTrait is { } inventoryTrait)
                sources = [workbench, inventoryTrait.CraftingGrid, state.Inventory];
            else if (workbench is not null)
                sources = [workbench, state.Inventory];
            else if (state.InventoryTrait is { } playerInventory)
                sources = [playerInventory.CraftingGrid, state.Inventory];
            else
                sources = [state.Inventory];

            ItemStack?[] items = new ItemStack?[sources.Sum(static source => source.ContainerSize)];
            int itemIndex = 0;
            foreach (Container source in sources) {
                for (int slot = 0; slot < source.ContainerSize; slot++)
                    items[itemIndex++] = source.GetItem(slot);
            }

            consumption = new int[items.Length];
            if (!recipe.TryMatchInventory(items, crafts, consumption)) {
                state.FailureReason = $"recipe {netId} does not match available crafting ingredients";
                return false;
            }
        }
        else {
            Container? grid = state.OpenedContainer is { Type: ContainerType.Workbench, ContainerName: ContainerEnumName.CraftingInput } table
                ? table
                : state.OpenedContainer is null || ReferenceEquals(state.OpenedContainer, state.Inventory)
                    ? state.InventoryTrait?.CraftingGrid
                    : null;
            if (grid is null || grid.ContainerSize is not (4 or 9)) {
                state.FailureReason = "crafting grid is unavailable";
                return false;
            }

            int gridWidth = grid.ContainerSize == 9 ? 3 : 2;
            ItemStack?[] items = new ItemStack?[grid.ContainerSize];
            for (int i = 0; i < items.Length; i++)
                items[i] = grid.GetItem(i);

            consumption = new int[items.Length];
            if (!recipe.TryMatch(items, gridWidth, crafts, consumption)) {
                state.FailureReason = $"recipe {netId} does not match crafting grid items";
                return false;
            }
            sources = [grid];
        }

        state.Recipe = recipe;
        state.CraftCount = crafts;
        state.CraftConsumption = consumption;
        state.CraftConsumed = new int[consumption.Length];
        state.CraftSources = sources;
        state.AutoCraft = autoCraft;
        return true;
    }

    private static bool CreateCraftResult(RequestState state, byte resultsIndex) {
        if (state.Recipe is not { } recipe || state.CreatedOutput is not null || resultsIndex != 0)
            return false;

        Container output = new(
            1,
            ContainerId.PlayerOnlyUi,
            ContainerEnumName.CreatedOutput,
            ContainerType.Inventory,
            slotOffset: 50
        );
        ItemStack result = new(recipe.Result, (ushort)(recipe.ResultCount * state.CraftCount), recipe.ResultMetadata);
        state.CreatedOutput = output;
        SetItem(state, new SlotKey(output, 0), result, ContainerEnumName.CreatedOutput, 0);
        return true;
    }

    private static bool RecordCraftResults(RequestState state, CraftResultsActionData results) {
        if (state.Recipe is null || state.CraftResults is not null || results.CraftResults.Length != 1 ||
            results.NumberOfCrafts != state.CraftCount)
            return false;
        if (state.CreatedOutput is null && !CreateCraftResult(state, 0))
            return false;
        state.CraftResults = results;
        return true;
    }

    private static bool ValidateCraft(RequestState state) {
        CraftingRecipe recipe = state.Recipe!;
        Container[] sources = state.CraftSources!;
        int[] consumption = state.CraftConsumption!;
        int index = 0;
        foreach (Container source in sources) {
            for (int slot = 0; slot < source.ContainerSize; slot++, index++) {
                SlotKey key = new(source, slot);
                bool isAutoCraftOutput = state.AutoCraftOutputSlot == key;
                if (state.CraftConsumed![index] != consumption[index] || isAutoCraftOutput && consumption[index] != 0)
                    return false;
                ItemStack? original = source.GetItem(slot);
                if (isAutoCraftOutput)
                    continue;
                ItemStack? current = state.Staged.TryGetValue(key, out ItemStack? staged) ? staged : original;
                int consumed = consumption[index];
                if (consumed == 0) {
                    if (!SameStack(original, current))
                        return false;
                }
                else if (original is null || current is null && original.StackSize != consumed ||
                    current is not null && (current.Type != original.Type || current.StackSize != original.StackSize - consumed)) {
                    return false;
                }
            }
        }

        if (state.CreatedOutput is not { } output ||
            state.Staged.GetValueOrDefault(new SlotKey(output, 0)) is not null)
            return false;

        if (state.CraftResults is not { } reported)
            return true;

        NetworkItemInstanceDescriptorData result = reported.CraftResults[0];
        return result.ItemDescriptor is ItemNameDescriptorData descriptor &&
            descriptor.FullName == recipe.Result.Identifier &&
            result.StackSize == recipe.ResultCount * state.CraftCount &&
            (descriptor.AuxValue == 0x7FFF || unchecked((uint)descriptor.AuxValue) == recipe.ResultMetadata);
    }

    private static bool SameStack(ItemStack? first, ItemStack? second) =>
        first is null ? second is null : second is not null &&
        first.Type == second.Type && first.StackSize == second.StackSize && first.Metadata == second.Metadata;

    private static bool Transfer(
        RequestState state,
        byte amount,
        ItemStackSlotInfoData sourceInfo,
        ItemStackSlotInfoData destinationInfo
    ) {
        if (amount == 0) {
            state.FailureReason = "transfer amount is zero";
            return false;
        }
        if (!TryGetItem(state, sourceInfo, out SlotKey sourceKey, out ItemStack? sourceItem)) {
            state.FailureReason ??= $"invalid source slot {sourceInfo.ContainerName.ContainerName}[{sourceInfo.Slot}]";
            return false;
        }
        if (!TryGetItem(state, destinationInfo, out SlotKey destinationKey, out ItemStack? destinationItem)) {
            state.FailureReason ??= $"invalid destination slot {destinationInfo.ContainerName.ContainerName}[{destinationInfo.Slot}]";
            return false;
        }

        bool touchesCraftGrid = IsCraftSource(state, sourceKey.Container) ||
            IsCraftSource(state, destinationKey.Container);
        bool autoCraftOutput = state.AutoCraft && ReferenceEquals(sourceKey.Container, state.CreatedOutput) &&
            ReferenceEquals(destinationKey.Container, state.Inventory);
        if (state.Recipe is not null && touchesCraftGrid && !autoCraftOutput) {
            state.FailureReason = "recipe transfer touches the crafting input";
            return false;
        }
        if (sourceKey == destinationKey)
            return true;
        if (sourceItem is null || amount > sourceItem.StackSize) {
            state.FailureReason = "source stack is empty or smaller than the transfer";
            return false;
        }

        if (destinationItem is null) {
            ItemStack moved = sourceItem.Clone(amount, cloneNbt: amount != sourceItem.StackSize);
            SetItem(state, destinationKey, moved, destinationInfo.ContainerName.ContainerName, destinationInfo.ContainerName.DynamicId);
            SetItem(state, sourceKey, amount == sourceItem.StackSize
                ? null
                : sourceItem.Clone((ushort)(sourceItem.StackSize - amount)),
                sourceInfo.ContainerName.ContainerName,
                sourceInfo.ContainerName.DynamicId);
            if (autoCraftOutput)
                state.AutoCraftOutputSlot = destinationKey;
            return true;
        }

        if (!sourceItem.CanStackWith(destinationItem)) {
            state.FailureReason = "source and destination stacks cannot merge";
            return false;
        }

        int available = destinationItem.Type.MaxAmount - destinationItem.StackSize;
        if (available < amount) {
            state.FailureReason = "destination stack has insufficient space";
            return false;
        }

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
        if (autoCraftOutput)
            state.AutoCraftOutputSlot = destinationKey;
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

        if (state.Recipe is not null &&
            (IsCraftSource(state, sourceKey.Container) || IsCraftSource(state, destinationKey.Container)))
            return false;

        SetItem(state, sourceKey, destinationItem, sourceInfo.ContainerName.ContainerName, sourceInfo.ContainerName.DynamicId);
        SetItem(state, destinationKey, sourceItem, destinationInfo.ContainerName.ContainerName, destinationInfo.ContainerName.DynamicId);
        return true;
    }

    private static bool Remove(RequestState state, byte amount, ItemStackSlotInfoData sourceInfo, bool consume) {
        if (amount == 0) {
            state.FailureReason = "consume amount is zero";
            return false;
        }
        if (!TryGetItem(state, sourceInfo, out SlotKey sourceKey, out ItemStack? sourceItem)) {
            state.FailureReason = $"invalid source slot {sourceInfo.ContainerName.ContainerName}[{sourceInfo.Slot}] netId={sourceInfo.NetIdVariant}";
            return false;
        }
        if (sourceItem is null || amount > sourceItem.StackSize) {
            state.FailureReason = $"source slot {sourceInfo.ContainerName.ContainerName}[{sourceInfo.Slot}] has {sourceItem?.StackSize ?? 0}, requested {amount}";
            return false;
        }

        if (state.Recipe is not null) {
            if (!consume) {
                state.FailureReason = "recipe action tried to destroy an ingredient";
                return false;
            }
            int sourceIndex = GetCraftSourceIndex(state, sourceKey);
            if (sourceIndex < 0 || state.CraftConsumption is null) {
                state.FailureReason = $"ingredient came from an unexpected container {sourceInfo.ContainerName.ContainerName}[{sourceInfo.Slot}]";
                return false;
            }
            int expected = state.CraftConsumption[sourceIndex] - state.CraftConsumed![sourceIndex];
            if (amount > expected && state.AutoCraft) {
                int needed = amount - expected;
                for (int slot = 0; slot < state.CraftConsumption.Length && needed > 0; slot++) {
                    if (slot == sourceIndex)
                        continue;

                    int available = state.CraftConsumption[slot] - state.CraftConsumed[slot];
                    if (available <= 0)
                        continue;

                    SlotKey candidateKey = GetCraftSourceKey(state, slot);
                    ItemStack? candidate = state.Staged.TryGetValue(candidateKey, out ItemStack? staged)
                        ? staged
                        : candidateKey.Container.GetItem(candidateKey.Slot);
                    if (candidate is null || !candidate.CanStackWith(sourceItem))
                        continue;

                    int shifted = Math.Min(available, needed);
                    state.CraftConsumption[slot] -= shifted;
                    state.CraftConsumption[sourceIndex] += shifted;
                    expected += shifted;
                    needed -= shifted;
                }
            }
            if (amount > expected) {
                state.FailureReason = $"slot {sourceKey.Slot} expected {expected} more ingredients, got {amount}";
                return false;
            }
            state.CraftConsumed[sourceIndex] += amount;
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

    private static bool Drop(RequestState state, DropActionData action) {
        if (action.Amount == 0 || state.InventoryTrait?.Entity is not Player player ||
            !player.Spawned || player.Disposed ||
            !TryGetItem(state, action.Source, out SlotKey sourceKey, out ItemStack? sourceItem) ||
            sourceItem is null || action.Amount > sourceItem.StackSize)
            return false;
        if (state.Recipe is not null && IsCraftSource(state, sourceKey.Container))
            return false;

        ItemStack dropped = sourceItem.Clone(action.Amount, cloneNbt: action.Amount != sourceItem.StackSize);
        ushort remaining = (ushort)(sourceItem.StackSize - action.Amount);
        SetItem(
            state,
            sourceKey,
            remaining == 0 ? null : sourceItem.Clone(remaining),
            action.Source.ContainerName.ContainerName,
            action.Source.ContainerName.DynamicId
        );
        state.DroppedItems.Add(new PendingDrop(dropped, action.Randomly));
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
            (uint)container.GetLocalSlot(info.Slot) >= container.ContainerSize) {
            key = default;
            item = null;
            return false;
        }

        key = new SlotKey(container, container.GetLocalSlot(info.Slot));
        if (!state.Staged.TryGetValue(key, out item)) {
            item = container.GetItem(key.Slot);
            bool predictedArmorStack = container.ContainerName == ContainerEnumName.Armor &&
                info.NetIdVariant < 0 && item?.Type.ArmorSlot == (ArmorSlot)key.Slot;
            if (item is not null && !ReferenceEquals(container, state.CreatedOutput) &&
                item.NetId != info.NetIdVariant &&
                !predictedArmorStack &&
                !(state.InventoryTrait?.MatchesClientRequestNetId(container, key.Slot, info.NetIdVariant, item) ?? false)) {
                state.FailureReason = $"stack ID mismatch in {info.ContainerName.ContainerName}[{info.Slot}]: client: {info.NetIdVariant}, server: {item.NetId}";
                return false;
            }
            item = item?.Clone();
            state.Staged.Add(key, item);
            return true;
        }

        return ReferenceEquals(container, state.CreatedOutput) ||
            state.Recipe is not null && IsCraftSource(state, container) ||
            item is null ||
            (item?.NetId ?? 0) == info.NetIdVariant ||
            state.StagedClientRequestNetIds.TryGetValue(key, out int clientRequestNetId) &&
            clientRequestNetId == info.NetIdVariant;
    }

    private static bool ResolveContainer(RequestState state, FullContainerName name, out Container? container) {
        container = null;
        if (state.OpenedContainer is { } openedContainer &&
            openedContainer.ContainerName == name.ContainerName &&
            (openedContainer.DynamicId == name.DynamicId ||
                name.ContainerName == ContainerEnumName.CraftingInput &&
                (openedContainer.DynamicId is null or 0) && (name.DynamicId is null or 0))) {
            container = openedContainer;
            return true;
        }

        if (name.ContainerName == ContainerEnumName.CreatedOutput &&
            (name.DynamicId is null or 0) && state.CreatedOutput is { } createdOutput) {
            container = createdOutput;
            return true;
        }

        if (name.DynamicId is uint dynamicId && dynamicId != 0)
            return false;

        if (name.ContainerName == ContainerEnumName.CraftingInput &&
            (state.OpenedContainer is null || ReferenceEquals(state.OpenedContainer, state.Inventory)) &&
            state.InventoryTrait is { } inventoryTrait) {
            container = inventoryTrait.CraftingGrid;
            return true;
        }

        container = name.ContainerName switch {
            ContainerEnumName.Inventory or ContainerEnumName.Hotbar or ContainerEnumName.CombinedHotbarAndInventory => state.Inventory,
            ContainerEnumName.Cursor => state.Cursor,
            ContainerEnumName.Armor => state.InventoryTrait?.Entity.GetTrait<EntityArmorTrait>()?.Armor,
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

    private static bool TouchesArmor(ItemStackRequestData request) {
        foreach (ItemStackRequestActionData action in request.Actions) {
            bool touchesArmor = action switch {
                TakeActionData take => IsArmorSlot(take.Source) || IsArmorSlot(take.Destination),
                PlaceActionData place => IsArmorSlot(place.Source) || IsArmorSlot(place.Destination),
                SwapActionData swap => IsArmorSlot(swap.Source) || IsArmorSlot(swap.Destination),
                DropActionData drop => IsArmorSlot(drop.Source),
                DestroyActionData destroy => IsArmorSlot(destroy.Source),
                ConsumeActionData consume => IsArmorSlot(consume.Source),
                _ => false
            };
            if (touchesArmor)
                return true;
        }

        return false;
    }

    private static bool IsArmorSlot(ItemStackSlotInfoData slot) =>
        slot.ContainerName.ContainerName == ContainerEnumName.Armor;

    private static bool IsCraftSource(RequestState state, Container container) {
        if (state.CraftSources is null)
            return false;
        foreach (Container source in state.CraftSources) {
            if (ReferenceEquals(source, container))
                return true;
        }
        return false;
    }

    private static int GetCraftSourceIndex(RequestState state, SlotKey key) {
        int index = 0;
        foreach (Container source in state.CraftSources!) {
            if (ReferenceEquals(source, key.Container))
                return index + key.Slot;
            index += source.ContainerSize;
        }
        return -1;
    }

    private static SlotKey GetCraftSourceKey(RequestState state, int index) {
        foreach (Container source in state.CraftSources!) {
            if (index < source.ContainerSize)
                return new SlotKey(source, index);
            index -= source.ContainerSize;
        }
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    private sealed class RequestState {
        internal Container Inventory = null!;
        internal Container Cursor = null!;
        internal Container? OpenedContainer;
        internal int ClientRequestNetId;
        internal int? MineBlockNetId;
        internal bool BlockDestroyed;
        internal PlayerInventoryTrait? InventoryTrait;
        internal CraftingRecipe? Recipe;
        internal byte CraftCount;
        internal int[]? CraftConsumption;
        internal int[]? CraftConsumed;
        internal Container[]? CraftSources;
        internal bool AutoCraft;
        internal SlotKey? AutoCraftOutputSlot;
        internal CraftResultsActionData? CraftResults;
        internal Container? CreatedOutput;
        internal readonly Dictionary<SlotKey, ItemStack?> Staged = [];
        internal readonly Dictionary<SlotKey, int> StagedClientRequestNetIds = [];
        internal readonly List<SlotKey> ChangedSlots = [];
        internal readonly HashSet<SlotKey> ChangedSlotSet = [];
        internal readonly List<ResponseSlotKey> ResponseSlots = [];
        internal readonly HashSet<ResponseSlotKey> ResponseSlotSet = [];
        internal readonly List<PendingDrop> DroppedItems = [];
        internal string? FailureReason;
    }

    private readonly record struct PendingDrop(ItemStack Item, bool Randomly);
    private readonly record struct SlotKey(Container Container, int Slot);
    private readonly record struct ResponseSlotKey(
        SlotKey SlotKey,
        ContainerEnumName ContainerName,
        uint? DynamicId,
        int DurabilityCorrection = 0
    );
    private readonly record struct ContainerNameKey(ContainerEnumName Name, uint? DynamicId);
}
