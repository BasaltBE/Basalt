namespace Basalt.Core.Network.Handlers;

using Basalt.Core.Containers;
using Basalt.Core.Crafting;
using Basalt.Core.Item;
using Basalt.Core.Item.Traits;
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
        Dictionary<Container, ItemStack?[]> snapshots = new(ReferenceEqualityComparer.Instance);
        int experienceLevel = player.ExperienceLevel;
        int experience = player.Experience;
        int totalExperience = player.TotalExperience;
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is not null) {
            snapshots[inventory.Container] = [.. inventory.Container.Storage.Select(item => item?.Clone())];
        }

        HashSet<Container> openedContainers = new(ReferenceEqualityComparer.Instance);
        foreach (Container? container in player.openedContainers.Values) {
            if (container is not null && openedContainers.Add(container)) {
                snapshots[container] = [.. container.Storage.Select(item => item?.Clone())];
            }
        }

        byte result = Success;

        foreach (ItemStackRequestAction action in request.Actions) {
            if (!HandleAction(player, action, request.StringsToFilter, changes)) {
                Logger.Warn(
                    "ItemStackRequest rejected player:{0} action:{1} source:{2}:{3} destination:{4}:{5} sourceItem:{6} destinationItem:{7}",
                    player.Username,
                    action.Type,
                    action.Source.Container.ContainerName,
                    action.Source.Slot,
                    action.Destination.Container.ContainerName,
                    action.Destination.Slot,
                    DescribeItem(player, action.Source),
                    DescribeItem(player, action.Destination));
                result = Error;
                foreach (KeyValuePair<Container, ItemStack?[]> snapshotEntry in snapshots) {
                    Container container = snapshotEntry.Key;
                    ItemStack?[] snapshot = snapshotEntry.Value;
                    for (int slot = 0; slot < snapshot.Length; slot++) {
                        if (snapshot[slot] is { } item) {
                            container.SetItem(slot, item);
                        }
                        else {
                            container.ClearSlot(slot);
                        }
                    }
                }
                player.RestoreExperience(experienceLevel, experience, totalExperience);
                player.Attributes.Send(true);
                break;
            }
        }

        return new ItemStackResponseInfo {
            Result = result,
            ClientRequestId = request.ClientRequestId,
            Containers = result == Success && changes.Count > 0 ? [.. changes.Values] : []
        };
    }

    private static bool HandleAction(Player.Player player, ItemStackRequestAction action, string[] stringsToFilter,
        Dictionary<ContainerEnumName, ItemStackResponseContainerInfo> changes) {
        return action.Type switch {
        ItemStackRequestActionType.Take or ItemStackRequestActionType.Place or
            ItemStackRequestActionType.PlaceInItemContainer or ItemStackRequestActionType.TakeFromItemContainer =>
            Transfer(player, action, changes),
        ItemStackRequestActionType.Swap => Swap(player, action, changes),
        ItemStackRequestActionType.Drop => Drop(player, action, changes),
        ItemStackRequestActionType.Destroy => Remove(player, action, changes),
        ItemStackRequestActionType.CraftCreative => CreateCreativeItem(player, action),
        ItemStackRequestActionType.Create or ItemStackRequestActionType.CraftResults => true,
        ItemStackRequestActionType.CraftRecipe or ItemStackRequestActionType.CraftRecipeAuto =>
            PrepareRecipeOutput(player, action),
        ItemStackRequestActionType.CraftRecipeOptional => ApplyAnvilAction(player, action, stringsToFilter),
        ItemStackRequestActionType.Consume when IsAnvilInputAction(player, action) => true,
        ItemStackRequestActionType.Consume => Remove(player, action, changes),
        ItemStackRequestActionType.CraftRepairAndDisenchant or
        ItemStackRequestActionType.CraftNonImplemented => false,
        _ => false
        };
    }

    private static bool IsAnvilInputAction(Player.Player player, ItemStackRequestAction action) {
        if (action.Source.Container.ContainerName is not (ContainerEnumName.AnvilInputContainer
            or ContainerEnumName.AnvilMaterialContainer)) {
            return false;
        }

        foreach (Container candidate in player.openedContainers.Values) {
            if (candidate is Basalt.Core.Blocks.Container.BlockContainer blockContainer &&
                blockContainer.Type == ContainerType.ANVIL) {
                return true;
            }
        }

        return false;
    }

    private static bool ApplyAnvilAction(Player.Player player, ItemStackRequestAction action, string[] stringsToFilter) {
        if (player.Dimension is not { } dimension) return false;

        string name = action.FilteredStringIndex >= 0 && action.FilteredStringIndex < stringsToFilter.Length
            ? stringsToFilter[action.FilteredStringIndex]
            : string.Empty;

        foreach (Container candidate in player.openedContainers.Values) {
            if (candidate is not Basalt.Core.Blocks.Container.BlockContainer blockContainer ||
                blockContainer.Type != ContainerType.ANVIL ||
                dimension.GetBlock(blockContainer.Position.X, blockContainer.Position.Y, blockContainer.Position.Z)
                    ?.GetTrait<Basalt.Core.Blocks.Traits.AnvilTrait>() is not { } anvil) {
                continue;
            }

            anvil.SetRename(name);
            return true;
        }

        return false;
    }

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
        if (action.Source.Container.ContainerName is ContainerEnumName.CreatedOutputContainer
            or ContainerEnumName.AnvilResultPreviewContainer) {
            if (!ResolveSlot(player, action.Destination, out Container outputDestination,
                out int outputDestinationSlot)) {
                return false;
            }

            foreach (Container candidate in player.openedContainers.Values) {
                if (candidate is not Basalt.Core.Blocks.Container.BlockContainer anvilContainer ||
                    anvilContainer.Type != ContainerType.ANVIL ||
                    anvilContainer.Dimension?.GetBlock(
                        anvilContainer.Position.X,
                        anvilContainer.Position.Y,
                        anvilContainer.Position.Z)?.GetTrait<Basalt.Core.Blocks.Traits.AnvilTrait>()
                        is not { } anvil) {
                    continue;
                }

                anvil.RefreshResult();
                if (anvil.Result is not { } result) {
                    return true;
                }
                int experienceCost = Math.Max(0, anvil.ExperienceCost - 1);
                int experienceLevelBefore = player.ExperienceLevel;
                if (player.Gamemode != GameType.Creative && player.ExperienceLevel < experienceCost) {
                    return false;
                }

                ItemStack movedResult = result.Clone();

                ItemStack? outputDestinationItem = outputDestination.GetItem(outputDestinationSlot);
                if (outputDestinationItem is not null) {
                    if (!movedResult.CanStackWith(outputDestinationItem)) return false;
                    int available = outputDestinationItem.Type.MaxStackSize - outputDestinationItem.StackSize;
                    if (available <= 0) return false;

                    outputDestinationItem.IncrementStack((ushort)Math.Min(available, movedResult.StackSize));
                    outputDestination.UpdateSlot(outputDestinationSlot);
                }
                else {
                    outputDestination.SetItem(outputDestinationSlot, movedResult);
                }

                anvil.CompleteResult();
                if (player.Gamemode != GameType.Creative) {
                    player.AddExperienceLevels(-experienceCost);
                    player.Attributes.Send(true);
                }
                Logger.Info(
                    $"Anvil charge player:{player.Username} cost:{experienceCost} " +
                    $"level:{experienceLevelBefore}->{player.ExperienceLevel}");
                player.GetTrait<EntityInventoryTrait>()?.Container.Update();
                ItemStack? storedResult = outputDestination.GetItem(outputDestinationSlot);
                // Logger.Info(
                //     "Anvil result moved player:{0} destination:{1} slot:{2} item:{3} count:{4}",
                //     player.Username,
                //     outputDestination.Type,
                //     outputDestinationSlot,
                //     storedResult?.Identifier ?? "empty",
                //     storedResult?.StackSize ?? 0);
                Record(changes, action.Destination, outputDestination, outputDestinationSlot);
                return true;
            }

            return false;
        }

        if (!ResolveSlot(player, action.Source, out Container source, out int sourceSlot) ||
            !ResolveSlot(player, action.Destination, out Container destination, out int destinationSlot)) {
            return false;
        }

        if (ReferenceEquals(source, destination) && sourceSlot == destinationSlot) {
            return true;
        }

        ItemStack? sourceItem = source.GetItem(sourceSlot);
        if (sourceItem is null) {
            if (action.Source.Container.ContainerName is ContainerEnumName.CreatedOutputContainer
                or ContainerEnumName.AnvilResultPreviewContainer
                or ContainerEnumName.AnvilInputContainer
                or ContainerEnumName.AnvilMaterialContainer) {
                return true;
            }

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
        if (slotInfo.Container.ContainerName is ContainerEnumName.CreatedOutputContainer
            or ContainerEnumName.AnvilResultPreviewContainer) {
            foreach (Container candidate in player.openedContainers.Values) {
                if (candidate is not Basalt.Core.Blocks.Container.BlockContainer anvilContainer ||
                    anvilContainer.Type != ContainerType.ANVIL ||
                    anvilContainer.Dimension?.GetBlock(
                        anvilContainer.Position.X,
                        anvilContainer.Position.Y,
                        anvilContainer.Position.Z)?.GetTrait<Basalt.Core.Blocks.Traits.AnvilTrait>() is not { } anvil) {
                    continue;
                }

                anvil.RefreshResult();
                if (anvil.Result is not null) {
                    container = candidate;
                    slot = 0;
                    return true;
                }
            }

            if (slotInfo.Container.ContainerName == ContainerEnumName.CreatedOutputContainer) {
                container = player.GetTrait<Basalt.Core.Player.Traits.PlayerCursorTrait>()?.Container!;
                slot = 0;
                return container is not null && container.GetItem(slot) is not null;
            }

            container = null!;
            slot = -1;
            return false;
        }

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

        if (slotInfo.Container.ContainerName == ContainerEnumName.AnvilInputContainer) {
            slot = 0;
        }
        else if (slotInfo.Container.ContainerName == ContainerEnumName.AnvilMaterialContainer) {
            slot = 1;
        }
        return slot >= 0 && slot < container.GetSize();
    }

    private static string DescribeItem(Player.Player player, SlotInfoData slotInfo) {
        if (!ResolveSlot(player, slotInfo, out Container container, out int slot)) {
            return "invalid";
        }

        return container.GetItem(slot) is { } item
            ? $"{item.Identifier}x{item.StackSize}"
            : "empty";
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
        SlotInfoData slotInfo, Container container, int slot, ItemStack? itemOverride = null) {
        ItemStack? item = itemOverride ?? container.GetItem(slot);
        bool resultContainer = slotInfo.Container.ContainerName is ContainerEnumName.CreatedOutputContainer
            or ContainerEnumName.AnvilResultPreviewContainer;
        byte responseSlot = slotInfo.Container.ContainerName switch {
            ContainerEnumName.AnvilInputContainer => 0,
            ContainerEnumName.AnvilMaterialContainer => 1,
            _ when resultContainer => 0,
            _ => (byte)slotInfo.Slot
        };
        if (!changes.TryGetValue(slotInfo.Container.ContainerName, out ItemStackResponseContainerInfo? response)) {
            response = new ItemStackResponseContainerInfo { Container = slotInfo.Container };
            changes.Add(slotInfo.Container.ContainerName, response);
        }

        response.Slots = [new ItemStackResponseSlotInfo {
            Slot = responseSlot,
            HotbarSlot = responseSlot,
            Count = (byte)(item?.StackSize ?? 0),
            ItemStackId = item?.NetworkStackId,
            CustomName = string.Empty,
            FilteredCustomName = string.Empty,
            DurabilityCorrection = item?.GetTrait<ItemStackDurabilityTrait>()?.GetCurrentDamage() ?? 0
        }];

    }
}
