namespace Basalt.Core.Network.Handlers;

using System.Collections.Concurrent;
using Basalt.Core;
using Basalt.Core.Block.Traits.Types;
using Basalt.Core.Entity.Traits;
using Basalt.Core.Entity.Traits.Types;
using Basalt.Core.Events;
using Basalt.Core.Item;
using Basalt.Core.Item.Traits;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Player.Traits;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;


public static class PlayerAuthInput
{
    private const float MaxHorizontalMovePerTick = 2.0f;
    private const ulong DefaultFoodUseTicks = 32UL;

    private static readonly ConcurrentDictionary<ulong, ulong> LastInputTickByRuntimeId = new();
    private static readonly ConcurrentDictionary<ulong, PendingItemUse> PendingItemUses = new();

    private readonly record struct PendingItemUse(int Slot, int StackNetworkId, ulong FinishTick);

    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        PlayerAuthInputPacket packet = new();
        try
        {
            int offset = 0;
            Binary.BinaryReader reader = new(packetBuffer, ref offset);
            packet = (PlayerAuthInputPacket)Protocol.Io.Packet.Deserialize(reader);
        }
        catch (Exception exception)
        {
            Logger.Warn("PlayerAuthInput deserialize failed: {0}", exception);
            return;
        }

        try
        {
            if (!server.Players.TryGetValue(connection, out global::Basalt.Core.Player.Player? player))
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
                    OnGround = packet.InputData.HasFlag(PlayerAuthInputFlag.VerticalCollision),
                    InputTick = packet.Tick
                });

                LastInputTickByRuntimeId[player.RuntimeId] = packet.Tick;
                return;
            }

            MovePlayer(player, packet);
            TickPendingItemUse(player);

            if (packet.InputData.HasFlag(PlayerAuthInputFlag.PerformItemInteraction))
            {
                InventoryTransaction.HandleUseItemFromAuthInput(
                    player,
                    packet.ItemInteractionData,
                    packet.InteractPitch,
                    packet.InteractYaw);
            }

            MineBlockStackRequestAction? mineBlockRequest = null;
            if (packet.InputData.HasFlag(PlayerAuthInputFlag.PerformItemStackRequest))
            {
                mineBlockRequest = GetMineBlockRequest(packet.ItemStackRequest);
                Logger.Warn(
                    "PlayerAuthInput item stack request player={0} request={1} actions={2} mineBlock={3}",
                    player.Username,
                    packet.ItemStackRequest.RequestId,
                    packet.ItemStackRequest.Actions.Count,
                    mineBlockRequest is not null);

                server.Network.SendPacket(connection, new ItemStackResponsePacket
                {
                    Responses = [ProcessItemStackRequest(player, packet.ItemStackRequest)]
                });
            }

            if (packet.InputData.HasFlag(PlayerAuthInputFlag.PerformBlockActions))
            {
                // Logger.Warn(
                //     "PlayerAuthInput block actions player={0} count={1} tick={2}",
                //     player.Username,
                //     packet.BlockActions.Count,
                //     packet.Tick);

                foreach (PlayerBlockAction action in packet.BlockActions)
                {
                    HandleBlockAction(player, action);
                }
            }

            if (packet.InputData.HasFlag(PlayerAuthInputFlag.StartUsingItem))
            {
                StartUsingItem(player);
            }

            if (packet.InputData.HasFlag(PlayerAuthInputFlag.StartSprinting))
            {
                player.IsSprinting = true;
            }

            else if (packet.InputData.HasFlag(PlayerAuthInputFlag.StopSprinting))
            {
                player.IsSprinting = false;
            }

            if (packet.InputData.HasFlag(PlayerAuthInputFlag.StartSneaking))
            {
                player.IsSneaking = true;
            }

            else if (packet.InputData.HasFlag(PlayerAuthInputFlag.StopSneaking))
            {
                player.IsSneaking = false;
            }
            else if (mineBlockRequest is not null && player.LastActionBlockPosition.HasValue)
            {
                BlockPos position = player.LastActionBlockPosition.Value;
                Logger.Warn(
                    "PlayerAuthInput mine fallback player={0} pos={1},{2},{3} face={4}",
                    player.Username,
                    position.X,
                    position.Y,
                    position.Z,
                    player.LastActionFace);

                DestroyBlock(player, new PlayerBlockAction
                {
                    Action = PlayerActionType.PredictDestroyBlock,
                    BlockPos = position,
                    Face = player.LastActionFace
                });
            }
            else if (mineBlockRequest is not null)
            {
                Logger.Warn("PlayerAuthInput mine request had no block actions and no last PlayerAction target player={0}", player.Username);
            }

            LastInputTickByRuntimeId[player.RuntimeId] = packet.Tick;
        }
        catch (Exception exception)
        {
            Logger.Warn("PlayerAuthInput handler failed: {0}", exception);
        }
    }

    private static void StartUsingItem(global::Basalt.Core.Player.Player player)
    {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.GetHeldItem();
        ItemStackFoodTrait? food = heldItem?.GetTrait<ItemStackFoodTrait>();
        if (inventory is null || heldItem is null || food is null)
        {
            PendingItemUses.TryRemove(player.RuntimeId, out _);
            player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
            return;
        }

        PlayerHungerTrait? hunger = player.GetTrait<PlayerHungerTrait>();
        if (hunger is null || (!food.CanAlwaysEat && hunger.CurrentValue >= hunger.MaximumValue))
        {
            PendingItemUses.TryRemove(player.RuntimeId, out _);
            player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
            return;
        }

        ulong currentTick = GetCurrentTick(player);
        ulong useTicks = GetUseDurationTicks(heldItem);
        PendingItemUses[player.RuntimeId] = new PendingItemUse(
            inventory.SelectedSlot,
            heldItem.NetworkStackId,
            currentTick + Math.Max(1UL, useTicks));

        player.Flags.SetActorFlag(ActorFlag.UsingItem, true);
    }

    private static void TickPendingItemUse(global::Basalt.Core.Player.Player player)
    {
        if (!PendingItemUses.TryGetValue(player.RuntimeId, out PendingItemUse pending))
        {
            return;
        }

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.Container.GetItem(pending.Slot);
        if (inventory is null || heldItem is null || heldItem.NetworkStackId != pending.StackNetworkId)
        {
            PendingItemUses.TryRemove(player.RuntimeId, out _);
            player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
            return;
        }

        if (GetCurrentTick(player) < pending.FinishTick)
        {
            return;
        }

        PendingItemUses.TryRemove(player.RuntimeId, out _);
        player.Flags.SetActorFlag(ActorFlag.UsingItem, false);

        ItemStackFoodTrait? food = heldItem.GetTrait<ItemStackFoodTrait>();
        PlayerHungerTrait? hunger = player.GetTrait<PlayerHungerTrait>();
        if (food is null || hunger is null || !hunger.Eat(food.Nutrition, food.SaturationModifier, food.CanAlwaysEat))
        {
            return;
        }

        heldItem.DecrementStack();
        if (heldItem.StackSize == 0)
        {
            inventory.Container.ClearSlot(pending.Slot);
        }
        else
        {
            inventory.Container.UpdateSlot(pending.Slot);
        }

        if (!string.IsNullOrWhiteSpace(food.UsingConvertsTo) && ItemType.Get(food.UsingConvertsTo) is ItemType convertedType)
        {
            ItemStack converted = new(convertedType);
            if (!inventory.Container.AddItem(converted))
            {
                _ = player.DropItem(converted);
            }
        }

        player.SendAttributes();
    }

    private static ulong GetCurrentTick(global::Basalt.Core.Player.Player player)
    {
        return player.Dimension?.World is Basalt.Core.World.Tickable tickable ? tickable.TickValue : 0UL;
    }

    private static ulong GetUseDurationTicks(ItemStack item)
    {
        if (item.Type.TryGetComponentProperties("minecraft:use_duration", out Basalt.Protocol.Nbt.CompoundTag tag))
        {
            return (ulong)Math.Max(1, tag.Get<Basalt.Protocol.Nbt.IntTag>("value")?.Value ?? (int)DefaultFoodUseTicks);
        }

        return DefaultFoodUseTicks;
    }

    private static ItemStackResponse ProcessItemStackRequest(global::Basalt.Core.Player.Player player, Protocol.Types.ItemStackRequest request)
    {
        List<StackResponseContainerInfo> containers = [];

        for (int i = 0; i < request.Actions.Count; i++)
        {
            if (request.Actions[i] is not MineBlockStackRequestAction mineBlock)
            {
                continue;
            }

            EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
            ItemStack? item = inventory?.Container.GetItem(mineBlock.HotbarSlot);

            containers.Add(new StackResponseContainerInfo
            {
                Container = new FullContainerName { ContainerId = 29 },
                SlotInfo =
                [
                    new StackResponseSlotInfo
                    {
                        Slot = (byte)mineBlock.HotbarSlot,
                        HotbarSlot = (byte)mineBlock.HotbarSlot,
                        Count = (byte)(item?.StackSize ?? 0),
                        StackNetworkId = item?.NetworkStackId ?? 0,
                        CustomName = string.Empty,
                        FilteredCustomName = string.Empty,
                        DurabilityCorrection = 0
                    }
                ]
            });
        }

        return new ItemStackResponse
        {
            Status = ItemStackResponseStatus.Ok,
            RequestId = request.RequestId,
            ContainerInfo = containers
        };
    }

    private static MineBlockStackRequestAction? GetMineBlockRequest(Protocol.Types.ItemStackRequest request)
    {
        for (int i = 0; i < request.Actions.Count; i++)
        {
            if (request.Actions[i] is MineBlockStackRequestAction mineBlock)
            {
                return mineBlock;
            }
        }

        return null;
    }

    private static bool MovedTooFar(global::Basalt.Core.Player.Player player, PlayerAuthInputPacket packet, out ulong rawTickDelta)
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

    private static void MovePlayer(global::Basalt.Core.Player.Player player, PlayerAuthInputPacket packet)
    {
        Vec3f previousPosition = player.Position;

        MovementRotation fromRotation = new MovementRotation()
        {
            HeadYaw = player.HeadYaw,
            Pitch = player.Pitch,
            Yaw = player.Yaw,
        };

        MovementRotation toRotation = new MovementRotation()
        {
            HeadYaw = packet.Yaw,
            Pitch = packet.Pitch,
            Yaw = packet.Yaw,
        };

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

        player.OnMove(new EntityMoveOptions(previousPosition, player.Position, fromRotation, toRotation));

    }

    private static void HandleBlockAction(global::Basalt.Core.Player.Player player, PlayerBlockAction action)
    {
        // Logger.Warn(
        //     "PlayerAuthInput block action player={0} action={1} pos={2},{3},{4} face={5}",
        //     player.Username,
        //     action.Action,
        //     action.BlockPos.X,
        //     action.BlockPos.Y,
        //     action.BlockPos.Z,
        //     action.Face);

        switch (action.Action)
        {
            case PlayerActionType.StartDestroyBlock:
                CrackBlock(player, action.BlockPos);
                break;

            case PlayerActionType.CrackBlock:
            case PlayerActionType.ContinueDestroyBlock:
                CrackBlock(player, action.BlockPos);
                break;

            case PlayerActionType.AbortDestroyBlock:
                StopCrackBlock(player, player.BreakingBlock ?? action.BlockPos);
                player.BreakingBlock = null;
                break;

            case PlayerActionType.StopDestroyBlock:
            case PlayerActionType.PredictDestroyBlock:
            case PlayerActionType.CreativeDestroyBlock:
                DestroyBlock(player, action);
                break;
        }
    }

    private static void CrackBlock(global::Basalt.Core.Player.Player player, BlockPos blockPosition)
    {
        if (player.BreakingBlock.HasValue && !SameBlock(player.BreakingBlock.Value, blockPosition))
        {
            StopCrackBlock(player, player.BreakingBlock.Value);
        }

        player.BreakingBlock = blockPosition;
        int breakTimeTicks = GetBreakTimeTicksForAnimation(player, blockPosition);
        int crackSpeed = Math.Max(1, 65535 / breakTimeTicks);

        player.Dimension?.Broadcast(new LevelEventPacket
        {
            Event = LevelEvent.StartBlockCracking,
            Position = CenterOf(blockPosition),
            Data = crackSpeed
        });
    }

    private static void DestroyBlock(global::Basalt.Core.Player.Player player, PlayerBlockAction action)
    {
        if (IsZero(action.BlockPos) && !player.BreakingBlock.HasValue)
        {
            // Logger.Warn("PlayerAuthInput destroy skipped player={0} reason=zero-position-no-target action={1}", player.Username, action.Action);
            return;
        }

        BlockPos blockPosition = IsZero(action.BlockPos)
            ? player.BreakingBlock!.Value
            : action.BlockPos;

        StopCrackBlock(player, blockPosition);
        player.BreakingBlock = null;

        if (player.Dimension is null)
        {
            // Logger.Warn("PlayerAuthInput destroy skipped player={0} reason=no-dimension", player.Username);
            return;
        }

        Basalt.Core.Block.BlockPermutation? block =
            player.Dimension.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        if (block is null)
        {
            // Logger.Warn(
            //     "PlayerAuthInput destroy skipped player={0} reason=null-block pos={1},{2},{3}",
            //     player.Username,
            //     blockPosition.X,
            //     blockPosition.Y,
            //     blockPosition.Z);
            return;
        }

        // Logger.Warn(
        //     "PlayerAuthInput destroy attempt player={0} pos={1},{2},{3} before={4} network={5} action={6}",
        //     player.Username,
        //     blockPosition.X,
        //     blockPosition.Y,
        //     blockPosition.Z,
        //     block.Type.Identifier,
        //     block.NetworkId,
        //     action.Action);

        Server? server = player.Dimension.World?.Server;
        if (server is not null)
        {
            PlayerBreakBlockSignal signal = new(player, blockPosition, action.Face);
            server.Emit(signal);
            if (!signal.Emit())
            {
                player.Send(new UpdateBlockPacket
                {
                    Position = blockPosition,
                    NetworkBlockId = (uint)block.NetworkId,
                    Flags = UpdateBlockFlagsType.Network,
                    Layer = UpdateBlockLayerType.Normal
                });

                EntityInventoryTrait? cancelInventory = player.GetTrait<EntityInventoryTrait>();
                if (cancelInventory is not null)
                {
                    ItemStack? rollbackItem = cancelInventory.GetHeldItem();
                    if (rollbackItem is not null)
                    {
                        cancelInventory.Container.SetItem(cancelInventory.SelectedSlot, rollbackItem.Clone());
                    }
                    cancelInventory.Container.UpdateSlot(cancelInventory.SelectedSlot);
                    cancelInventory.Container.Update();
                    cancelInventory.SyncToPlayer(player);
                }
                return;
            }
        }

        player.Dimension.Broadcast(new LevelEventPacket
        {
            Event = LevelEvent.ParticlesDestroyBlock,
            Position = CenterOf(blockPosition),
            Data = block.NetworkId
        });

        Basalt.Core.Block.BlockPermutation air = Basalt.Core.Block.BlockType
            .GetOrAir("minecraft:air")
            .GetPermutation();

        Basalt.Core.Block.Block breakingBlock =
            player.Dimension.GetBlock(blockPosition.X, blockPosition.Y, blockPosition.Z) ??
            new Basalt.Core.Block.Block(block);

        breakingBlock.OnBreak(new BlockBreakDetails(player, blockPosition));

        player.Dimension.SetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z, air);

        Basalt.Core.Block.BlockPermutation after =
            player.Dimension.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        // Logger.Warn(
        //     "PlayerAuthInput destroy result player={0} pos={1},{2},{3} after={4} network={5}",
        //     player.Username,
        //     blockPosition.X,
        //     blockPosition.Y,
        //     blockPosition.Z,
        //     after.Type.Identifier,
        //     after.NetworkId);

        player.Dimension.Broadcast(new UpdateBlockPacket
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
                blockPosition,
                action.Face));
        }
    }

    private static void StopCrackBlock(global::Basalt.Core.Player.Player player, BlockPos blockPosition)
    {
        player.Dimension?.Broadcast(new LevelEventPacket
        {
            Event = LevelEvent.StopBlockCracking,
            Position = CenterOf(blockPosition),
            Data = 0
        });
    }

    private static int GetBreakTimeTicksForAnimation(global::Basalt.Core.Player.Player player, BlockPos blockPosition)
    {
        Basalt.Core.Block.BlockPermutation? block =
            player.Dimension?.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        if (block is null)
        {
            return 20;
        }

        float hardness = block.Type.Hardness;
        if (hardness < 0f)
        {
            return 9999;
        }

        if (hardness == 0f)
        {
            return 1;
        }

        return Math.Max(1, (int)(hardness * 1.5f * 20f));
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










