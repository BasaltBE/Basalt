namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Events;
using Basalt.Core.Item;
using Basalt.Core.Item.Traits;
using Basalt.Core.Item.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;


public static class InventoryTransaction
{
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

    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        InventoryTransactionPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (InventoryTransactionPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!server.Players.TryGetValue(connection, out Player.Player? player))
        {
            return;
        }

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null)
        {
            return;
        }


        switch (packet.TransactionData)
        {
            case NormalInventoryTransactionData:
                HandleInventoryActions(player, inventory, packet.Actions, packet.LegacySetItemSlots);
                break;

            case UseItemInventoryTransactionData useItem:
                HandleUseItem(player, inventory, useItem, packet.Actions);
                break;

            case UseItemOnEntityInventoryTransactionData useItemOnEntity:
                HandleUseItemOnEntity(player, inventory, useItemOnEntity);
                break;

            case ReleaseItemInventoryTransactionData:
            case MismatchInventoryTransactionData:
                break;
        }

    }

    public static void HandleUseItemFromAuthInput(Player.Player player, UseItemTransactionData data, float pitch, float yaw)
    {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null)
        {
            return;
        }

        player.Pitch = pitch;
        player.Yaw = yaw;
        player.HeadYaw = yaw;

        UseItemInventoryTransactionData transaction = new()
        {
            ActionType = data.ActionType,
            TriggerType = data.TriggerType,
            BlockPosition = data.BlockPosition,
            BlockFace = data.BlockFace,
            HotBarSlot = data.HotBarSlot,
            HeldItem = data.HeldItem,
            Position = data.Position,
            ClickedPosition = data.ClickedPosition,
            BlockRuntimeId = data.BlockRuntimeId,
            ClientPrediction = data.ClientPrediction,
            ClientCooldownState = data.ClientCooldownState
        };

        bool missingBlockPosition =
            transaction.BlockPosition.X == 0 &&
            transaction.BlockPosition.Y == 0 &&
            transaction.BlockPosition.Z == 0 &&
            transaction.BlockRuntimeId == 0;

        if (missingBlockPosition && FindBlockFromView(player, pitch, yaw, out BlockPos viewedBlock, out int viewedFace))
        {
            transaction.BlockPosition = viewedBlock;
            transaction.BlockFace = viewedFace;
        }
        else if (missingBlockPosition)
        {
            transaction.BlockPosition = new BlockPos
            {
                X = (int)MathF.Floor(player.Location.X),
                Y = (int)MathF.Floor(player.Location.Y - 1f),
                Z = (int)MathF.Floor(player.Location.Z)
            };

            if (transaction.BlockFace is < 0 or > 5)
            {
                transaction.BlockFace = 1;
            }
        }

        HandleUseItem(player, inventory, transaction, []);
    }

    private static void HandleInventoryActions(
        Player.Player player,
        EntityInventoryTrait inventory,
        List<InventoryAction> actions,
        List<LegacySetItemSlot> legacySetItemSlots)
    {
        foreach (InventoryAction action in actions)
        {
            if (action.SourceType == (uint)InventoryActionSourceType.World)
            {
                // int worldSlot = ResolveWorldActionSlot(inventory, action, actions, legacySetItemSlots);
                SpawnWorldDrop(player, action);

                // Logger.Info("World Interaction WindowId: " + action.WindowId + " Slot: " + worldSlot + " RawSlot: " + action.InventorySlot);
                // Logger.Info("NetworkId Old/New: " + action.OldItem.Stack.NetworkId + "/" + action.NewItem.Stack.NetworkId);
                // Logger.Info("StackSize Old/New: " + action.OldItem.Stack.StackSize + "/" + action.NewItem.Stack.StackSize);

                inventory.Container.Update();
                continue;
            }

            if (action.SourceType != (uint)InventoryActionSourceType.Container)
            {
                continue;
            }

            Containers.Container? container = null;
            if ((ContainerId)action.WindowId == (inventory.Container.Identifier ?? ContainerId.Inventory))
            {
                container = inventory.Container;
            }
            else if (player.TryGetOpenContainer((ContainerId)action.WindowId, out Containers.Container? opened))
            {
                container = opened;
            }

            if (container is null)
            {
                continue;
            }

            int slot = (int)action.InventorySlot;
            if (slot < 0 || slot >= container.GetSize())
            {
                continue;
            }

            NetworkItemStackDescriptor stack = action.NewItem;
            if (stack.NetworkId == 0 || stack.Count == 0)
            {
                container.ClearSlot(slot);
                continue;
            }

            try
            {
                ItemStack item = ItemStack.FromNetworkStack(stack);
                container.SetItem(slot, item);
            }
            catch
            {
            }
        }
    }

    private static int ResolveWorldActionSlot(
        EntityInventoryTrait inventory,
        InventoryAction action,
        List<InventoryAction> actions,
        List<LegacySetItemSlot> legacySetItemSlots)
    {
        for (int i = 0; i < legacySetItemSlots.Count; i++)
        {
            LegacySetItemSlot legacy = legacySetItemSlots[i];
            if (legacy.Slots.Length == 0)
            {
                continue;
            }

            if (legacy.ContainerId is (byte)ContainerName.Inventory or (byte)ContainerName.Hotbar)
            {
                return legacy.Slots[0];
            }
        }

        for (int i = 0; i < actions.Count; i++)
        {
            InventoryAction candidate = actions[i];
            if (candidate.SourceType != (uint)InventoryActionSourceType.Container || candidate.WindowId != 0)
            {
                continue;
            }

            NetworkItemStackDescriptor candidateOld = candidate.OldItem;
            NetworkItemStackDescriptor candidateNew = candidate.NewItem;
            NetworkItemStackDescriptor dropped = action.NewItem;

            if (candidateOld.NetworkId == 0 || dropped.NetworkId == 0)
            {
                continue;
            }

            if (candidateOld.NetworkId != dropped.NetworkId || candidateOld.Count <= candidateNew.Count)
            {
                continue;
            }

            int delta = candidateOld.Count - candidateNew.Count;
            if (delta == dropped.Count)
            {
                return (int)candidate.InventorySlot;
            }
        }

        if (action.InventorySlot == 0 && inventory.SelectedSlot is >= 0 and < 9)
        {
            return inventory.SelectedSlot;
        }

        return (int)action.InventorySlot;
    }

    private static void SpawnWorldDrop(Player.Player player, InventoryAction action)
    {
        if (player.Dimension is null)
        {
            return;
        }

        NetworkItemStackDescriptor dropped = action.NewItem;
        if (dropped.NetworkId == 0 || dropped.Count == 0)
        {
            return;
        }

        ItemStack item;
        try
        {
            item = ItemStack.FromNetworkStack(dropped);
        }
        catch
        {
            return;
        }
        player.DropItem(item);
    }

    private static void HandleUseItem(
        Player.Player player,
        EntityInventoryTrait inventory,
        UseItemInventoryTransactionData transaction,
        List<InventoryAction> actions)
    {
        
        if (transaction.ActionType == UseItemActionClickAir)
        {
            ItemStack? airHeldItem = GetHeldItem(inventory, transaction.HotBarSlot);
            if (airHeldItem is null)
            {
                return;
            }

            if (player.Dimension is not null)
            {
                BlockPos blockPosition = transaction.BlockPosition;
                int blockFace = transaction.BlockFace;

                if (IsEmptyPosition(blockPosition) && transaction.BlockRuntimeId == 0 && player.LastActionBlockPosition.HasValue)
                {
                    blockPosition = player.LastActionBlockPosition.Value;

                    if (player.LastActionFace is >= 0 and <= 5)
                    {
                        blockFace = player.LastActionFace;
                    }
                }

                Basalt.Core.Blocks.BlockPermutation clickedBlock =
                    player.Dimension.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

                if (clickedBlock.Type.Identifier is not "minecraft:air" and not "minecraft:cave_air" and not "minecraft:void_air")
                {
                    airHeldItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                        player,
                        transaction.HotBarSlot,
                        blockPosition,
                        blockFace,
                        transaction.Position,
                        transaction.ClickedPosition));
                    return;
                }
            }

            airHeldItem.OnUseOnAir(new ItemUseOnAirDetails(player, transaction.HotBarSlot, transaction.Position));
            return;
        }

        if (transaction.ActionType != UseItemActionClickBlock)
        {
            return;
        }

        if (transaction.TriggerType == UseItemTriggerRepeat)
        {
            ItemStack? repeatItem = GetHeldItem(inventory, transaction.HotBarSlot);
            if (repeatItem is null)
            {
                return;
            }

            bool isPlaceableBlock = repeatItem.Type.BlockType is not null
                || Basalt.Core.Blocks.BlockType.Get(repeatItem.Identifier) is not null;

            // Holding place continuously sends Repeat; server must still commit block placement.
            if (isPlaceableBlock)
            {
                UseItemOnBlock(player, inventory, repeatItem, transaction);
                return;
            }

            if (player.Dimension is not null)
            {
                BlockPos blockPosition = transaction.BlockPosition;

                if (IsEmptyPosition(blockPosition) && transaction.BlockRuntimeId == 0 && player.LastActionBlockPosition.HasValue)
                {
                    blockPosition = player.LastActionBlockPosition.Value;
                }

                repeatItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                    player,
                    transaction.HotBarSlot,
                    blockPosition,
                    transaction.BlockFace,
                    transaction.Position,
                    transaction.ClickedPosition));
            }
            return;
        }

        if (transaction.TriggerType == UseItemTriggerInitial &&
            transaction.ClientPrediction != UseItemClientPredictionPlace)
        {
            ItemStack? nonPlaceItem = GetHeldItem(inventory, transaction.HotBarSlot);
            if (nonPlaceItem is not null && nonPlaceItem.Type.BlockType is null && Basalt.Core.Blocks.BlockType.Get(nonPlaceItem.Identifier) is null)
            {
                if (player.Dimension is not null)
                {
                    BlockPos blockPosition = transaction.BlockPosition;

                    if (IsEmptyPosition(blockPosition) && transaction.BlockRuntimeId == 0 && player.LastActionBlockPosition.HasValue)
                    {
                        blockPosition = player.LastActionBlockPosition.Value;
                    }

                    nonPlaceItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                        player,
                        transaction.HotBarSlot,
                        blockPosition,
                        transaction.BlockFace,
                        transaction.Position,
                        transaction.ClickedPosition));
                }
            }
            return;
        }

        if (player.Dimension is not null)
        {
            BlockPos blockPosition = transaction.BlockPosition;

            if (IsEmptyPosition(blockPosition) && transaction.BlockRuntimeId == 0 && player.LastActionBlockPosition.HasValue)
            {
                blockPosition = player.LastActionBlockPosition.Value;
            }

            Basalt.Core.Blocks.Block? block = player.Dimension.GetBlock(blockPosition.X, blockPosition.Y, blockPosition.Z);
            if (block is not null && !player.IsSneaking)
            {
                block.OnInteract(new BlockInteractDetails(
                    player,
                    blockPosition,
                    transaction.BlockFace,
                    transaction.ClickedPosition));

                return;
            }
        }

        ItemStack? heldItem = GetHeldItem(inventory, transaction.HotBarSlot);
        if (heldItem is null)
        {
            return;
        }

        if (player.Gamemode == Gamemode.Survival &&
            transaction.TriggerType == UseItemTriggerInitial &&
            actions.Count == 0)
        {
            if (heldItem.Type.BlockType is null && Basalt.Core.Blocks.BlockType.Get(heldItem.Identifier) is null)
            {
                heldItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                    player,
                    transaction.HotBarSlot,
                    transaction.BlockPosition,
                    transaction.BlockFace,
                    transaction.Position,
                    transaction.ClickedPosition));
            }
            return;
        }

        UseItemOnBlock(player, inventory, heldItem, transaction);
    }

    private static void UseItemOnBlock(
        Player.Player player,
        EntityInventoryTrait inventory,
        ItemStack heldItem,
        UseItemInventoryTransactionData transaction)
    {
        if (player.Dimension is null)
        {
            return;
        }

        BlockPos clickedPosition = transaction.BlockPosition;
        int clickedFace = transaction.BlockFace;

        if (IsEmptyPosition(clickedPosition) && transaction.BlockRuntimeId == 0 && player.LastActionBlockPosition.HasValue)
        {
            clickedPosition = player.LastActionBlockPosition.Value;

            if (player.LastActionFace is >= 0 and <= 5)
            {
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

        if (blockEntity is not null && !player.IsSneaking)
        {
            blockEntity.OnInteract(new BlockInteractDetails(
                player,
                clickedPosition,
                clickedFace,
                transaction.ClickedPosition));

            SendBlockUpdate(player, clickedPosition, clickedBlock.NetworkId);
            return;
        }

        Basalt.Core.Blocks.BlockType? blockType = heldItem.Type.BlockType ?? Basalt.Core.Blocks.BlockType.Get(heldItem.Identifier);

        if (blockType is null || blockType.Identifier == "minecraft:air")
        {
            heldItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                player,
                transaction.HotBarSlot,
                clickedPosition,
                clickedFace,
                transaction.Position,
                transaction.ClickedPosition));

            Basalt.Core.Blocks.BlockPermutation currentBlock =
                player.Dimension.GetPermutation(placePosition.X, placePosition.Y, placePosition.Z);
            SendBlockUpdate(player, placePosition, currentBlock.NetworkId);
            return;
        }

        if (existingBlock.Type.Identifier == blockType.Identifier ||
            !ReplaceableBlocks.Contains(existingBlock.Type.Identifier))
        {
            SendBlockUpdate(player, placePosition, existingBlock.NetworkId);
            return;
        }

        Server? server = player.Dimension.World?.Server;
        if (server is not null)
        {
            PlayerPlaceBlockSignal signal = new(player, placePosition, clickedFace, blockType, heldItem);
            server.Emit(signal);
            if (!signal.Emit())
            {
                SendBlockUpdate(player, placePosition, existingBlock.NetworkId);
                ItemStack? rollbackItem = inventory.Container.GetItem(transaction.HotBarSlot);
                if (rollbackItem is not null)
                {
                    inventory.Container.SetItem(transaction.HotBarSlot, rollbackItem.Clone());
                }
                inventory.Container.UpdateSlot(transaction.HotBarSlot);
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
            transaction.ClickedPosition));

        if (placedBlock is not null && placedBlock.Permutation.NetworkId != placedPermutation.NetworkId)
        {
            placedPermutation = placedBlock.Permutation;
            player.Dimension.SetPermutation(placePosition.X, placePosition.Y, placePosition.Z, placedPermutation);
        }

        SendBlockUpdate(player, placePosition, placedPermutation.NetworkId);

        player.Dimension.Broadcast(new UpdateBlockPacket
        {
            Position = placePosition,
            NetworkBlockId = (uint)placedPermutation.NetworkId,
            Flags = UpdateBlockFlagsType.Network,
            Layer = UpdateBlockLayerType.Normal
        });

        player.Dimension.Broadcast(new LevelSoundEventPacket
        {
            Event = LevelSoundEvent.Place,
            Position = new Vec3f
            {
                X = placePosition.X + 0.5f,
                Y = placePosition.Y + 0.5f,
                Z = placePosition.Z + 0.5f
            },
            Data = placedPermutation.NetworkId,
            ActorIdentifier = string.Empty,
            BabyMob = false,
            DisableRelativeVolume = false,
            UniqueActorId = 0,
            FireAtPosition = new Optional<Vec3f> { HasValue = false, Value = default }
        });

        heldItem.OnPlace(new ItemPlaceDetails(
            player,
            transaction.HotBarSlot,
            clickedPosition,
            clickedFace,
            transaction.Position,
            transaction.ClickedPosition));

        if (player.Gamemode != Gamemode.Survival)
        {
            return;
        }

        heldItem.DecrementStack();

        if (heldItem.StackSize == 0)
        {
            inventory.Container.ClearSlot(inventory.SelectedSlot);
        }
        else
        {
            inventory.Container.UpdateSlot(inventory.SelectedSlot);
        }
    }

    private static void HandleUseItemOnEntity(
        Player.Player player,
        EntityInventoryTrait inventory,
        UseItemOnEntityInventoryTransactionData transaction)
    {
        if (player.Dimension is null)
        {
            return;
        }

        ItemStack? heldItem = GetHeldItem(inventory, transaction.HotBarSlot);

        Basalt.Core.Entities.Entity? target = null;

        foreach (Basalt.Core.Entities.Entity entity in player.Dimension.Entities)
        {
            if (entity.RuntimeId == transaction.TargetEntityRuntimeId)
            {
                target = entity;
                break;
            }
        }

        if (target is null)
        {
            return;
        }

        switch (transaction.ActionType)
        {
            case 0:
                if (heldItem is null)
                {
                    return;
                }

                heldItem.OnUseOnEntity(new ItemUseOnEntityDetails(
                    player,
                    target,
                    transaction.HotBarSlot,
                    transaction.Position,
                    transaction.ClickedPosition));
                break;

            case 1:
                if (heldItem is not null)
                {
                    heldItem.OnUseAttack(new ItemUseAttackDetails(
                        player,
                        target,
                        transaction.HotBarSlot,
                        transaction.Position,
                        transaction.ClickedPosition));
                }

                if (!ReferenceEquals(target, player))
                {
                    EntityHealthTrait? health = target.GetTrait<EntityHealthTrait>();
                    if (health is not null && target.IsAlive)
                    {
                        float damage = heldItem?.Type.AttackDamage ?? 1f;

                        ItemStackEnchantmentTrait? enchantments = heldItem?.GetTrait<ItemStackEnchantmentTrait>();
                        if (enchantments is not null)
                        {
                            damage += enchantments.GetAttackBonus();
                        }

                        health.ApplyDamage(damage, player, ActorDamageCause.EntityAttack);
                    }
                }
                break;
        }
    }

    private static ItemStack? GetHeldItem(EntityInventoryTrait inventory, int hotBarSlot)
    {
        if (hotBarSlot is < 0 or >= 9)
        {
            hotBarSlot = 0;
        }

        inventory.SetHeldItem(hotBarSlot);

        ItemStack? heldItem = inventory.GetHeldItem();
        return heldItem is null || heldItem.StackSize == 0 ? null : heldItem;
    }

    private static void SendBlockUpdate(Player.Player player, BlockPos position, int networkId)
    {
        player.Send(new UpdateBlockPacket
        {
            Position = position,
            NetworkBlockId = (uint)networkId,
            Flags = UpdateBlockFlagsType.Network,
            Layer = UpdateBlockLayerType.Normal
        });
    }

    private static BlockPos GetPlacedBlockPosition(BlockPos position, int face)
    {
        return face switch
        {
            0 => new BlockPos { X = position.X, Y = position.Y - 1, Z = position.Z },
            1 => new BlockPos { X = position.X, Y = position.Y + 1, Z = position.Z },
            2 => new BlockPos { X = position.X, Y = position.Y, Z = position.Z - 1 },
            3 => new BlockPos { X = position.X, Y = position.Y, Z = position.Z + 1 },
            4 => new BlockPos { X = position.X - 1, Y = position.Y, Z = position.Z },
            5 => new BlockPos { X = position.X + 1, Y = position.Y, Z = position.Z },
            _ => position
        };
    }

    private static bool IsEmptyPosition(BlockPos position)
    {
        return position.X == 0 && position.Y == 0 && position.Z == 0;
    }

    private static bool FindBlockFromView(Player.Player player, float pitchDegrees, float yawDegrees, out BlockPos blockPosition, out int face)
    {
        blockPosition = default;
        face = 1;

        if (player.Dimension is null)
        {
            return false;
        }

        float yaw = MathF.PI / 180f * yawDegrees;
        float pitch = MathF.PI / 180f * pitchDegrees;

        float directionX = -MathF.Sin(yaw) * MathF.Cos(pitch);
        float directionY = -MathF.Sin(pitch);
        float directionZ = MathF.Cos(yaw) * MathF.Cos(pitch);

        float startX = player.Location.X;
        float startY = player.Location.Y + 1.62f;
        float startZ = player.Location.Z;

        int previousX = (int)MathF.Floor(startX);
        int previousY = (int)MathF.Floor(startY);
        int previousZ = (int)MathF.Floor(startZ);

        const float maxDistance = 6f;
        const float step = 0.1f;

        for (float distance = step; distance <= maxDistance; distance += step)
        {
            float rayX = startX + directionX * distance;
            float rayY = startY + directionY * distance;
            float rayZ = startZ + directionZ * distance;

            int blockX = (int)MathF.Floor(rayX);
            int blockY = (int)MathF.Floor(rayY);
            int blockZ = (int)MathF.Floor(rayZ);

            Basalt.Core.Blocks.BlockPermutation block =
                player.Dimension.GetPermutation(blockX, blockY, blockZ);

            if (block.Type.Identifier != "minecraft:air")
            {
                blockPosition = new BlockPos
                {
                    X = blockX,
                    Y = blockY,
                    Z = blockZ
                };

                int deltaX = previousX - blockX;
                int deltaY = previousY - blockY;
                int deltaZ = previousZ - blockZ;

                face = (deltaX, deltaY, deltaZ) switch
                {
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










