using Basalt.Core;
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
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        InventoryTransactionPacket packet = new();
        packet.Deserialize(packetBuffer);

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
            case NormalInventoryTransactionData normal:
                HandleNormalTransaction(player, inventory, packet.Actions, normal);
                break;
            case UseItemInventoryTransactionData useItem:
                HandleUseItemTransaction(player, inventory, useItem);
                break;
            case UseItemOnEntityInventoryTransactionData useOnEntity:
                HandleUseItemOnEntityTransaction(player, inventory, useOnEntity);
                break;
            case ReleaseItemInventoryTransactionData:
            case MismatchInventoryTransactionData:
                break;
        }
    }

    public static void HandleUseItemFromAuthInput(Player player, UseItemTransactionData data, float interactPitch, float interactYaw)
    {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null)
        {
            return;
        }

        UseItemInventoryTransactionData mapped = new()
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

        if (mapped.BlockPosition.X == 0 && mapped.BlockPosition.Y == 0 && mapped.BlockPosition.Z == 0
            && TryResolveFromView(player, interactPitch, interactYaw, out BlockPos lookedBlock, out int lookedFace))
        {
            mapped.BlockPosition = lookedBlock;
            mapped.BlockFace = lookedFace;
        }
        else if (mapped.BlockPosition.X == 0 && mapped.BlockPosition.Y == 0 && mapped.BlockPosition.Z == 0)
        {
            BlockPos aroundPlayer = new()
            {
                X = (int)MathF.Floor(player.Position.X),
                Y = (int)MathF.Floor(player.Position.Y - 1f),
                Z = (int)MathF.Floor(player.Position.Z)
            };
            mapped.BlockPosition = aroundPlayer;
            if (mapped.BlockFace is < 0 or > 5)
            {
                mapped.BlockFace = 1;
            }
        }

        HandleUseItemTransaction(player, inventory, mapped);
    }

    private static void HandleNormalTransaction(Player player, EntityInventoryTrait inventory, List<InventoryAction> actions, NormalInventoryTransactionData _)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            InventoryAction action = actions[i];
            if (action.SourceType == (uint)InventoryActionSourceType.World)
            {
                inventory.Container.Update();
                return;
            }
        }
    }


    private static void HandleUseItemTransaction(Player player, EntityInventoryTrait inventory, UseItemInventoryTransactionData transaction)
    {

        if (!TryGetHeldItem(inventory, transaction.HotBarSlot, out ItemStack heldItem))
        {
            return;
        }


        switch (transaction.ActionType)
        {
            case 0:
                HandleUseItemPlaceOrInteract(player, inventory, heldItem, transaction);
                break;
            case 1:
                heldItem.OnUseOnAir(new ItemUseOnAirDetails(player, transaction.HotBarSlot, transaction.Position));
                break;
            case 2:
                HandleUseItemPlaceOrInteract(player, inventory, heldItem, transaction);
                break;
            default:
                HandleUseItemPlaceOrInteract(player, inventory, heldItem, transaction);
                break;
        }
    }

    private static void HandleUseItemPlaceOrInteract(Player player, EntityInventoryTrait inventory, ItemStack heldItem, UseItemInventoryTransactionData transaction)
    {
        if (player.Dimension is null)
        {
            return;
        }

        BlockPos interactedPos = transaction.BlockPosition;
        int blockFace = transaction.BlockFace;
        if (interactedPos.X == 0 && interactedPos.Y == 0 && interactedPos.Z == 0 && player.LastActionBlockPosition.HasValue)
        {
            interactedPos = player.LastActionBlockPosition.Value;
            if (player.LastActionFace >= 0 && player.LastActionFace <= 5)
            {
                blockFace = player.LastActionFace;
            }
        }

        Basalt.Block.BlockPermutation interacted = player.Dimension.GetPermutation(interactedPos.X, interactedPos.Y, interactedPos.Z);
        BlockPos placedPos = GetResultantPosition(interactedPos, blockFace);
        Basalt.Block.BlockPermutation currentPlaced = player.Dimension.GetPermutation(placedPos.X, placedPos.Y, placedPos.Z);

        Basalt.Block.BlockType? blockType = ResolvePlaceBlockType(heldItem);
        if (blockType is null)
        {
            heldItem.OnUseOnBlock(new ItemUseOnBlockDetails(player, transaction.HotBarSlot, interactedPos, blockFace, transaction.Position, transaction.ClickedPosition));
            SyncBlockToPlayer(player, placedPos, currentPlaced.NetworkId);
            return;
        }


        if (IsSelfFeetBlock(player, placedPos))
        {
            SyncBlockToPlayer(player, placedPos, currentPlaced.NetworkId);
            return;
        }

        if (IsBlockOccupiedByEntity(player, placedPos))
        {
            SyncBlockToPlayer(player, placedPos, currentPlaced.NetworkId);
            return;
        }

        if (string.Equals(currentPlaced.Type.Identifier, blockType.Identifier, StringComparison.Ordinal))
        {
            SyncBlockToPlayer(player, placedPos, currentPlaced.NetworkId);
            return;
        }

        if (!CanReplace(currentPlaced.Type.Identifier))
        {
            SyncBlockToPlayer(player, placedPos, currentPlaced.NetworkId);
            return;
        }

        Basalt.Block.BlockPermutation permutation = blockType.Permutations.Count > 0
            ? blockType.Permutations[0]
            : blockType.GetPermutation();

        player.Dimension.SetPermutation(placedPos.X, placedPos.Y, placedPos.Z, permutation);

        UpdateBlockPacket updateBlock = new()
        {
            Position = placedPos,
            NetworkBlockId = (uint)permutation.NetworkId,
            Flags = UpdateBlockFlagsType.Network,
            Layer = UpdateBlockLayerType.Normal
        };
        player.Dimension.Broadcast(updateBlock);
        BroadcastPlaceSound(player, placedPos, permutation.NetworkId);

        heldItem.OnPlace(new ItemPlaceDetails(player, transaction.HotBarSlot, interactedPos, blockFace, transaction.Position, transaction.ClickedPosition));

        if (player.Gamemode != Gamemode.Survival)
        {
            return;
        }

        heldItem.DecrementStack();
        if (heldItem.StackSize == 0)
        {
            inventory.Container.ClearSlot(inventory.SelectedSlot);
            return;
        }

        inventory.Container.UpdateSlot(inventory.SelectedSlot);
    }

    private static void HandleUseItemOnEntityTransaction(Player player, EntityInventoryTrait inventory, UseItemOnEntityInventoryTransactionData transaction)
    {
        if (!TryGetHeldItem(inventory, transaction.HotBarSlot, out ItemStack heldItem))
        {
            return;
        }

        Basalt.Entity.Entity? target = ResolveTargetEntity(player, transaction.TargetEntityRuntimeId);
        if (target is null)
        {
            return;
        }

        switch (transaction.ActionType)
        {
            case 0:
                heldItem.OnUseOnEntity(new ItemUseOnEntityDetails(player, target, transaction.HotBarSlot, transaction.Position, transaction.ClickedPosition));
                break;
            case 1:
                heldItem.OnUseAttack(new ItemUseAttackDetails(player, target, transaction.HotBarSlot, transaction.Position, transaction.ClickedPosition));
                break;
        }
    }

    private static Basalt.Block.BlockType? ResolvePlaceBlockType(ItemStack heldItem)
    {
        Basalt.Block.BlockType? blockType = heldItem.Type.BlockType ?? Basalt.Block.BlockType.Get(heldItem.Identifier);
        if (blockType is null || blockType.Identifier == "minecraft:air")
        {
            return null;
        }

        return blockType;
    }

    private static void SyncBlockToPlayer(Player player, BlockPos pos, int networkId)
    {
        UpdateBlockPacket update = new()
        {
            Position = pos,
            NetworkBlockId = (uint)networkId,
            Flags = UpdateBlockFlagsType.Network,
            Layer = UpdateBlockLayerType.Normal
        };
        player.Send(update);
    }

    private static void BroadcastPlaceSound(Player player, BlockPos pos, int networkId)
    {
        if (player.Dimension is null)
        {
            return;
        }

        LevelSoundEventPacket placeSound = new()
        {
            Event = LevelSoundEvent.Place,
            Position = new Vec3f { X = pos.X + 0.5f, Y = pos.Y + 0.5f, Z = pos.Z + 0.5f },
            Data = networkId,
            ActorIdentifier = string.Empty,
            IsBabyMob = false,
            IsGlobal = false,
            UniqueActorId = 0,
            FireAtPosition = new Optional<Vec3f> { HasValue = false, Value = default }
        };
        player.Dimension.Broadcast(placeSound);
    }

    private static BlockPos GetResultantPosition(BlockPos pos, int face)
    {
        return face switch
        {
            0 => new BlockPos { X = pos.X, Y = pos.Y - 1, Z = pos.Z },
            1 => new BlockPos { X = pos.X, Y = pos.Y + 1, Z = pos.Z },
            2 => new BlockPos { X = pos.X, Y = pos.Y, Z = pos.Z - 1 },
            3 => new BlockPos { X = pos.X, Y = pos.Y, Z = pos.Z + 1 },
            4 => new BlockPos { X = pos.X - 1, Y = pos.Y, Z = pos.Z },
            5 => new BlockPos { X = pos.X + 1, Y = pos.Y, Z = pos.Z },
            _ => pos
        };
    }

    private static bool TryGetHeldItem(EntityInventoryTrait inventory, int hotBarSlot, out ItemStack heldItem)
    {
        heldItem = null!;
        if (hotBarSlot < 0 || hotBarSlot >= 9)
        {
            hotBarSlot = 0;
        }

        inventory.SetHeldItem(hotBarSlot);
        ItemStack? item = inventory.GetHeldItem();
        if (item is null || item.StackSize == 0)
        {
            return false;
        }

        heldItem = item;
        return true;
    }

    private static bool IsBlockOccupiedByEntity(Player player, BlockPos pos)
    {
        if (player.Dimension is null)
        {
            return false;
        }

        float blockMinX = pos.X;
        float blockMaxX = pos.X + 1f;
        float blockMinY = pos.Y;
        float blockMaxY = pos.Y + 1f;
        float blockMinZ = pos.Z;
        float blockMaxZ = pos.Z + 1f;

        foreach (Basalt.Entity.Entity entity in player.Dimension.Entities)
        {
            if (!entity.IsAlive || ReferenceEquals(entity, player))
            {
                continue;
            }

            float halfWidth = 0.3f;
            float height = 1.8f;

            float entityMinX = entity.Position.X - halfWidth;
            float entityMaxX = entity.Position.X + halfWidth;
            float entityMinY = entity.Position.Y;
            float entityMaxY = entity.Position.Y + height;
            float entityMinZ = entity.Position.Z - halfWidth;
            float entityMaxZ = entity.Position.Z + halfWidth;

            bool overlapX = entityMaxX > blockMinX && entityMinX < blockMaxX;
            bool overlapY = entityMaxY > blockMinY && entityMinY < blockMaxY;
            bool overlapZ = entityMaxZ > blockMinZ && entityMinZ < blockMaxZ;
            if (overlapX && overlapY && overlapZ)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSelfFeetBlock(Player player, BlockPos pos)
    {
        const float halfWidth = 0.3f;
        float minX = player.Position.X - halfWidth;
        float maxX = player.Position.X + halfWidth;
        float minZ = player.Position.Z - halfWidth;
        float maxZ = player.Position.Z + halfWidth;

        int minBlockX = (int)MathF.Floor(minX);
        int maxBlockX = (int)MathF.Floor(maxX);
        int minBlockZ = (int)MathF.Floor(minZ);
        int maxBlockZ = (int)MathF.Floor(maxZ);

        if (pos.X < minBlockX || pos.X > maxBlockX || pos.Z < minBlockZ || pos.Z > maxBlockZ)
        {
            return false;
        }

        int bodyY = (int)MathF.Floor(player.Position.Y);
        int supportY = bodyY - 1;
        return pos.Y == bodyY || pos.Y == supportY;
    }

    private static bool CanReplace(string identifier)
    {
        return identifier is
            "minecraft:air" or
            "minecraft:cave_air" or
            "minecraft:void_air" or
            "minecraft:water" or
            "minecraft:flowing_water" or
            "minecraft:lava" or
            "minecraft:flowing_lava" or
            "minecraft:short_grass" or
            "minecraft:tall_grass" or
            "minecraft:fern" or
            "minecraft:large_fern" or
            "minecraft:dead_bush" or
            "minecraft:vine" or
            "minecraft:seagrass" or
            "minecraft:tall_seagrass" or
            "minecraft:snow_layer" or
            "minecraft:fire";
    }


    private static Basalt.Entity.Entity? ResolveTargetEntity(Player player, ulong runtimeId)
    {
        if (player.Dimension is null)
        {
            return null;
        }

        foreach (Basalt.Entity.Entity entity in player.Dimension.Entities)
        {
            if (entity.RuntimeId == runtimeId)
            {
                return entity;
            }
        }

        return null;
    }

    private static bool TryResolveFromView(Player player, float pitchDegrees, float yawDegrees, out BlockPos blockPos, out int face)
    {
        blockPos = default;
        face = 1;

        if (player.Dimension is null)
        {
            return false;
        }

        float yaw = MathF.PI / 180f * yawDegrees;
        float pitch = MathF.PI / 180f * pitchDegrees;

        float dirX = -MathF.Sin(yaw) * MathF.Cos(pitch);
        float dirY = -MathF.Sin(pitch);
        float dirZ = MathF.Cos(yaw) * MathF.Cos(pitch);

        float startX = player.Position.X;
        float startY = player.Position.Y + 1.62f;
        float startZ = player.Position.Z;

        int prevX = (int)MathF.Floor(startX);
        int prevY = (int)MathF.Floor(startY);
        int prevZ = (int)MathF.Floor(startZ);

        const float maxDistance = 6.0f;
        const float step = 0.1f;

        for (float t = step; t <= maxDistance; t += step)
        {
            float px = startX + dirX * t;
            float py = startY + dirY * t;
            float pz = startZ + dirZ * t;

            int bx = (int)MathF.Floor(px);
            int by = (int)MathF.Floor(py);
            int bz = (int)MathF.Floor(pz);

            Basalt.Block.BlockPermutation perm = player.Dimension.GetPermutation(bx, by, bz);
            if (perm.Type.Identifier != "minecraft:air")
            {
                blockPos = new BlockPos { X = bx, Y = by, Z = bz };

                int dx = prevX - bx;
                int dy = prevY - by;
                int dz = prevZ - bz;

                face = (dx, dy, dz) switch
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

            prevX = bx;
            prevY = by;
            prevZ = bz;
        }

        return false;
    }
}
