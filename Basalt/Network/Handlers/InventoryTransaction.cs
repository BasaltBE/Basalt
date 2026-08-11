namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Events;
using Basalt.Core.Item;
using Basalt.Core.Item.Traits;
using Basalt.Core.Item.Traits.Types;
using Basalt.RakNet;

using BedrockProtocol.Types;
using BedrockProtocol.Packets;
using BedrockProtocol.Enums;
using Basalt.Core.Blocks;

public static class InventoryTransaction {
    private const uint UseItemActionClickBlock = 0;
    private const uint UseItemActionClickAir = 1;
    private const uint UseItemTriggerInitial = 1;
    private const uint UseItemTriggerRepeat = 2;
    private const uint UseItemClientPredictionPlace = 1;

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


        switch (packet.Transaction) {
            case NormalTransactionData normal:
                HandleInventoryActions(player, inventory, normal.Actions.Actions, packet.LegacySetItemSlots ?? []);
                break;

            case ItemUseInventoryTransaction useItem:
                HandleUseItem(player, inventory, useItem, useItem.Actions.Actions);
                break;

            case ItemUseOnActorInventoryTransaction useItemOnEntity:
                HandleUseItemOnEntity(player, inventory, useItemOnEntity);
                break;

            case ItemReleaseInventoryTransaction:
                PlayerAuthInput.CancelPendingItemUse(player);
                break;

            case InventoryMismatchData:
                break;
        }

    }

    public static void HandleUseItemFromAuthInput(
        Player.Player player,
        PackedItemUseLegacyInventoryTransaction data,
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

        ItemUseInventoryTransaction transaction = data.ItemUseTransaction;


        bool missingBlockPosition =
            transaction.Position.X == 0 &&
            transaction.Position.Y == 0 &&
            transaction.Position.Z == 0 &&
            transaction.TargetBlockId == 0;

        if (missingBlockPosition && FindBlockFromView(player, pitch, yaw, cameraOrientation, out BlockPos viewedBlock, out int viewedFace)) {
            transaction.Position = viewedBlock;
            transaction.Face = (byte)viewedFace;
        }
        else if (missingBlockPosition) {
            transaction.Position = new BlockPos {
                X = (int)MathF.Floor(player.Location.X),
                Y = (int)MathF.Floor(player.Location.Y - 1f),
                Z = (int)MathF.Floor(player.Location.Z)
            };

            if (transaction.Face is < 0 or > 5) {
                transaction.Face = 1;
            }
        }

        HandleUseItem(player, inventory, transaction, []);
    }

    private static void HandleInventoryActions(
        Player.Player player,
        EntityInventoryTrait inventory,
        List<InventoryAction> actions,
        List<LegacySetSlot> legacySetItemSlots) {
        foreach (InventoryAction action in actions) {
            if (action.Source.SourceType == InventorySourceType.WorldInteraction) {
                // int worldSlot = ResolveWorldActionSlot(inventory, action, actions, legacySetItemSlots);
                if (!SpawnWorldDrop(player, action)) {
                    inventory.Container.Update();
                    return;
                }

                // Logger.Info("World Interaction WindowId: " + action.WindowId + " Slot: " + worldSlot + " RawSlot: " + action.InventorySlot);
                // Logger.Info("Id Old/New: " + action.FromItem.Stack.Id + "/" + action.ToItem.Stack.Id);
                // Logger.Info("StackSize Old/New: " + action.FromItem.Stack.StackSize + "/" + action.ToItem.Stack.StackSize);

                inventory.Container.Update();
                continue;
            }

            if (action.Source.SourceType != (uint)InventorySourceType.ContainerInventory) {
                continue;
            }
            if (action.Source.ContainerID is null) continue;

            Containers.Container? container = null;
            if ((ContainerID)action.Source.ContainerID == (inventory.Container.Identifier ?? ContainerID.CONTAINER_ID_INVENTORY)) {
                container = inventory.Container;
            }
            else if (player.TryGetOpenContainer((ContainerID)action.Source.ContainerID, out Containers.Container? opened)) {
                container = opened;
            }

            if (container is null) {
                continue;
            }

            int slot = (int)action.Slot;
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
        InventoryAction action,
        List<InventoryAction> actions,
        List<LegacySetSlot> legacySetItemSlots) {
        for (int i = 0; i < legacySetItemSlots.Count; i++) {
            LegacySetSlot legacy = legacySetItemSlots[i];
            if (legacy.Slots.Count == 0) {
                continue;
            }

            if (legacy.ContainerEnum is ContainerEnumName.InventoryContainer or ContainerEnumName.HotbarContainer) {
                return legacy.Slots[0];
            }
        }

        for (int i = 0; i < actions.Count; i++) {
            InventoryAction candidate = actions[i];
            if (candidate.Source.SourceType != (uint)InventorySourceType.ContainerInventory || candidate.Source.ContainerID != 0) {
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

    private static bool SpawnWorldDrop(Player.Player player, InventoryAction action) {
        if (player.Dimension is null) {
            return false;
        }

        NetworkItemStackDescriptor dropped = action.ToItem;
        if (dropped.Id == 0 || dropped.StackSize == 0) {
            return false;
        }

        ItemStack item;
        try {
            item = ItemStack.FromNetworkStack(dropped);
        }
        catch {
            return false;
        }
        return player.DropItem(item);
    }

    private static void HandleUseItem(
        Player.Player player,
        EntityInventoryTrait inventory,
        ItemUseInventoryTransaction transaction,
        List<InventoryAction> actions) {
        // Logger.Info($"HandleUseItem ActionType:{transaction.ActionType} Trigger:{transaction.TriggerType} Prediction:{transaction.ClientPrediction} Pos:({transaction.BlockPosition.X},{transaction.BlockPosition.Y},{transaction.BlockPosition.Z}) Face:{transaction.BlockFace} actions:{actions.Count}");

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
                SendBlockUpdate(player, transaction.Position, serverPermutation.NetworkId);
                return;
            }
        }

        if (transaction.TriggerType == ItemUseTriggerType.SimulationTick) {
            ItemStack? repeatItem = GetHeldItem(inventory, transaction.Slot);
            if (repeatItem is null) {
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
            ItemStack? nonPlaceItem = GetHeldItem(inventory, transaction.Slot);
            if (nonPlaceItem is not null && nonPlaceItem.Type.BlockType is null && Basalt.Core.Blocks.BlockType.Get(nonPlaceItem.Identifier) is null) {
                if (player.Dimension is not null) {
                    BlockPos blockPosition = transaction.Position;

                    if (IsEmptyPosition(blockPosition) && transaction.TargetBlockId == 0 && player.LastActionBlockPosition is not null) {
                        blockPosition = player.LastActionBlockPosition;
                    }

                    if (!CanUseItem(player, nonPlaceItem)) {
                        return;
                    }

                    nonPlaceItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                        player,
                        transaction.Slot,
                        blockPosition,
                        transaction.Face,
                        transaction.Position,
                        transaction.ClickPosition));
                }
                return;
            }
        }

        if (player.Dimension is not null) {
            BlockPos blockPosition = transaction.Position;

            if (IsEmptyPosition(blockPosition) && transaction.TargetBlockId == 0 && player.LastActionBlockPosition is not null) {
                blockPosition = player.LastActionBlockPosition;
            }

            Basalt.Core.Blocks.Block? block = player.Dimension.GetBlock(blockPosition.X, blockPosition.Y, blockPosition.Z);
            if (block is not null && block.Interactable && !player.IsSneaking) {
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

        ItemStack? heldItem = GetHeldItem(inventory, transaction.Slot);
        if (heldItem is null) {
            return;
        }

        if (player.Gamemode == GameType.Survival &&
            transaction.TriggerType == ItemUseTriggerType.PlayerInput &&
            actions.Count == 0) {
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
        ItemUseInventoryTransaction transaction) {
        if (player.Dimension is null) {
            return;
        }

        BlockPos clickedPosition = transaction.Position;
        int clickedFace = transaction.Face;

        // Logger.Info($"UseItemOnBlock pos:({clickedPosition.X},{clickedPosition.Y},{clickedPosition.Z}) face:{clickedFace} item:{heldItem.Identifier}");

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
            BlockPosition = placePosition,
            BlockRuntimeID = (uint)placedPermutation.NetworkId,
            Flags = (uint)(UpdateBlockFlagsType.Neighbors | UpdateBlockFlagsType.Network),
            Layer = (uint)UpdateBlockLayerType.Normal
        });

        player.Dimension.PlaySound(LevelSoundEvent.place.ToProtocolString(), new Vec3 {
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
        ItemUseOnActorInventoryTransaction transaction) {
        if (player.Dimension is null) {
            return;
        }

        ItemStack? heldItem = GetHeldItem(inventory, transaction.Slot);

        Basalt.Core.Entities.Entity? target = null;

        foreach (Basalt.Core.Entities.Entity entity in player.Dimension.Entities) {
            if (entity.RuntimeId == transaction.RuntimeId.Value) {
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
                if (player.Dimension.World?.Server is Server server) {
                    PlayerAttackEntitySignal signal = new(player, target);
                    server.Emit(signal);
                    if (!signal.Emit()) {
                        return;
                    }
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
                        }

                        health.ApplyDamage(damage, player, ActorDamageCause.EntityAttack);
                    }
                }
                break;
        }
    }

    private static ItemStack? GetHeldItem(EntityInventoryTrait inventory, int hotBarSlot) {
        if (hotBarSlot is < 0 or >= 9) {
            hotBarSlot = 0;
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
            BlockPosition = position,
            BlockRuntimeID = (uint)networkId,
            Flags = (uint)(UpdateBlockFlagsType.Neighbors | UpdateBlockFlagsType.Network),
            Layer = (uint)UpdateBlockLayerType.Normal
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
        float startY = player.Location.Y + 1.62f;
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










