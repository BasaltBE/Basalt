namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Events;
using Basalt.Core.Item;
using Basalt.Core.Item.Enchantment;
using Basalt.Core.Item.Traits;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Player.Traits;

using Basalt.BedrockProtocol.Types;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Enums;

using Basalt.Core.Blocks;
using ActorDamageCause = Basalt.BedrockProtocol.Enums.ActorDamageCause;
using ContainerId = Basalt.BedrockProtocol.Enums.ContainerId;

public static class InventoryTransaction {
    private const float EntityAttackReach = 3f;
    private const ItemUseActionType UseItemActionClickBlock = ItemUseActionType.Place;

    private static readonly HashSet<string> ReplaceableBlocks =
    [
        "minecraft:air",
        "minecraft:cave_air",
        "minecraft:void_air",
        "minecraft:water",
        "minecraft:flowing_water",
        "minecraft:lava",
        "minecraft:flowing_lava",
        "minecraft:short_grass",
        "minecraft:tall_grass",
        "minecraft:fern",
        "minecraft:large_fern",
        "minecraft:dead_bush",
        "minecraft:vine",
        "minecraft:seagrass",
        "minecraft:tall_seagrass",
        "minecraft:snow_layer",
        "minecraft:fire"
    ];

    public static void Handle(Server server, NetworkConnection connection, InventoryTransactionPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null) {
            return;
        }

        Logger.Warn(
            "InventoryTransaction player:{0} type:{1} actions:{2}",
            player.Username,
            packet.TransactionType,
            packet.Actions.Length);

        if (packet.TransactionType == InventoryTransactionType.ItemUseOnActor) {
            Logger.Warn(
                "InventoryTransaction entity player:{0} runtime:{1} action:{2} slot:{3}",
                player.Username,
                packet.ItemUseOnActorTransaction.RuntimeId,
                packet.ItemUseOnActorTransaction.ActionType,
                packet.ItemUseOnActorTransaction.Slot);
        }

        if (packet.TransactionType == InventoryTransactionType.ItemUse) {
            Logger.Warn(
                "InventoryTransaction block player:{0} action:{1} trigger:{2} prediction:{3} slot:{4} pos:{5},{6},{7} face:{8} target:{9} item:{10} count:{11}",
                player.Username,
                packet.ItemUseTransaction.ActionType,
                packet.ItemUseTransaction.TriggerType,
                packet.ItemUseTransaction.ClientInteractPrediction,
                packet.ItemUseTransaction.Slot,
                packet.ItemUseTransaction.Position.X,
                packet.ItemUseTransaction.Position.Y,
                packet.ItemUseTransaction.Position.Z,
                packet.ItemUseTransaction.Face,
                packet.ItemUseTransaction.TargetBlockId,
                packet.ItemUseTransaction.Item.Id,
                packet.ItemUseTransaction.Item.StackSize);
        }


        switch (packet.TransactionType) {
            case InventoryTransactionType.Normal:
                HandleInventoryActions(player, inventory, packet.Actions, packet.LegacySetItemSlots ?? []);
                break;

            case InventoryTransactionType.ItemUse:
                HandleUseItem(player, inventory, packet.ItemUseTransaction, packet.Actions);
                break;

            case InventoryTransactionType.ItemUseOnActor:
                HandleUseItemOnEntity(player, inventory, packet.ItemUseOnActorTransaction);
                break;

            case InventoryTransactionType.ItemRelease:
                PlayerAuthInput.CancelPendingItemUse(player);
                break;

            case InventoryTransactionType.InventoryMismatch:
                break;
        }

    }

    public static void HandleUseItemFromAuthInput(
        Player.Player player,
        PackedItemUseLegacyInventoryTransactionData data,
        float pitch,
        float yaw,
        Vec3 cameraOrientation
    ) {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null) {
            return;
        }

        player.Pitch = pitch;
        player.Yaw = yaw;
        player.HeadYaw = yaw;

        ItemUseInventoryTransactionData transaction = data.ItemUseTransaction;


        bool missingPosition =
            transaction.Position.X == 0 &&
            transaction.Position.Y == 0 &&
            transaction.Position.Z == 0 &&
            transaction.TargetBlockId == 0;

        if (missingPosition && FindBlockFromView(player, pitch, yaw, cameraOrientation, out BlockPos viewedBlock, out int viewedFace)) {
            transaction.Position = viewedBlock;
            transaction.Face = (byte)viewedFace;
        }
        else if (missingPosition) {
            transaction.Position = new BlockPos {
                X = (int)MathF.Floor(player.Location.X),
                Y = (int)MathF.Floor(player.Location.Y - 1f),
                Z = (int)MathF.Floor(player.Location.Z)
            };

            if (transaction.Face is < 0 or > 5) {
                transaction.Face = 1;
            }
        }

        HandleUseItem(player, inventory, transaction, data.Actions);
    }

    private static void HandleInventoryActions(
        Player.Player player,
        EntityInventoryTrait inventory,
        InventoryActionData[] actions,
        LegacySetSlot[] legacySetItemSlots) {
        for (int i = 0; i < actions.Length; i++) {
            InventoryActionData action = actions[i];
            if (action.Source.Type != InventorySourceType.WorldInteraction) {
                continue;
            }

            if (!SpawnWorldDrop(player, inventory, action, actions)) {
                inventory.Container.Update();
                return;
            }
        }

        foreach (InventoryActionData action in actions) {
            if (action.Source.Type == InventorySourceType.WorldInteraction) {
                continue;
            }

            if (action.Source.Type != InventorySourceType.ContainerInventory) {
                continue;
            }
            if (action.Source.ContainerId is null) continue;

            ContainerId sourceContainerId = action.Source.ContainerId.Value;
            int slot = (int)action.Slot;
            PlayerCraftingGridTrait? craftingGrid = player.GetTrait<PlayerCraftingGridTrait>();

            Containers.Container? container = null;
            if (sourceContainerId == ContainerId.Inventory &&
                slot >= PlayerCraftingGridTrait.SlotOffset &&
                slot < PlayerCraftingGridTrait.SlotOffset + PlayerCraftingGridTrait.GridSize &&
                craftingGrid is not null) {
                container = craftingGrid.Container;
                slot = PlayerCraftingGridTrait.MapSlot(slot);
            }
            else if (sourceContainerId == (inventory.Container.Identifier ?? ContainerId.Inventory)) {
                container = inventory.Container;
            }
            else if (player.TryGetOpenContainer(sourceContainerId, out Containers.Container? opened)) {
                container = opened;
            }

            if (container is null) {
                continue;
            }

            if (slot < 0 || slot >= container.GetSize()) {
                continue;
            }

            NetworkItemStackDescriptor stack = action.ToItem;
            if (stack.Id == 0 || stack.StackSize == 0) {
                container.ClearSlot(slot);
                continue;
            }

            try {
                ItemStack item = ItemStack.FromNetworkStack(stack);
                container.SetItem(slot, item);
            }
            catch {
            }
        }
    }

    private static int ResolveWorldActionSlot(
        EntityInventoryTrait inventory,
        InventoryActionData action,
        InventoryActionData[] actions,
        LegacySetSlot[] legacySetItemSlots) {
        for (int i = 0; i < legacySetItemSlots.Length; i++) {
            LegacySetSlot legacy = legacySetItemSlots[i];
            if (legacy.Slots.Length == 0) {
                continue;
            }

            if (legacy.ContainerEnum is ContainerEnumName.InventoryContainer or ContainerEnumName.HotbarContainer) {
                return legacy.Slots[0];
            }
        }

        for (int i = 0; i < actions.Length; i++) {
            InventoryActionData candidate = actions[i];
            if (candidate.Source.Type != InventorySourceType.ContainerInventory || candidate.Source.ContainerId != 0) {
                continue;
            }

            NetworkItemStackDescriptor candidateOld = candidate.FromItem;
            NetworkItemStackDescriptor candidateNew = candidate.ToItem;
            NetworkItemStackDescriptor dropped = action.ToItem;

            if (candidateOld.Id == 0 || dropped.Id == 0) {
                continue;
            }

            if (candidateOld.Id != dropped.Id || candidateOld.StackSize <= candidateNew.StackSize) {
                continue;
            }

            int delta = candidateOld.StackSize - candidateNew.StackSize;
            if (delta == dropped.StackSize) {
                return (int)candidate.Slot;
            }
        }

        if (action.Slot == 0 && inventory.SelectedSlot is >= 0 and < 9) {
            return inventory.SelectedSlot;
        }

        return (int)action.Slot;
    }

    private static bool SpawnWorldDrop(
        Player.Player player,
        EntityInventoryTrait inventory,
        InventoryActionData action,
        InventoryActionData[] actions) {
        if (player.Dimension is null) {
            Logger.Error("Drop failed: player has no dimension");
            return false;
        }

        for (int i = 0; i < actions.Length; i++) {
            InventoryActionData source = actions[i];

            int slot = (int)source.Slot;
            if (slot < 0 || slot >= inventory.Container.GetSize()) {
                continue;
            }

            ItemStack? item = inventory.Container.GetItem(slot);
            if (item is null || item.StackSize == 0) {
                return false;
            }

            int amount = source.FromItem.StackSize - source.ToItem.StackSize;
            if (amount <= 0 || amount > item.StackSize) {
                return false;
            }

            return player.DropItem(item.Clone((ushort)amount));
        }

        return false;
    }

    private static void HandleUseItem(
        Player.Player player,
        EntityInventoryTrait inventory,
        ItemUseInventoryTransactionData transaction,
        InventoryActionData[] actions) {
        if (transaction.ActionType == ItemUseActionType.Use) {
            ItemStack? airHeldItem = GetHeldItem(inventory, transaction.Slot);
            if (airHeldItem is null) {
                return;
            }

            if (player.Dimension is not null) {
                BlockPos blockPosition = transaction.Position;
                int blockFace = transaction.Face;

                if (IsEmptyPosition(blockPosition) && transaction.TargetBlockId == 0 && player.LastActionBlockPosition is not null) {
                    blockPosition = player.LastActionBlockPosition;

                    if (player.LastActionFace is >= 0 and <= 5) {
                        blockFace = player.LastActionFace;
                    }
                }

                Basalt.Core.Blocks.BlockPermutation clickedBlock =
                    player.Dimension.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

                if (clickedBlock.Type.Identifier is not "minecraft:air" and not "minecraft:cave_air" and not "minecraft:void_air") {
                    if (!CanUseItem(player, airHeldItem)) {
                        return;
                    }

                    airHeldItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                        player,
                        transaction.Slot,
                        blockPosition,
                        blockFace,
                        transaction.Position,
                        transaction.ClickPosition));
                    return;
                }
            }

            if (CanUseItem(player, airHeldItem)) {
                airHeldItem.OnUseOnAir(new ItemUseOnAirDetails(player, transaction.Slot, transaction.Position));
            }
            return;
        }

        if (transaction.ActionType != UseItemActionClickBlock) {
            return;
        }

        if (player.Dimension is not null && transaction.TargetBlockId != 0 && !IsEmptyPosition(transaction.Position)) {
            Basalt.Core.Blocks.BlockPermutation serverPermutation =
                player.Dimension.GetPermutation(transaction.Position.X, transaction.Position.Y, transaction.Position.Z);

            if ((uint)serverPermutation.NetworkId != transaction.TargetBlockId) {
                Logger.Warn(
                    "InventoryTransaction block id mismatch player:{0} pos:{1},{2},{3} client:{4} server:{5}",
                    player.Username,
                    transaction.Position.X,
                    transaction.Position.Y,
                    transaction.Position.Z,
                    transaction.TargetBlockId,
                    serverPermutation.NetworkId);
                SendBlockUpdate(player, transaction.Position, serverPermutation.NetworkId);
            }
        }

        ItemStack? heldItem = GetHeldItem(inventory, transaction.Slot);
        if (heldItem is null && transaction.Item.Id != 0 && transaction.Item.StackSize != 0) {
            for (int slot = 0; slot < 9; slot++) {
                ItemStack? candidate = inventory.Container.GetItem(slot);
                if (candidate is null || candidate.Type.NetworkId != transaction.Item.Id || candidate.Metadata != transaction.Item.AuxValue) {
                    continue;
                }

                transaction.Slot = slot;
                inventory.SetHeldItem(slot);
                heldItem = candidate;
                break;
            }
        }

        if (heldItem is null && transaction.Item.Id != 0 && transaction.Item.StackSize != 0 && transaction.Slot >= 0) {
            ItemType? transactionItemType = ItemType.GetByNetwork(transaction.Item.Id);
            if (transactionItemType is not null) {
                heldItem = new ItemStack(
                    transactionItemType,
                    transaction.Item.StackSize,
                    transaction.Item.AuxValue);
                inventory.Container.SetItem(transaction.Slot, heldItem);
            }
        }

        Logger.Warn(
            "InventoryTransaction held player:{0} slot:{1} selected:{2} item:{3}",
            player.Username,
            transaction.Slot,
            inventory.SelectedSlot,
            heldItem?.Identifier ?? "null");

        if (transaction.TriggerType == ItemUseTriggerType.SimulationTick) {
            ItemStack? repeatItem = GetHeldItem(inventory, transaction.Slot);
            if (repeatItem is null) {
                return;
            }

            if (transaction.ClientInteractPrediction != ItemUsePredictedResult.Success) {
                bool predictionPlaceableBlock = repeatItem.Type.BlockType is not null
                    || Basalt.Core.Blocks.BlockType.Get(repeatItem.Identifier) is not null;

                if (predictionPlaceableBlock) {
                    UseItemOnBlock(player, inventory, repeatItem, transaction);
                    return;
                }

                if (player.Dimension is null || !CanUseItem(player, repeatItem)) {
                    return;
                }

                repeatItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                    player,
                    transaction.Slot,
                    transaction.Position,
                    transaction.Face,
                    transaction.Position,
                    transaction.ClickPosition));
                return;
            }

            bool isPlaceableBlock = repeatItem.Type.BlockType is not null
                || Basalt.Core.Blocks.BlockType.Get(repeatItem.Identifier) is not null;

            // Holding place continuously sends Repeat; server must still commit block placement.
            if (isPlaceableBlock) {
                UseItemOnBlock(player, inventory, repeatItem, transaction);
                return;
            }

            if (player.Dimension is not null) {
                BlockPos blockPosition = transaction.Position;

                if (IsEmptyPosition(blockPosition) && transaction.TargetBlockId == 0 && player.LastActionBlockPosition is not null) {
                    blockPosition = player.LastActionBlockPosition;
                }

                if (!CanUseItem(player, repeatItem)) {
                    return;
                }

                repeatItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                    player,
                    transaction.Slot,
                    blockPosition,
                    transaction.Face,
                    transaction.Position,
                    transaction.ClickPosition));
            }
            return;
        }

        if (transaction.TriggerType == ItemUseTriggerType.PlayerInput &&
            transaction.ClientInteractPrediction != ItemUsePredictedResult.Success) {
            ItemStack? usedItem = GetHeldItem(inventory, transaction.Slot);
            if (usedItem is null || player.Dimension is null) {
                return;
            }

            BlockPos blockPosition = transaction.Position;

            if (IsEmptyPosition(blockPosition) && transaction.TargetBlockId == 0 && player.LastActionBlockPosition is not null) {
                blockPosition = player.LastActionBlockPosition;
            }

            if (!CanUseItem(player, usedItem)) {
                return;
            }

            bool placeableBlock = usedItem.Type.BlockType is not null
                || Basalt.Core.Blocks.BlockType.Get(usedItem.Identifier) is not null;

            if (placeableBlock) {
                UseItemOnBlock(player, inventory, usedItem, transaction);
                return;
            }

            usedItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                player,
                transaction.Slot,
                blockPosition,
                transaction.Face,
                transaction.Position,
                transaction.ClickPosition));
            return;
        }

        if (player.Dimension is not null) {
            BlockPos blockPosition = transaction.Position;

            if (IsEmptyPosition(blockPosition) && transaction.TargetBlockId == 0 && player.LastActionBlockPosition is not null) {
                blockPosition = player.LastActionBlockPosition;
            }

            Basalt.Core.Blocks.Block? block = player.Dimension.GetBlock(blockPosition.X, blockPosition.Y, blockPosition.Z);
            if (block is not null && block.Interactable && !player.IsSneaking) {
                Logger.Warn(
                    "InventoryTransaction interaction branch player:{0} block:{1} pos:{2},{3},{4}",
                    player.Username,
                    block.Type.Identifier,
                    blockPosition.X,
                    blockPosition.Y,
                    blockPosition.Z);
                if (!CanInteractBlock(player, blockPosition)) {
                    return;
                }

                block.OnInteract(new BlockInteractDetails(
                    player,
                    blockPosition,
                    transaction.Face,
                    transaction.ClickPosition));

                return;
            }
        }

        if (heldItem is null) {
            Logger.Warn(
                "InventoryTransaction placement rejected empty held item player:{0} slot:{1}",
                player.Username,
                transaction.Slot);
            return;
        }

        if (player.Gamemode == GameType.Survival &&
            transaction.TriggerType == ItemUseTriggerType.PlayerInput &&
            actions.Length == 0) {
            if (heldItem.Type.BlockType is null && Basalt.Core.Blocks.BlockType.Get(heldItem.Identifier) is null) {
                if (!CanUseItem(player, heldItem)) {
                    return;
                }

                heldItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                    player,
                    transaction.Slot,
                    transaction.Position,
                    transaction.Face,
                    transaction.Position,
                    transaction.ClickPosition));
                return;
            }
        }

        UseItemOnBlock(player, inventory, heldItem, transaction);
    }

    private static void UseItemOnBlock(
        Player.Player player,
        EntityInventoryTrait inventory,
        ItemStack heldItem,
        ItemUseInventoryTransactionData transaction) {
        if (player.Dimension is null) {
            return;
        }

        BlockPos clickedPosition = transaction.Position;
        int clickedFace = transaction.Face;

        if (IsEmptyPosition(clickedPosition) && transaction.TargetBlockId == 0 && player.LastActionBlockPosition is not null) {
            clickedPosition = player.LastActionBlockPosition;

            if (player.LastActionFace is >= 0 and <= 5) {
                clickedFace = player.LastActionFace;
            }
        }

        Basalt.Core.Blocks.BlockPermutation clickedBlock =
            player.Dimension.GetPermutation(clickedPosition.X, clickedPosition.Y, clickedPosition.Z);

        BlockPos placePosition = GetPlacedBlockPosition(clickedPosition, clickedFace);

        Basalt.Core.Blocks.BlockPermutation existingBlock =
            player.Dimension.GetPermutation(placePosition.X, placePosition.Y, placePosition.Z);

        Basalt.Core.Blocks.Block? blockEntity =
            player.Dimension.GetBlock(clickedPosition.X, clickedPosition.Y, clickedPosition.Z);

        if (blockEntity?.Interactable == true && !player.IsSneaking) {
            if (!CanInteractBlock(player, clickedPosition)) {
                SendBlockUpdate(player, clickedPosition, clickedBlock.NetworkId);
                return;
            }

            blockEntity.OnInteract(new BlockInteractDetails(
                player,
                clickedPosition,
                clickedFace,
                transaction.ClickPosition));

            SendBlockUpdate(player, clickedPosition, clickedBlock.NetworkId);
            return;
        }

        Basalt.Core.Blocks.BlockType? blockType = heldItem.Type.BlockType ?? Basalt.Core.Blocks.BlockType.Get(heldItem.Identifier);

        Logger.Warn(
            "InventoryTransaction placement player:{0} held:{1} itemId:{2} block:{3} clicked:{4},{5},{6} place:{7},{8},{9}",
            player.Username,
            heldItem.Identifier,
            heldItem.Type.NetworkId,
            blockType?.Identifier ?? "null",
            clickedPosition.X,
            clickedPosition.Y,
            clickedPosition.Z,
            placePosition.X,
            placePosition.Y,
            placePosition.Z);

        if (blockType is null || blockType.Identifier == "minecraft:air") {
            if (!CanUseItem(player, heldItem)) {
                return;
            }

            heldItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                player,
                transaction.Slot,
                clickedPosition,
                clickedFace,
                transaction.Position,
                transaction.ClickPosition));

            Basalt.Core.Blocks.BlockPermutation currentBlock =
                player.Dimension.GetPermutation(placePosition.X, placePosition.Y, placePosition.Z);
            SendBlockUpdate(player, placePosition, currentBlock.NetworkId);
            return;
        }

        if (existingBlock.Type.Identifier == blockType.Identifier ||
            !ReplaceableBlocks.Contains(existingBlock.Type.Identifier)) {
            SendBlockUpdate(player, placePosition, existingBlock.NetworkId);
            return;
        }

        Server? server = player.Dimension.World?.Server;
        if (server is not null) {
            PlayerPlaceBlockSignal signal = new(player, placePosition, clickedFace, blockType, heldItem);
            server.Emit(signal);
            if (!signal.Emit()) {
                SendBlockUpdate(player, placePosition, existingBlock.NetworkId);
                ItemStack? rollbackItem = inventory.Container.GetItem(transaction.Slot);
                if (rollbackItem is not null) {
                    inventory.Container.SetItem(transaction.Slot, rollbackItem.Clone());
                }
                inventory.Container.UpdateSlot(transaction.Slot);
                inventory.Container.Update();
                inventory.SyncToPlayer(player);
                return;
            }
        }


        Basalt.Core.Blocks.BlockPermutation placedPermutation = blockType.Permutations.Count > 0
            ? blockType.Permutations[0]
            : blockType.GetPermutation();

        player.Dimension.SetPermutation(placePosition.X, placePosition.Y, placePosition.Z, placedPermutation);

        Basalt.Core.Blocks.Block? placedBlock =
            player.Dimension.GetBlock(placePosition.X, placePosition.Y, placePosition.Z);

        placedBlock?.OnPlace(new BlockPlaceDetails(
            player,
            placePosition,
            clickedFace,
            transaction.Position));

        if (placedBlock is not null && placedBlock.Permutation.NetworkId != placedPermutation.NetworkId) {
            placedPermutation = placedBlock.Permutation;
            player.Dimension.SetPermutation(placePosition.X, placePosition.Y, placePosition.Z, placedPermutation);
        }

        SendBlockUpdate(player, placePosition, placedPermutation.NetworkId);

        player.Dimension.Broadcast(new UpdateBlockPacket {
            Position = placePosition,
            BlockRuntimeId = (uint)placedPermutation.NetworkId,
            Flags = (uint)(Basalt.Core.Blocks.UpdateBlockFlagsType.Neighbors | Basalt.Core.Blocks.UpdateBlockFlagsType.Network),
            Layer = (uint)Basalt.Core.Blocks.UpdateBlockLayerType.Normal
        });

        player.Dimension.PlaySound("place", new Vec3 {
            X = placePosition.X + 0.5f,
            Y = placePosition.Y + 0.5f,
            Z = placePosition.Z + 0.5f
        },
            data: placedPermutation.NetworkId);

        heldItem.OnPlace(new ItemPlaceDetails(
            player,
            transaction.Slot,
            clickedPosition,
            clickedFace,
            new Vec3() {
                X = placePosition.X,
                Y = placePosition.Y,
                Z = placePosition.Z,
            },
            transaction.ClickPosition));

        if (player.Gamemode != GameType.Survival) {
            return;
        }

        heldItem.DecrementStack();

        if (heldItem.StackSize == 0) {
            inventory.Container.ClearSlot(inventory.SelectedSlot);
        }
        else {
            inventory.Container.UpdateSlot(inventory.SelectedSlot);
        }
    }

    private static void HandleUseItemOnEntity(
        Player.Player player,
        EntityInventoryTrait inventory,
        ItemUseOnActorInventoryTransactionData transaction) {
        if (player.Dimension is null) {
            return;
        }

        ItemStack? heldItem = GetHeldItem(inventory, transaction.Slot);

        Basalt.Core.Entities.Entity? target = null;

        foreach (Basalt.Core.Entities.Entity entity in player.Dimension.Entities) {
            if (entity.RuntimeId == transaction.RuntimeId) {
                target = entity;
                break;
            }
        }

        if (target is null) {
            return;
        }

        switch (transaction.ActionType) {
            case ItemUseOnActorActionType.Interact:
                target.OnInteract(player, Basalt.Core.Entities.Traits.Enums.EntityInteractMethod.Interact);

                if (heldItem is null) {
                    return;
                }

                if (!CanUseItem(player, heldItem)) {
                    return;
                }

                heldItem.OnUseOnEntity(new ItemUseOnEntityDetails(
                    player,
                    target,
                    transaction.Slot,
                    transaction.FromPosition,
                    transaction.HitPosition));
                break;

            case ItemUseOnActorActionType.Attack:
            case ItemUseOnActorActionType.ItemInteract:
                if (!WithinAttackReach(player, target)) {
                    return;
                }

                if (player.Dimension.World?.Server is Server server) {
                    PlayerAttackEntitySignal signal = new(player, target);
                    server.Emit(signal);
                    if (!signal.Emit()) {
                        return;
                    }

                    target.OnInteract(player, Basalt.Core.Entities.Traits.Enums.EntityInteractMethod.Attack);
                }

                if (heldItem is not null) {
                    heldItem.OnUseAttack(new ItemUseAttackDetails(
                        player,
                        target,
                        transaction.Slot,
                        transaction.FromPosition,
                        transaction.HitPosition));
                }

                if (!ReferenceEquals(target, player)) {
                    EntityHealthTrait? health = target.GetTrait<EntityHealthTrait>();
                    if (health is not null && target.IsAlive) {
                        float damage = heldItem?.Type.AttackDamage ?? 1f;

                        ItemStackEnchantmentTrait? enchantments = heldItem?.GetTrait<ItemStackEnchantmentTrait>();
                        if (enchantments is not null) {
                            damage += enchantments.GetAttackBonus();

                            AttackEntityEnchantmentContext enchantmentContext = new() {
                                Player = player,
                                Target = target
                            };
                            enchantments.OnAttackEntity(enchantmentContext);
                            target.SetOnFire(enchantmentContext.FireTicks);
                        }

                        health.ApplyDamage(damage, player, ActorDamageCause.EntityAttack);
                    }
                }
                break;
        }
    }

    private static bool WithinAttackReach(Player.Player player, Basalt.Core.Entities.Entity target) {
        if (!target.IsAlive || target.Dimension != player.Dimension || ReferenceEquals(player, target)) {
            return false;
        }

        EntityCollisionTrait? targetCollision = target.GetTrait<EntityCollisionTrait>();
        float targetWidth = targetCollision?.Width ?? EntityCollisionTrait.DefaultWidth;
        float targetHeight = targetCollision?.Height ?? EntityCollisionTrait.DefaultHeight;
        Vec3 targetFeet = target is Player.Player targetPlayer ? targetPlayer.GetPosition() : target.Position;
        Vec3 eye = player.GetEyePosition();

        float halfWidth = targetWidth * 0.5f;
        float closestX = Math.Clamp(eye.X, targetFeet.X - halfWidth, targetFeet.X + halfWidth);
        float closestY = Math.Clamp(eye.Y, targetFeet.Y, targetFeet.Y + targetHeight);
        float closestZ = Math.Clamp(eye.Z, targetFeet.Z - halfWidth, targetFeet.Z + halfWidth);
        float deltaX = eye.X - closestX;
        float deltaY = eye.Y - closestY;
        float deltaZ = eye.Z - closestZ;

        return deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ <= EntityAttackReach * EntityAttackReach;
    }

    private static ItemStack? GetHeldItem(EntityInventoryTrait inventory, int hotBarSlot) {
        if (hotBarSlot is < 0 or >= 9) {
            return null;
        }

        inventory.SetHeldItem(hotBarSlot);

        ItemStack? heldItem = inventory.GetHeldItem();
        return heldItem is null || heldItem.StackSize == 0 ? null : heldItem;
    }

    private static bool CanInteractBlock(Player.Player player, BlockPos blockPosition) {
        if (player.Dimension?.World?.Server is not Server server) {
            return true;
        }

        PlayerInteractBlockSignal signal = new(player, blockPosition);
        server.Emit(signal);
        return signal.Emit();
    }

    private static bool CanUseItem(Player.Player player, ItemStack item) {
        if (player.Dimension?.World?.Server is not Server server) {
            return true;
        }

        PlayerUseItemSignal signal = new(player, item);
        server.Emit(signal);
        return signal.Emit();
    }

    private static void SendBlockUpdate(Player.Player player, BlockPos position, int networkId) {
        player.Send(new UpdateBlockPacket {
            Position = position,
            BlockRuntimeId = (uint)networkId,
            Flags = (uint)(Basalt.Core.Blocks.UpdateBlockFlagsType.Neighbors | Basalt.Core.Blocks.UpdateBlockFlagsType.Network),
            Layer = (uint)Basalt.Core.Blocks.UpdateBlockLayerType.Normal
        });
    }

    private static BlockPos GetPlacedBlockPosition(BlockPos position, int face) {
        return face switch {
            0 => new BlockPos { X = position.X, Y = position.Y - 1, Z = position.Z },
            1 => new BlockPos { X = position.X, Y = position.Y + 1, Z = position.Z },
            2 => new BlockPos { X = position.X, Y = position.Y, Z = position.Z - 1 },
            3 => new BlockPos { X = position.X, Y = position.Y, Z = position.Z + 1 },
            4 => new BlockPos { X = position.X - 1, Y = position.Y, Z = position.Z },
            5 => new BlockPos { X = position.X + 1, Y = position.Y, Z = position.Z },
            _ => position
        };
    }

    private static bool IsEmptyPosition(BlockPos position) {
        return position.X == 0 && position.Y == 0 && position.Z == 0;
    }

    private static bool FindBlockFromView(
        Player.Player player,
        float pitchDegrees,
        float yawDegrees,
        Vec3 cameraOrientation,
        out BlockPos blockPosition,
        out int face) {
        blockPosition = new BlockPos();
        face = 1;

        if (player.Dimension is null) {
            return false;
        }

        float directionX;
        float directionY;
        float directionZ;

        float cameraLengthSquared = cameraOrientation.X * cameraOrientation.X
            + cameraOrientation.Y * cameraOrientation.Y
            + cameraOrientation.Z * cameraOrientation.Z;

        if (cameraLengthSquared > 0.0001f) {
            float inverseLength = 1f / MathF.Sqrt(cameraLengthSquared);
            directionX = cameraOrientation.X * inverseLength;
            directionY = cameraOrientation.Y * inverseLength;
            directionZ = cameraOrientation.Z * inverseLength;
        }
        else {
            float yaw = MathF.PI / 180f * yawDegrees;
            float pitch = MathF.PI / 180f * pitchDegrees;

            directionX = -MathF.Sin(yaw) * MathF.Cos(pitch);
            directionY = -MathF.Sin(pitch);
            directionZ = MathF.Cos(yaw) * MathF.Cos(pitch);
        }

        float startX = player.Location.X;
        float startY = player.GetEyePosition().Y;
        float startZ = player.Location.Z;

        int previousX = (int)MathF.Floor(startX);
        int previousY = (int)MathF.Floor(startY);
        int previousZ = (int)MathF.Floor(startZ);

        const float maxDistance = 6f;
        const float step = 0.1f;

        for (float distance = step; distance <= maxDistance; distance += step) {
            float rayX = startX + directionX * distance;
            float rayY = startY + directionY * distance;
            float rayZ = startZ + directionZ * distance;

            int blockX = (int)MathF.Floor(rayX);
            int blockY = (int)MathF.Floor(rayY);
            int blockZ = (int)MathF.Floor(rayZ);

            Basalt.Core.Blocks.BlockPermutation block =
                player.Dimension.GetPermutation(blockX, blockY, blockZ);

            if (block.Type.Identifier != "minecraft:air") {
                blockPosition = new BlockPos {
                    X = blockX,
                    Y = blockY,
                    Z = blockZ
                };

                int deltaX = previousX - blockX;
                int deltaY = previousY - blockY;
                int deltaZ = previousZ - blockZ;

                face = (deltaX, deltaY, deltaZ) switch {
                    (1, 0, 0) => 5,
                    (-1, 0, 0) => 4,
                    (0, 1, 0) => 1,
                    (0, -1, 0) => 0,
                    (0, 0, 1) => 3,
                    (0, 0, -1) => 2,
                    _ => 1
                };

                return true;
            }

            previousX = blockX;
            previousY = blockY;
            previousZ = blockZ;
        }

        return false;
    }
}










