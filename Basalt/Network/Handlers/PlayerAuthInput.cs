using Basalt.Core;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;
using Basalt.Entity.Traits.Types;
using System.Collections.Concurrent;

namespace Basalt.Network.Handlers;

public static class PlayerAuthInput
{
    private const float MaxHorizontalDeltaPerTick = 2.0f;
    private static readonly ConcurrentDictionary<ulong, ulong> LastInputTickByRuntimeId = new();

    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        PlayerAuthInputPacket packet = new();
        packet.Deserialize(packetBuffer);

        if (!server.Players.TryGetValue(connection, out Player? player))
        {
            return;
        }

        float deltaX = packet.Position.X - player.Position.X;
        float deltaZ = packet.Position.Z - player.Position.Z;
        float horizontalDistanceSquared = (deltaX * deltaX) + (deltaZ * deltaZ);
        ulong previousInputTick = LastInputTickByRuntimeId.GetOrAdd(player.RuntimeId, packet.Tick);
        ulong tickDeltaRaw = packet.Tick > previousInputTick ? packet.Tick - previousInputTick : 1UL;
        float tickDelta = Math.Clamp((float)tickDeltaRaw, 1f, 20f);
        float maxHorizontalDelta = MaxHorizontalDeltaPerTick * tickDelta;
        float maxHorizontalDeltaSquared = maxHorizontalDelta * maxHorizontalDelta;

        if (horizontalDistanceSquared > maxHorizontalDeltaSquared)
        {
            Logger.Warn($"Player {player.Username} moved too fast ({packet.Position.X}, {packet.Position.Y}, {packet.Position.Z}) tickDelta={tickDeltaRaw}");
            
            CorrectPlayerMovePredictionPacket correction = new()
            {
                PredictionType = PredictionType.Player,
                Position = player.Position,
                PositionDelta = new Vec3f { X = 0f, Y = 0f, Z = 0f },
                Rotation = new Vec2f { X = packet.Pitch, Y = packet.Yaw },
                VehicleAngularVelocity = new OptionalValue<float> { HasValue = false },
                OnGround = packet.HasFlag(PlayerAuthInputFlag.VerticalCollision),
                InputTick = packet.Tick
            };

            server.Network.SendPacket(connection, correction);
            LastInputTickByRuntimeId[player.RuntimeId] = packet.Tick;
            return;
        }

        Vec3f previousPosition = player.Position;
        bool zeroPosition = packet.Position.X == 0f && packet.Position.Y == 0f && packet.Position.Z == 0f;
        if (zeroPosition && (packet.Delta.X != 0f || packet.Delta.Y != 0f || packet.Delta.Z != 0f))
        {
            player.Position = new Vec3f
            {
                X = previousPosition.X + packet.Delta.X,
                Y = previousPosition.Y + packet.Delta.Y,
                Z = previousPosition.Z + packet.Delta.Z
            };
        }
        else
        {
            player.Position = packet.Position;
        }
        player.OnMove(new EntityMoveOptions(previousPosition, player.Position));

        if (packet.HasFlag(PlayerAuthInputFlag.PerformItemInteraction))
        {
            InventoryTransaction.HandleUseItemFromAuthInput(player, packet.ItemInteractionData, packet.InteractPitch, packet.InteractYaw);
        }

        if (packet.HasFlag(PlayerAuthInputFlag.PerformBlockActions))
        {
            foreach (PlayerBlockAction action in packet.BlockActions)
            {
                switch (action.Action)
                {
                    case PlayerActionType.StartDestroyBlock:
                    case PlayerActionType.CrackBlock: // Handle CrackBlock same as StartDestroyBlock
                    {
                        if (player.BreakingBlock.HasValue && (player.BreakingBlock.Value.X != action.BlockPos.X || player.BreakingBlock.Value.Y != action.BlockPos.Y || player.BreakingBlock.Value.Z != action.BlockPos.Z))
                        {
                            LevelEventPacket stopOldCracking = new()
                            {
                                Event = LevelEvent.StopBlockCracking,
                                Position = new Vec3f { X = player.BreakingBlock.Value.X + 0.5f, Y = player.BreakingBlock.Value.Y + 0.5f, Z = player.BreakingBlock.Value.Z + 0.5f },
                                Data = 0
                            };
                            player.Dimension?.Broadcast(stopOldCracking);
                        }
                        
                        player.BreakingBlock = action.BlockPos;

                        Basalt.Block.BlockPermutation? block = player.Dimension?.GetPermutation(action.BlockPos.X, action.BlockPos.Y, action.BlockPos.Z);
                        int breakTimeTicks = 20; // fallback
                        if (block != null)
                        {
                            float hardness = block.Type.Hardness;
                            if (hardness < 0) breakTimeTicks = 65535; // unbreakable
                            else if (hardness == 0) breakTimeTicks = 1; // instant
                            else breakTimeTicks = (int)(hardness * 1.5f * 20);
                        }
                        
                        breakTimeTicks = Math.Max(1, breakTimeTicks);

                        LevelEventPacket startCracking = new()
                        {
                            Event = LevelEvent.StartBlockCracking,
                            Position = new Vec3f { X = action.BlockPos.X + 0.5f, Y = action.BlockPos.Y + 0.5f, Z = action.BlockPos.Z + 0.5f },
                            Data = 65535 / breakTimeTicks
                        };
                        player.Dimension?.Broadcast(startCracking);
                        break;
                    }

                    case PlayerActionType.ContinueDestroyBlock:
                    {
                        if (player.BreakingBlock.HasValue && (player.BreakingBlock.Value.X != action.BlockPos.X || player.BreakingBlock.Value.Y != action.BlockPos.Y || player.BreakingBlock.Value.Z != action.BlockPos.Z))
                        {
                            LevelEventPacket stopOldCracking = new()
                            {
                                Event = LevelEvent.StopBlockCracking,
                                Position = new Vec3f { X = player.BreakingBlock.Value.X + 0.5f, Y = player.BreakingBlock.Value.Y + 0.5f, Z = player.BreakingBlock.Value.Z + 0.5f },
                                Data = 0
                            };
                            player.Dimension?.Broadcast(stopOldCracking);
                        }
                        
                        player.BreakingBlock = action.BlockPos;

                        Basalt.Block.BlockPermutation? block = player.Dimension?.GetPermutation(action.BlockPos.X, action.BlockPos.Y, action.BlockPos.Z);
                        int breakTimeTicks = 20; // fallback
                        if (block != null)
                        {
                            float hardness = block.Type.Hardness;
                            if (hardness < 0) breakTimeTicks = 65535; // unbreakable
                            else if (hardness == 0) breakTimeTicks = 1; // instant
                            else breakTimeTicks = (int)(hardness * 1.5f * 20);
                        }
                        
                        breakTimeTicks = Math.Max(1, breakTimeTicks);

                        LevelEventPacket startNewCracking = new()
                        {
                            Event = LevelEvent.StartBlockCracking,
                            Position = new Vec3f { X = action.BlockPos.X + 0.5f, Y = action.BlockPos.Y + 0.5f, Z = action.BlockPos.Z + 0.5f },
                            Data = 65535 / breakTimeTicks
                        };
                        player.Dimension?.Broadcast(startNewCracking);
                        break;
                    }

                    case PlayerActionType.AbortDestroyBlock:
                    {
                        BlockPos pos = player.BreakingBlock ?? action.BlockPos;
                        LevelEventPacket stopCracking = new()
                        {
                            Event = LevelEvent.StopBlockCracking,
                            Position = new Vec3f { X = pos.X + 0.5f, Y = pos.Y + 0.5f, Z = pos.Z + 0.5f },
                            Data = 0
                        };
                        player.Dimension?.Broadcast(stopCracking);
                        player.BreakingBlock = null;
                        break;
                    }

                    case PlayerActionType.PredictDestroyBlock:
                    {
                        BlockPos pos = action.BlockPos;
                        if (action.BlockPos.X == 0 && action.BlockPos.Y == 0 && action.BlockPos.Z == 0 && player.BreakingBlock.HasValue)
                        {
                            pos = player.BreakingBlock.Value;
                        }

                        LevelEventPacket stopCracking = new()
                        {
                            Event = LevelEvent.StopBlockCracking,
                            Position = new Vec3f { X = pos.X + 0.5f, Y = pos.Y + 0.5f, Z = pos.Z + 0.5f },
                            Data = 0
                        };
                        player.Dimension?.Broadcast(stopCracking);
                        player.BreakingBlock = null;

                        Basalt.Block.BlockPermutation? block = player.Dimension?.GetPermutation(pos.X, pos.Y, pos.Z);
                        if (block is not null)
                        {
                            LevelEventPacket particles = new()
                            {
                                Event = LevelEvent.ParticlesDestroyBlock,
                                Position = new Vec3f { X = pos.X + 0.5f, Y = pos.Y + 0.5f, Z = pos.Z + 0.5f },
                                Data = block.NetworkId
                            };
                            player.Dimension?.Broadcast(particles);

                            // LevelEventPacket with ParticlesDestroyBlock already handles both particles and the breaking sound.
                            // We avoid sending LevelSoundEventPacket to prevent duplicate sounds or version-specific client crashes.

                            player.Dimension?.SetPermutation(pos.X, pos.Y, pos.Z, Basalt.Block.BlockType.GetOrAir("minecraft:air").GetPermutation());

                            UpdateBlockPacket update = new()
                            {
                                Position = pos,
                                NetworkBlockId = (uint)Basalt.Block.BlockType.GetOrAir("minecraft:air").GetPermutation().NetworkId,
                                Flags = UpdateBlockFlagsType.Network,
                                Layer = UpdateBlockLayerType.Normal
                            };
                            player.Dimension?.Broadcast(update);
                        }

                        Basalt.Entity.Traits.EntityInventoryTrait? inventory = player.GetTrait<Basalt.Entity.Traits.EntityInventoryTrait>();
                        if (inventory is not null)
                        {
                            Basalt.Item.ItemStack? heldItem = inventory.GetHeldItem();
                            if (heldItem is not null)
                            {
                                Basalt.Item.Traits.Types.ItemBreakBlockDetails details = new(player, inventory.SelectedSlot, action.BlockPos, action.Face);
                                heldItem.OnBreakBlock(details);
                            }
                        }
                        break;
                    }
                }
            }
        }

        LastInputTickByRuntimeId[player.RuntimeId] = packet.Tick;
    }
}
