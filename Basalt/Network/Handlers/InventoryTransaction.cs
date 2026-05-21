using Basalt.Core;
using Basalt.Block.Traits.Types;
using Basalt.Entity.Traits;
using Basalt.Item;
using Basalt.Item.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

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

        if (!server.Players.TryGetValue(connection, out Player? player))
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
                HandleInventoryActions(inventory, packet.Actions);
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

    public static void HandleUseItemFromAuthInput(Player player, UseItemTransactionData data, float pitch, float yaw)
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
                X = (int)MathF.Floor(player.Position.X),
                Y = (int)MathF.Floor(player.Position.Y - 1f),
                Z = (int)MathF.Floor(player.Position.Z)
            };

            if (transaction.BlockFace is < 0 or > 5)
            {
                transaction.BlockFace = 1;
            }
        }

        HandleUseItem(player, inventory, transaction, []);
    }

    private static void HandleInventoryActions(EntityInventoryTrait inventory, List<InventoryAction> actions)
    {
        foreach (InventoryAction action in actions)
        {
            if (action.SourceType != (uint)InventoryActionSourceType.World)
            {
                continue;
            }

            inventory.Container.Update();
            return;
        }
    }

    private static void HandleUseItem(
        Player player,
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

                Basalt.Block.BlockPermutation clickedBlock =
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
            return;
        }

        if (transaction.TriggerType == UseItemTriggerInitial &&
            transaction.ClientPrediction != UseItemClientPredictionPlace)
        {
            return;
        }

        if (player.Dimension is not null)
        {
            BlockPos blockPosition = transaction.BlockPosition;

            if (IsEmptyPosition(blockPosition) && transaction.BlockRuntimeId == 0 && player.LastActionBlockPosition.HasValue)
            {
                blockPosition = player.LastActionBlockPosition.Value;
            }

            Basalt.Block.Block? block = player.Dimension.GetBlock(blockPosition.X, blockPosition.Y, blockPosition.Z);
            if (block is not null)
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
            return;
        }

        UseItemOnBlock(player, inventory, heldItem, transaction);
    }

    private static void UseItemOnBlock(
        Player player,
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

        Basalt.Block.BlockPermutation clickedBlock =
            player.Dimension.GetPermutation(clickedPosition.X, clickedPosition.Y, clickedPosition.Z);

        BlockPos placePosition = GetPlacedBlockPosition(clickedPosition, clickedFace);

        Basalt.Block.BlockPermutation existingBlock =
            player.Dimension.GetPermutation(placePosition.X, placePosition.Y, placePosition.Z);

        Basalt.Block.Block? blockEntity =
            player.Dimension.GetBlock(clickedPosition.X, clickedPosition.Y, clickedPosition.Z);

        if (blockEntity is not null)
        {
            blockEntity.OnInteract(new BlockInteractDetails(
                player,
                clickedPosition,
                clickedFace,
                transaction.ClickedPosition));

            SendBlockUpdate(player, clickedPosition, clickedBlock.NetworkId);
            return;
        }

        Basalt.Block.BlockType? blockType = heldItem.Type.BlockType ?? Basalt.Block.BlockType.Get(heldItem.Identifier);

        if (blockType is null || blockType.Identifier == "minecraft:air")
        {
            heldItem.OnUseOnBlock(new ItemUseOnBlockDetails(
                player,
                transaction.HotBarSlot,
                clickedPosition,
                clickedFace,
                transaction.Position,
                transaction.ClickedPosition));

            SendBlockUpdate(player, placePosition, existingBlock.NetworkId);
            return;
        }

        if (existingBlock.Type.Identifier == blockType.Identifier ||
            !ReplaceableBlocks.Contains(existingBlock.Type.Identifier))
        {
            SendBlockUpdate(player, placePosition, existingBlock.NetworkId);
            return;
        }

        Basalt.Block.BlockPermutation placedPermutation = blockType.Permutations.Count > 0
            ? blockType.Permutations[0]
            : blockType.GetPermutation();

        player.Dimension.SetPermutation(placePosition.X, placePosition.Y, placePosition.Z, placedPermutation);

        Basalt.Block.Block? placedBlock =
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
            IsBabyMob = false,
            IsGlobal = false,
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
        Player player,
        EntityInventoryTrait inventory,
        UseItemOnEntityInventoryTransactionData transaction)
    {
        ItemStack? heldItem = GetHeldItem(inventory, transaction.HotBarSlot);
        if (heldItem is null || player.Dimension is null)
        {
            return;
        }

        Basalt.Entity.Entity? target = null;

        foreach (Basalt.Entity.Entity entity in player.Dimension.Entities)
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
                heldItem.OnUseOnEntity(new ItemUseOnEntityDetails(
                    player,
                    target,
                    transaction.HotBarSlot,
                    transaction.Position,
                    transaction.ClickedPosition));
                break;

            case 1:
                heldItem.OnUseAttack(new ItemUseAttackDetails(
                    player,
                    target,
                    transaction.HotBarSlot,
                    transaction.Position,
                    transaction.ClickedPosition));
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

    private static void SendBlockUpdate(Player player, BlockPos position, int networkId)
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

    private static bool FindBlockFromView(Player player, float pitchDegrees, float yawDegrees, out BlockPos blockPosition, out int face)
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

        float startX = player.Position.X;
        float startY = player.Position.Y + 1.62f;
        float startZ = player.Position.Z;

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

            Basalt.Block.BlockPermutation block =
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

