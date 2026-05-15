using System.Collections.Concurrent;
using Basalt.Core;
using Basalt.Entity.Traits;
using Basalt.Entity.Traits.Types;
using Basalt.Item;
using Basalt.Item.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class PlayerAuthInput
{
    private const float MaxHorizontalMovePerTick = 2.0f;

    private static readonly ConcurrentDictionary<ulong, ulong> LastInputTickByRuntimeId = new();

    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        PlayerAuthInputPacket packet = new();
        packet.Deserialize(packetBuffer);

        if (!server.Players.TryGetValue(connection, out Player? player))
        {
            return;
        }

        if (MovedTooFar(player, packet, out ulong tickDelta))
        {
            Logger.Warn($"Player {player.Username} moved too fast ({packet.Position.X}, {packet.Position.Y}, {packet.Position.Z}) tickDelta={tickDelta}");

            server.Network.SendPacket(connection, new CorrectPlayerMovePredictionPacket
            {
                PredictionType = PredictionType.Player,
                Position = player.Position,
                PositionDelta = new Vec3f { X = 0f, Y = 0f, Z = 0f },
                Rotation = new Vec2f { X = packet.Pitch, Y = packet.Yaw },
                VehicleAngularVelocity = new OptionalValue<float> { HasValue = false },
                OnGround = packet.HasFlag(PlayerAuthInputFlag.VerticalCollision),
                InputTick = packet.Tick
            });

            LastInputTickByRuntimeId[player.RuntimeId] = packet.Tick;
            return;
        }

        MovePlayer(player, packet);

        if (packet.HasFlag(PlayerAuthInputFlag.PerformItemInteraction))
        {
            InventoryTransaction.HandleUseItemFromAuthInput(
                player,
                packet.ItemInteractionData,
                packet.InteractPitch,
                packet.InteractYaw);
        }

        if (packet.HasFlag(PlayerAuthInputFlag.PerformBlockActions))
        {
            foreach (PlayerBlockAction action in packet.BlockActions)
            {
                HandleBlockAction(player, action);
            }
        }

        LastInputTickByRuntimeId[player.RuntimeId] = packet.Tick;
    }

    private static bool MovedTooFar(Player player, PlayerAuthInputPacket packet, out ulong rawTickDelta)
    {
        float deltaX = packet.Position.X - player.Position.X;
        float deltaZ = packet.Position.Z - player.Position.Z;
        float movedDistanceSquared = deltaX * deltaX + deltaZ * deltaZ;

        ulong previousTick = LastInputTickByRuntimeId.GetOrAdd(player.RuntimeId, packet.Tick);
        rawTickDelta = packet.Tick > previousTick ? packet.Tick - previousTick : 1UL;

        float tickDelta = Math.Clamp((float)rawTickDelta, 1f, 20f);
        float allowedDistance = MaxHorizontalMovePerTick * tickDelta;

        return movedDistanceSquared > allowedDistance * allowedDistance;
    }

    private static void MovePlayer(Player player, PlayerAuthInputPacket packet)
    {
        Vec3f previousPosition = player.Position;
        player.Pitch = packet.Pitch;
        player.Yaw = packet.Yaw;
        player.HeadYaw = packet.Yaw;

        bool missingPosition =
            packet.Position.X == 0f &&
            packet.Position.Y == 0f &&
            packet.Position.Z == 0f;

        bool hasDelta =
            packet.Delta.X != 0f ||
            packet.Delta.Y != 0f ||
            packet.Delta.Z != 0f;

        player.Position = missingPosition && hasDelta
            ? new Vec3f
            {
                X = previousPosition.X + packet.Delta.X,
                Y = previousPosition.Y + packet.Delta.Y,
                Z = previousPosition.Z + packet.Delta.Z
            }
            : packet.Position;

        player.OnMove(new EntityMoveOptions(previousPosition, player.Position));
    }

    private static void HandleBlockAction(Player player, PlayerBlockAction action)
    {
        switch (action.Action)
        {
            case PlayerActionType.StartDestroyBlock:
            case PlayerActionType.CrackBlock:
            case PlayerActionType.ContinueDestroyBlock:
                CrackBlock(player, action.BlockPos);
                break;

            case PlayerActionType.AbortDestroyBlock:
                StopCrackBlock(player, player.BreakingBlock ?? action.BlockPos);
                player.BreakingBlock = null;
                break;

            case PlayerActionType.PredictDestroyBlock:
                DestroyBlock(player, action);
                break;
        }
    }

    private static void CrackBlock(Player player, BlockPos blockPosition)
    {
        if (player.BreakingBlock.HasValue && !SameBlock(player.BreakingBlock.Value, blockPosition))
        {
            StopCrackBlock(player, player.BreakingBlock.Value);
        }

        player.BreakingBlock = blockPosition;

        int breakTimeTicks = GetBreakTimeTicks(player, blockPosition);

        player.Dimension?.Broadcast(new LevelEventPacket
        {
            Event = LevelEvent.StartBlockCracking,
            Position = CenterOf(blockPosition),
            Data = 65535 / breakTimeTicks
        });
    }

    private static void DestroyBlock(Player player, PlayerBlockAction action)
    {
        BlockPos blockPosition = IsZero(action.BlockPos) && player.BreakingBlock.HasValue
            ? player.BreakingBlock.Value
            : action.BlockPos;

        StopCrackBlock(player, blockPosition);
        player.BreakingBlock = null;

        Basalt.Block.BlockPermutation? block =
            player.Dimension?.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        if (block is null)
        {
            return;
        }

        player.Dimension?.Broadcast(new LevelEventPacket
        {
            Event = LevelEvent.ParticlesDestroyBlock,
            Position = CenterOf(blockPosition),
            Data = block.NetworkId
        });

        Basalt.Block.BlockPermutation air = Basalt.Block.BlockType
            .GetOrAir("minecraft:air")
            .GetPermutation();

        player.Dimension?.SetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z, air);

        player.Dimension?.Broadcast(new UpdateBlockPacket
        {
            Position = blockPosition,
            NetworkBlockId = (uint)air.NetworkId,
            Flags = UpdateBlockFlagsType.Network,
            Layer = UpdateBlockLayerType.Normal
        });

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.GetHeldItem();

        if (inventory is not null && heldItem is not null)
        {
            heldItem.OnBreakBlock(new ItemBreakBlockDetails(
                player,
                inventory.SelectedSlot,
                action.BlockPos,
                action.Face));
        }
    }

    private static void StopCrackBlock(Player player, BlockPos blockPosition)
    {
        player.Dimension?.Broadcast(new LevelEventPacket
        {
            Event = LevelEvent.StopBlockCracking,
            Position = CenterOf(blockPosition),
            Data = 0
        });
    }

    private static int GetBreakTimeTicks(Player player, BlockPos blockPosition)
    {
        Basalt.Block.BlockPermutation? block =
            player.Dimension?.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        if (block is null)
        {
            return 20;
        }

        float hardness = block.Type.Hardness;

        if (hardness < 0)
        {
            return 65535;
        }

        if (hardness == 0)
        {
            return 1;
        }

        return Math.Max(1, (int)(hardness * 1.5f * 20));
    }

    private static Vec3f CenterOf(BlockPos position)
    {
        return new Vec3f
        {
            X = position.X + 0.5f,
            Y = position.Y + 0.5f,
            Z = position.Z + 0.5f
        };
    }

    private static bool SameBlock(BlockPos a, BlockPos b)
    {
        return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    }

    private static bool IsZero(BlockPos position)
    {
        return position.X == 0 && position.Y == 0 && position.Z == 0;
    }
}
