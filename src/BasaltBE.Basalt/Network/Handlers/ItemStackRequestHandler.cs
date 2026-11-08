using BasaltBE.Basalt.Crafting;
using BasaltBE.Basalt.Blocks.Traits;
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
        if (player is null) throw new InvalidDataException("Item stack request arrived before player login.");
        if (player.Disposed) return;

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

        player.SendPacket(new ItemStackResponsePacket {
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

            if (player.OpenedContainer is EnchantingTableContainer enchantingTable &&
                Array.Exists(request.Actions, static action => action is CraftRecipeActionData)) {
                ItemStackResponseInfo enchantingResponse = enchantingTable.ProcessRequest(request, out failureReason);
                if (enchantingResponse.Result != ItemStackNetResult.Success) {
                    enchantingTable.Update();
                    inventory.Inventory.Update();
                    inventory.Cursor.Update();
                }

                return enchantingResponse;
            }

            ItemStackResponseInfo response = ProcessRequest(inventory, request, mineBlockNetId, blockDestroyed, out failureReason);
            if (response.Result != ItemStackNetResult.Success) {
                inventory.Inventory.Update();
                inventory.Cursor.Update();
                if (player.OpenedContainer is EnchantingTableContainer openedEnchantingTable) openedEnchantingTable.Update();
                if (player.OpenedContainer is GrindstoneContainer openedGrindstone) openedGrindstone.Update();
                if (player.OpenedContainer is AnvilContainer openedAnvil) openedAnvil.Update();
                if (TouchesArmor(request)) inventory.Entity.GetTrait<EntityArmorTrait>()?.Armor.Update();
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
        state.GrindstoneCrafted = false;
        state.GrindstoneOutputTaken = false;
        state.GrindstoneExperience = 0;
        state.AnvilCrafted = false;
        state.AnvilOutputTaken = false;
        state.AnvilLevelCost = 0;
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

            if (state.GrindstoneCrafted) {
                if (!state.GrindstoneResults || !state.GrindstoneOutputTaken) {
                    failureReason = "grindstone result was not completed";
                    return ErrorResponse(request.ClientRequestId);
                }
                for (int slot = 0; slot < 2; slot++) {
                    SlotKey key = new(state.Grindstone!, slot);
                    if (state.Staged.GetValueOrDefault(key, key.Container.GetItem(slot)) is null) continue;
                    SetItem(
                        state,
                        key,
                        null,
                        slot == 0 ? ContainerEnumName.GrindstoneInput : ContainerEnumName.GrindstoneAdditional,
                        state.Grindstone!.DynamicId
                    );
                }
            }

            if (state.AnvilCrafted) {
                if (!state.AnvilOutputTaken) {
                    failureReason = "anvil result was not taken";
                    return ErrorResponse(request.ClientRequestId);
                }
                if (state.AnvilLevelCost > 0 && inventoryTrait?.Entity is Player experiencePlayer &&
                    experiencePlayer.GameType != GameType.Creative) {
                    PlayerExperienceTrait? experience = experiencePlayer.GetTrait<PlayerExperienceTrait>();
                    if (experience is null || experience.Level < state.AnvilLevelCost) {
                        failureReason = $"not enough experience levels: need {state.AnvilLevelCost}";
                        return ErrorResponse(request.ClientRequestId);
                    }
                }
                for (int slot = 0; slot < 2; slot++) {
                    SlotKey key = new(state.Anvil!, slot);
                    if (state.Staged.GetValueOrDefault(key, key.Container.GetItem(slot)) is null) continue;
                    SetItem(
                        state,
                        key,
                        null,
                        slot == 0 ? ContainerEnumName.AnvilInput : ContainerEnumName.AnvilMaterial,
                        state.Anvil!.DynamicId
                    );
                }
            }

            if (state.Recipe is not null && !ValidateCraft(state)) {
                failureReason = "craft validation";
                return ErrorResponse(request.ClientRequestId);
            }

            EntityArmorTrait? armor = inventoryTrait?.Entity.GetTrait<EntityArmorTrait>();
            foreach (SlotKey key in state.ChangedSlots) {
                if (!ReferenceEquals(key.Container, armor?.Armor) || state.Staged[key] is not { } item) continue;
                if (item.Type.ArmorSlot != (ArmorSlot)key.Slot) {
                    failureReason = "item does not fit armor slot";
                    return ErrorResponse(request.ClientRequestId);
                }
            }

            if (inventoryTrait?.Entity is Player actor && actor.Network.Server.Events.HasSubscribers<ContainerTransactionEvent>()) {
                int changeCount = state.ChangedSlots.Count;
                if (state.CreatedOutput is { } output && state.ChangedSlotSet.Contains(new SlotKey(output, 0))) changeCount--;
                ContainerSlotChange[] changes = new ContainerSlotChange[changeCount];
                int changeIndex = 0;
                foreach (SlotKey key in state.ChangedSlots) {
                    if (ReferenceEquals(key.Container, state.CreatedOutput)) continue;
                    changes[changeIndex++] = new(key.Container, key.Slot, key.Container.GetItem(key.Slot), state.Staged[key]);
                }
                ContainerTransactionEvent transaction = new(actor, request.ClientRequestId, changes);
                actor.Network.Server.Events.Emit(transaction);
                if (transaction.IsCanceled() || actor.Disposed) return ErrorResponse(request.ClientRequestId);
            }

            bool armorChanged = false;
            foreach (SlotKey key in state.ChangedSlots) {
                if (ReferenceEquals(key.Container, state.CreatedOutput)) continue;
                key.Container.SetItem(key.Slot, state.Staged[key]);
                armorChanged |= ReferenceEquals(key.Container, armor?.Armor);
                inventoryTrait?.RememberClientRequestNetId(
                    key.Container,
                    key.Slot,
                    clientRequestNetId,
                    state.Staged[key]
                );
            }

            if (armorChanged) armor?.SyncEquipment();

            if (state.GrindstoneExperience > 0 && inventoryTrait?.Entity is Player grinder)
                grinder.GetTrait<PlayerExperienceTrait>()?.AddPoints(state.GrindstoneExperience);
            if (state.AnvilLevelCost > 0 && inventoryTrait?.Entity is Player anvilPlayer &&
                anvilPlayer.GameType != GameType.Creative)
                anvilPlayer.GetTrait<PlayerExperienceTrait>()?.SpendLevels(state.AnvilLevelCost);
            if (state.AnvilCrafted) state.Anvil?.Use();

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
                byte slot = responseKey.ContainerName switch {
                    ContainerEnumName.CreatedOutput => 50,
                    _ => checked((byte)responseKey.SlotKey.Container.GetNetworkSlot(responseKey.SlotKey.Slot))
                };
                slots.Add(new ItemStackResponseSlotInfo {
                    RequestedSlot = slot,
                    Slot = slot,
                    Amount = checked((byte)(item?.StackSize ?? 0)),
                    ItemStackNetId = item?.NetId,
                    DurabilityCorrection = item?.Damage ?? 0
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
        } finally {
            GrindstoneContainer? grindstone = state.Grindstone;
            if (grindstone is not null) grindstone.Crafting = false;
            AnvilContainer? anvil = state.Anvil;
            if (anvil is not null) anvil.Crafting = false;
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
            state.CraftInputItems = null;
            state.CraftConsumed = null;
            state.CraftSources = null;
            state.AutoCraft = false;
            state.AutoCraftOutputSlot = null;
            state.CraftResults = null;
            state.CreativeCraft = false;
            state.GrindstoneCrafted = false;
            state.GrindstoneOutputTaken = false;
            state.GrindstoneExperience = 0;
            state.GrindstoneResults = false;
            state.PendingGrindstoneResults = null;
            state.GrindstonePreview = null;
            state.Grindstone = null;
            state.AnvilCrafted = false;
            state.AnvilOutputTaken = false;
            state.AnvilLevelCost = 0;
            state.AnvilResults = false;
            state.PendingAnvilResults = null;
            state.AnvilPreview = null;
            state.Anvil = null;
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
            grindstone?.Refresh();
            anvil?.Refresh();
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
        CraftRecipeOptionalActionData => PrepareAnvilCraft(state),
        CraftRepairAndDisenchantActionData craft => PrepareGrindstoneCraft(state, craft),
        CraftNonImplementedActionData => PrepareAnvilCraft(state),
        CraftCreativeActionData craft => CreateCreativeItem(state, craft),
        CreateActionData create => state.CreativeCraft || CreateCraftResult(state, create.ResultsIndex),
        CraftResultsActionData results => state.CreativeCraft ||
            (state.OpenedContainer is AnvilContainer
                ? RecordAnvilResults(state, results)
                : state.OpenedContainer is GrindstoneContainer
                ? RecordGrindstoneResults(state, results)
                : RecordCraftResults(state, results)),
        MineBlockActionData mineBlock => MineBlock(state, mineBlock),
        _ => false
    };

    private static bool PrepareAnvilCraft(RequestState state) {
        if (state.OpenedContainer is not AnvilContainer anvil || state.AnvilCrafted) {
            state.FailureReason = "anvil is not open";
            return false;
        }

        ItemStack? first = state.Staged.GetValueOrDefault(new SlotKey(anvil, 0), anvil.GetItem(0));
        ItemStack? second = state.Staged.GetValueOrDefault(new SlotKey(anvil, 1), anvil.GetItem(1));
        ItemStack? preview = anvil.GetItem(2);
        if (state.InventoryTrait?.Entity is not Player player) {
            state.FailureReason = "anvil player is unavailable";
            return false;
        }

        ItemStack? result = AnvilContainer.CreateResult(
            first,
            second,
            player.Network.Server.Enchantments,
            player.GameType == GameType.Creative,
            out int levelCost
        );
        if (result is null || preview is null || !result.CanStackWith(preview)) {
            state.FailureReason = "anvil has no valid result preview";
            return false;
        }

        SetItem(state, new SlotKey(anvil, 2), preview.Clone(), ContainerEnumName.AnvilResultPreview, anvil.DynamicId);
        anvil.Crafting = true;
        state.Anvil = anvil;
        state.AnvilPreview = preview.Clone();
        state.AnvilCrafted = true;
        state.AnvilLevelCost = levelCost;
        return state.PendingAnvilResults is null || ValidateAnvilResults(state, state.PendingAnvilResults);
    }

    private static bool RecordAnvilResults(RequestState state, CraftResultsActionData results) {
        if (state.PendingAnvilResults is not null || state.AnvilResults) {
            state.FailureReason = "anvil result was reported more than once";
            return false;
        }
        if (state.AnvilCrafted) return ValidateAnvilResults(state, results);

        state.PendingAnvilResults = results;
        return true;
    }

    private static bool ValidateAnvilResults(RequestState state, CraftResultsActionData results) {
        if (state.OpenedContainer is not AnvilContainer || results.NumberOfCrafts != 1 ||
            results.CraftResults.Length != 1 || state.AnvilPreview is not { } preview ||
            results.CraftResults[0] is not { ItemDescriptor: ItemNameDescriptorData descriptor } reported ||
            descriptor.FullName != preview.Type.Identifier || reported.StackSize != preview.StackSize ||
            descriptor.AuxValue != 0x7FFF && unchecked((uint)descriptor.AuxValue) != preview.Metadata) {
            state.FailureReason = "reported anvil result does not match its preview";
            return false;
        }

        state.AnvilResults = true;
        state.PendingAnvilResults = null;
        return true;
    }

    private static bool PrepareGrindstoneCraft(RequestState state, CraftRepairAndDisenchantActionData action) {
        if (state.OpenedContainer is not GrindstoneContainer grindstone || state.GrindstoneCrafted) {
            state.FailureReason = "grindstone is not open";
            return false;
        }
        if (action.NumberOfRequestedCrafts != 1) {
            state.FailureReason = "grindstone requires one craft";
            return false;
        }

        ItemStack? first = state.Staged.GetValueOrDefault(new SlotKey(grindstone, 0), grindstone.GetItem(0));
        ItemStack? second = state.Staged.GetValueOrDefault(new SlotKey(grindstone, 1), grindstone.GetItem(1));
        ItemStack? preview = grindstone.GetItem(2);
        ItemStack? result = grindstone.CreateResult(first, second, out int experience);
        if (result is null || preview is null) {
            state.FailureReason = "grindstone has no valid result preview";
            return false;
        }

        SetItem(state, new SlotKey(grindstone, 2), preview.Clone(), ContainerEnumName.GrindstoneResultPreview, grindstone.DynamicId);
        grindstone.Crafting = true;
        state.Grindstone = grindstone;
        state.GrindstonePreview = preview.Clone();
        state.GrindstoneCrafted = true;
        state.GrindstoneExperience = experience;
        return state.PendingGrindstoneResults is null || ValidateGrindstoneResults(state, state.PendingGrindstoneResults);
    }

    private static bool RecordGrindstoneResults(RequestState state, CraftResultsActionData results) {
        if (state.PendingGrindstoneResults is not null || state.GrindstoneResults) {
            state.FailureReason = "grindstone result was reported more than once";
            return false;
        }
        if (state.GrindstoneCrafted) return ValidateGrindstoneResults(state, results);

        state.PendingGrindstoneResults = results;
        return true;
    }

    private static bool ValidateGrindstoneResults(RequestState state, CraftResultsActionData results) {
        if (state.OpenedContainer is not GrindstoneContainer || results.NumberOfCrafts != 1 ||
            results.CraftResults.Length != 1 ||
            state.GrindstonePreview is not { } preview ||
            results.CraftResults[0] is not { ItemDescriptor: ItemNameDescriptorData descriptor } reported ||
            descriptor.FullName != preview.Type.Identifier || reported.StackSize != preview.StackSize ||
            descriptor.AuxValue != 0x7FFF && unchecked((uint)descriptor.AuxValue) != preview.Metadata) {
            state.FailureReason = "reported grindstone result does not match its preview";
            return false;
        }

        state.GrindstoneResults = true;
        state.PendingGrindstoneResults = null;
        return true;
    }

    private static bool CreateCreativeItem(RequestState state, CraftCreativeActionData action) {
        SlotKey cursor = new(state.Cursor, 0);
        if (state.InventoryTrait?.Entity is not Player { GameType: GameType.Creative }) {
            state.FailureReason = "player is not in Creative";
            return false;
        }

        if (ItemPalette.CreateCreativeItem(action.CreativeItemNetId) is not { } item) {
            state.FailureReason = $"creative item ID {action.CreativeItemNetId} was not sent";
            return false;
        }

        state.CreativeCraft = true;
        SetItem(state, cursor, item, ContainerEnumName.Cursor, state.Cursor.DynamicId);
        return true;
    }

    private static bool MineBlock(RequestState state, MineBlockActionData action) {
        if (!state.BlockDestroyed || state.MineBlockNetId is not int expectedNetId ||
            (uint)action.Slot >= 9 || state.InventoryTrait?.SelectedSlot != action.Slot ||
            expectedNetId != action.NetIdVariant) {
            state.FailureReason = "block break or held item prediction did not match";
            return false;
        }

        SlotKey key = new(state.Inventory, action.Slot);
        ItemStack? item = state.Inventory.GetItem(action.Slot);
        int correction = item?.Damage ?? 0;
        if (correction is < short.MinValue or > short.MaxValue) {
            state.FailureReason = "durability correction is out of range";
            return false;
        }

        state.Staged[key] = item?.Clone();
        ResponseSlotKey responseKey = new(key, ContainerEnumName.Hotbar, null);
        if (state.ResponseSlotSet.Add(responseKey)) state.ResponseSlots.Add(responseKey);
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
        ItemStack?[] items;
        int[] consumption;
        if (autoCraft) {
            Container? workbench = state.OpenedContainer is { Type: ContainerType.Workbench, ContainerName: ContainerEnumName.CraftingInput } table
                ? table
                : null;
            if (workbench is not null && state.InventoryTrait is { } inventoryTrait)
                sources = [workbench, inventoryTrait.CraftingGrid, state.Inventory];
            else if (workbench is not null) sources = [workbench, state.Inventory];
            else if (state.InventoryTrait is { } playerInventory) sources = [playerInventory.CraftingGrid, state.Inventory];
            else sources = [state.Inventory];

            items = new ItemStack?[sources.Sum(static source => source.ContainerSize)];
            int itemIndex = 0;
            foreach (Container source in sources) {
                for (int slot = 0; slot < source.ContainerSize; slot++) {
                    SlotKey key = new(source, slot);
                    items[itemIndex++] = state.Staged.GetValueOrDefault(key, source.GetItem(slot));
                }
            }

            consumption = new int[items.Length];
            if (!recipe.TryMatchInventory(items, crafts, consumption)) {
                state.FailureReason = $"recipe {netId} does not match available crafting ingredients";
                return false;
            }
        } else {
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
            items = new ItemStack?[grid.ContainerSize];
            for (int i = 0; i < items.Length; i++) {
                SlotKey key = new(grid, i);
                items[i] = state.Staged.GetValueOrDefault(key, grid.GetItem(i));
            }

            consumption = new int[items.Length];
            if (!recipe.TryMatch(items, gridWidth, crafts, consumption)) {
                string gridItems = string.Join(", ", items.Select((item, slot) =>
                    item is null
                        ? $"{slot}:empty"
                        : $"{slot}:{item.Type.Identifier} x{item.StackSize} metadata:{item.Metadata}"));
                state.FailureReason = $"recipe {netId} does not match crafting grid items (grid: {gridItems})";
                return false;
            }
            sources = [grid];
        }

        state.Recipe = recipe;
        state.CraftCount = crafts;
        state.CraftConsumption = consumption;
        state.CraftInputItems = new ItemStack?[items.Length];
        for (int i = 0; i < items.Length; i++)
            state.CraftInputItems[i] = items[i]?.Clone();
        state.CraftConsumed = new int[consumption.Length];
        state.CraftSources = sources;
        state.AutoCraft = autoCraft;
        return true;
    }

    private static bool CreateCraftResult(RequestState state, byte resultsIndex) {
        if (state.Recipe is not { } recipe || state.CreatedOutput is not null || resultsIndex != 0) return false;

        Container output = new(
            1,
            ContainerId.PlayerOnlyUi,
            ContainerEnumName.CreatedOutput,
            ContainerType.Inventory,
            slotOffset: 50
        );
        ItemStack result = new(recipe.Result, (ushort)(recipe.ResultCount * state.CraftCount), recipe.ResultMetadata);
        if (recipe.Result.Identifier.EndsWith("_shulker_box", StringComparison.Ordinal) &&
            state.CraftSources is not null && state.CraftConsumption is { } consumption) {
            for (int index = 0; index < consumption.Length; index++) {
                if (consumption[index] == 0) continue;

                SlotKey ingredientKey = GetCraftSourceKey(state, index);
                if (ingredientKey.Container.GetItem(ingredientKey.Slot) is not { } ingredient ||
                    !ingredient.Type.Identifier.EndsWith("_shulker_box", StringComparison.Ordinal))
                    continue;

                result.Nbt = ingredient.Clone().Nbt;
                break;
            }
        }
        state.CreatedOutput = output;
        SetItem(state, new SlotKey(output, 0), result, ContainerEnumName.CreatedOutput, 0);
        return true;
    }

    private static bool RecordCraftResults(RequestState state, CraftResultsActionData results) {
        if (state.Recipe is not { } recipe || state.CraftResults is not null || results.CraftResults.Length != 1) {
            state.FailureReason = "craft results do not match the selected recipe";
            return false;
        }
        if (results.NumberOfCrafts == 0) {
            state.FailureReason = "craft result count is zero";
            return false;
        }
        if (state.CraftConsumed is null || state.CraftConsumed.Any(static consumed => consumed != 0)) {
            state.FailureReason = "craft results arrived after ingredient consumption";
            return false;
        }

        int crafts = results.NumberOfCrafts;
        int resultSize = recipe.ResultCount * crafts;
        if (resultSize > recipe.Result.MaxAmount) {
            state.FailureReason = "craft result exceeds the item stack limit";
            return false;
        }

        int[] consumption = new int[state.CraftConsumption!.Length];
        ItemStack?[] items = state.CraftInputItems!;
        bool matched = state.AutoCraft
            ? recipe.TryMatchInventory(items, crafts, consumption)
            : recipe.TryMatch(items, state.CraftSources![0].ContainerSize == 9 ? 3 : 2, crafts, consumption);
        if (!matched) {
            state.FailureReason = "craft result count exceeds available ingredients";
            return false;
        }

        NetworkItemInstanceDescriptorData result = results.CraftResults[0];
        if (result.ItemDescriptor is not ItemNameDescriptorData descriptor ||
            descriptor.FullName != recipe.Result.Identifier || result.StackSize != resultSize ||
            descriptor.AuxValue != 0x7FFF && unchecked((uint)descriptor.AuxValue) != recipe.ResultMetadata) {
            state.FailureReason = "reported craft result does not match the recipe output";
            return false;
        }

        state.CraftCount = (byte)crafts;
        state.CraftConsumption = consumption;
        state.CraftConsumed = new int[consumption.Length];
        if (state.CreatedOutput is null) {
            if (!CreateCraftResult(state, 0)) return false;
        } else {
            SlotKey outputKey = new(state.CreatedOutput, 0);
            ItemStack output = new(recipe.Result, (ushort)resultSize, recipe.ResultMetadata);
            if (state.Staged.GetValueOrDefault(outputKey) is { } previousOutput) output.Nbt = previousOutput.Nbt;
            SetItem(
                state,
                outputKey,
                output,
                ContainerEnumName.CreatedOutput,
                0
            );
        }

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
                if (state.CraftConsumed![index] != consumption[index] || isAutoCraftOutput && consumption[index] != 0) return false;

                ItemStack? original = state.CraftInputItems![index];
                if (isAutoCraftOutput) continue;

                ItemStack? current = state.Staged.TryGetValue(key, out ItemStack? staged) ? staged : original;
                int consumed = consumption[index];
                if (consumed == 0) {
                    if (!SameStack(original, current)) return false;
                } else if (original is null || current is null && original.StackSize != consumed ||
                    current is not null && (current.Type != original.Type || current.StackSize != original.StackSize - consumed)) {
                    return false;
                }
            }
        }

        if (state.CreatedOutput is not { } output ||
            state.Staged.GetValueOrDefault(new SlotKey(output, 0)) is not null)
            return false;

        if (state.CraftResults is not { } reported) return true;

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
        if (sourceKey.Container is GrindstoneContainer grindstone && sourceKey.Slot == 2) {
            if (!state.GrindstoneCrafted || destinationKey.Container == grindstone || amount != 1) {
                state.FailureReason = "grindstone result transfer is invalid";
                return false;
            }
            state.GrindstoneOutputTaken = true;
        }
        if (sourceKey.Container is AnvilContainer anvil && sourceKey.Slot == 2) {
            if (!state.AnvilCrafted || destinationKey.Container == anvil || amount != 1) {
                state.FailureReason = "anvil result transfer is invalid";
                return false;
            }
            state.AnvilOutputTaken = true;
        }
        if (state.Recipe is not null && touchesCraftGrid && !autoCraftOutput) {
            state.FailureReason = "recipe transfer touches the crafting input";
            return false;
        }
        if (sourceKey == destinationKey) return true;
        if (sourceItem is null || amount > sourceItem.StackSize) {
            state.FailureReason = "source stack is empty or smaller than the transfer";
            return false;
        }
        if (destinationKey.Container is GrindstoneContainer && destinationKey.Slot == 2 ||
            destinationKey.Container is AnvilContainer && destinationKey.Slot == 2) {
            state.FailureReason = "container result slot is read-only";
            return false;
        }
        if (!destinationKey.Container.CanContain(destinationKey.Slot, sourceItem)) {
            state.FailureReason = $"{sourceItem.Type.Identifier} is not allowed in {destinationKey.Container.ContainerName}";
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
            if (autoCraftOutput) state.AutoCraftOutputSlot = destinationKey;
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
        if (autoCraftOutput) state.AutoCraftOutputSlot = destinationKey;
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

        if (sourceKey == destinationKey) return true;

        if (sourceKey.Container is GrindstoneContainer && sourceKey.Slot == 2 ||
            destinationKey.Container is GrindstoneContainer && destinationKey.Slot == 2 ||
            sourceKey.Container is AnvilContainer && sourceKey.Slot == 2 ||
            destinationKey.Container is AnvilContainer && destinationKey.Slot == 2) {
            state.FailureReason = "container result slot is read-only";
            return false;
        }

        if ((sourceItem is not null && !destinationKey.Container.CanContain(destinationKey.Slot, sourceItem)) ||
            (destinationItem is not null && !sourceKey.Container.CanContain(sourceKey.Slot, destinationItem))) {
            state.FailureReason = "an item is not allowed in the destination container";
            return false;
        }

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
        if (state.GrindstoneCrafted &&
            (!consume || !ReferenceEquals(sourceKey.Container, state.Grindstone) || sourceKey.Slot > 1)) {
            state.FailureReason = "grindstone can only consume its input items";
            return false;
        }
        if (state.AnvilCrafted &&
            (!consume || !ReferenceEquals(sourceKey.Container, state.Anvil) || sourceKey.Slot > 1)) {
            state.FailureReason = "anvil can only consume its input items";
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
                    if (slot == sourceIndex) continue;

                    int available = state.CraftConsumption[slot] - state.CraftConsumed[slot];
                    if (available <= 0) continue;

                    SlotKey candidateKey = GetCraftSourceKey(state, slot);
                    ItemStack? candidate = state.Staged.TryGetValue(candidateKey, out ItemStack? staged)
                        ? staged
                        : candidateKey.Container.GetItem(candidateKey.Slot);
                    if (candidate is null || !candidate.CanStackWith(sourceItem)) continue;

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
        if (state.Recipe is not null && IsCraftSource(state, sourceKey.Container)) return false;

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
        if (!ResolveContainer(state, info.ContainerName, out Container? container) || container is null) {
            key = default;
            item = null;
            state.FailureReason ??= $"container {info.ContainerName.ContainerName} is unavailable " +
                $"(dynamic ID: {info.ContainerName.DynamicId?.ToString() ?? "none"})";
            return false;
        }

        if (info.ContainerName.ContainerName == ContainerEnumName.Hotbar && info.Slot >= 9) {
            key = default;
            item = null;
            state.FailureReason ??= $"hotbar slot {info.Slot} is outside the hotbar";
            return false;
        }

        bool createdOutputCursor = info.ContainerName.ContainerName == ContainerEnumName.CreatedOutput &&
            ReferenceEquals(container, state.Cursor);
        bool grindstoneOutput = info.ContainerName.ContainerName == ContainerEnumName.CreatedOutput &&
            state.GrindstoneCrafted && ReferenceEquals(container, state.Grindstone);
        bool anvilOutput = info.ContainerName.ContainerName == ContainerEnumName.CreatedOutput &&
            state.AnvilCrafted && ReferenceEquals(container, state.Anvil);
        int localSlot = createdOutputCursor ? 0 : grindstoneOutput || anvilOutput ? 2 : container.GetLocalSlot(info.Slot);
        if (state.OpenedContainer is { Type: ContainerType.Furnace or ContainerType.BlastFurnace or ContainerType.Smoker } furnace &&
            (info.ContainerName.ContainerName is ContainerEnumName.FurnaceFuel or ContainerEnumName.FurnaceIngredient or ContainerEnumName.FurnaceResult or
                ContainerEnumName.BlastFurnaceIngredient or ContainerEnumName.SmokerIngredient)) {
            int expectedSlot = info.ContainerName.ContainerName switch {
                ContainerEnumName.FurnaceFuel => 1,
                ContainerEnumName.FurnaceResult => 2,
                ContainerEnumName.BlastFurnaceIngredient when furnace.Type == ContainerType.BlastFurnace => 0,
                ContainerEnumName.SmokerIngredient when furnace.Type == ContainerType.Smoker => 0,
                ContainerEnumName.FurnaceIngredient when furnace.Type == ContainerType.Furnace => 0,
                _ => -1
            };
            if (localSlot != expectedSlot) {
                key = default;
                item = null;
                state.FailureReason ??= $"furnace slot {info.Slot} does not match {info.ContainerName.ContainerName}";
                return false;
            }
        }
        if ((uint)localSlot >= container.ContainerSize) {
            key = default;
            item = null;
            state.FailureReason ??= $"slot {info.Slot} is outside {info.ContainerName.ContainerName} " +
                $"(size: {container.ContainerSize}, offset: {container.SlotOffset})";
            return false;
        }

        key = new SlotKey(container, localSlot);
        bool cursorSlot = ReferenceEquals(container, state.Cursor);
        if (!state.Staged.TryGetValue(key, out item)) {
            item = container.GetItem(key.Slot);
            bool predictedArmorStack = container.ContainerName == ContainerEnumName.Armor &&
                info.NetIdVariant < 0 && item?.Type.ArmorSlot == (ArmorSlot)key.Slot;
            if (item is not null && !cursorSlot && !ReferenceEquals(container, state.CreatedOutput) && !createdOutputCursor &&
                !anvilOutput &&
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

        bool validStagedStack = cursorSlot || ReferenceEquals(container, state.CreatedOutput) || createdOutputCursor ||
            state.Recipe is not null && IsCraftSource(state, container) ||
            item is null ||
            (item?.NetId ?? 0) == info.NetIdVariant ||
            state.StagedClientRequestNetIds.TryGetValue(key, out int clientRequestNetId) &&
            clientRequestNetId == info.NetIdVariant;
        if (!validStagedStack) {
            state.FailureReason ??= $"stack ID mismatch in {info.ContainerName.ContainerName}[{info.Slot}] " +
                $"(client: {info.NetIdVariant}, server: {item?.NetId ?? 0})";
        }

        return validStagedStack;
    }

    private static bool ResolveContainer(RequestState state, FullContainerName name, out Container? container) {
        container = null;
        if (name.ContainerName == ContainerEnumName.Cursor) {
            container = state.Cursor;
            return true;
        }

        if (state.OpenedContainer is { } furnaceContainer &&
            furnaceContainer.Type is ContainerType.Furnace or ContainerType.BlastFurnace or ContainerType.Smoker &&
            (name.ContainerName is ContainerEnumName.FurnaceFuel or ContainerEnumName.FurnaceResult ||
                name.ContainerName == ContainerEnumName.FurnaceIngredient && furnaceContainer.Type == ContainerType.Furnace ||
                name.ContainerName == ContainerEnumName.BlastFurnaceIngredient && furnaceContainer.Type == ContainerType.BlastFurnace ||
                name.ContainerName == ContainerEnumName.SmokerIngredient && furnaceContainer.Type == ContainerType.Smoker) &&
            (name.DynamicId is null or 0)) {
            container = furnaceContainer;
            return true;
        }

        if (state.OpenedContainer is EnchantingTableContainer enchantingTable &&
            name.ContainerName is ContainerEnumName.EnchantingInput or ContainerEnumName.EnchantingMaterial &&
            (enchantingTable.DynamicId == name.DynamicId || name.DynamicId is null or 0)) {
            container = enchantingTable;
            return true;
        }

        if (state.OpenedContainer is GrindstoneContainer grindstone &&
            (name.ContainerName is ContainerEnumName.GrindstoneInput or ContainerEnumName.GrindstoneAdditional or ContainerEnumName.GrindstoneResultPreview ||
                name.ContainerName == ContainerEnumName.CreatedOutput && state.GrindstoneCrafted && state.GrindstonePreview is not null) &&
            (grindstone.DynamicId == name.DynamicId || name.DynamicId is null or 0)) {
            container = grindstone;
            return true;
        }

        if (state.OpenedContainer is AnvilContainer anvil &&
            (name.ContainerName is ContainerEnumName.AnvilInput or ContainerEnumName.AnvilMaterial or ContainerEnumName.AnvilResultPreview ||
                name.ContainerName == ContainerEnumName.CreatedOutput && state.AnvilCrafted && state.AnvilPreview is not null) &&
            (anvil.DynamicId == name.DynamicId || name.DynamicId is null or 0)) {
            container = anvil;
            return true;
        }

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

        if (name.ContainerName == ContainerEnumName.CreatedOutput &&
            (name.DynamicId is null or 0) &&
            (state.Staged.TryGetValue(new SlotKey(state.Cursor, 0), out ItemStack? stagedCursor)
                ? stagedCursor is not null
                : state.Cursor.GetItem(0) is not null)) {
            container = state.Cursor;
            return true;
        }

        if (name.DynamicId is uint dynamicId && dynamicId != 0) return false;

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
        if (name == ContainerEnumName.Cursor) dynamicId = state.Cursor.DynamicId;

        state.Staged[key] = item;
        state.StagedClientRequestNetIds[key] = state.ClientRequestNetId;
        if (state.ChangedSlotSet.Add(key)) state.ChangedSlots.Add(key);

        ResponseSlotKey responseKey = new(key, name, dynamicId);
        if (state.ResponseSlotSet.Add(responseKey)) state.ResponseSlots.Add(responseKey);
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
            if (touchesArmor) return true;
        }

        return false;
    }

    private static bool IsArmorSlot(ItemStackSlotInfoData slot) =>
        slot.ContainerName.ContainerName == ContainerEnumName.Armor;

    private static bool IsCraftSource(RequestState state, Container container) {
        if (state.CraftSources is null) return false;
        foreach (Container source in state.CraftSources) {
            if (ReferenceEquals(source, container)) return true;
        }

        return false;
    }

    private static int GetCraftSourceIndex(RequestState state, SlotKey key) {
        int index = 0;
        foreach (Container source in state.CraftSources!) {
            if (ReferenceEquals(source, key.Container)) return index + key.Slot;
            index += source.ContainerSize;
        }

        return -1;
    }

    private static SlotKey GetCraftSourceKey(RequestState state, int index) {
        foreach (Container source in state.CraftSources!) {
            if (index < source.ContainerSize) return new SlotKey(source, index);
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
        internal ItemStack?[]? CraftInputItems;
        internal int[]? CraftConsumed;
        internal Container[]? CraftSources;
        internal bool AutoCraft;
        internal SlotKey? AutoCraftOutputSlot;
        internal CraftResultsActionData? CraftResults;
        internal bool CreativeCraft;
        internal bool GrindstoneCrafted;
        internal bool GrindstoneOutputTaken;
        internal int GrindstoneExperience;
        internal bool GrindstoneResults;
        internal CraftResultsActionData? PendingGrindstoneResults;
        internal ItemStack? GrindstonePreview;
        internal GrindstoneContainer? Grindstone;
        internal bool AnvilCrafted;
        internal bool AnvilOutputTaken;
        internal int AnvilLevelCost;
        internal bool AnvilResults;
        internal CraftResultsActionData? PendingAnvilResults;
        internal ItemStack? AnvilPreview;
        internal AnvilContainer? Anvil;
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
        uint? DynamicId
    );
    private readonly record struct ContainerNameKey(ContainerEnumName Name, uint? DynamicId);
}
