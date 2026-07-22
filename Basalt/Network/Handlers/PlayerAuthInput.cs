namespace Basalt.Core.Network.Handlers;

using System.Collections.Concurrent;
using Basalt.Core;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Events;
using Basalt.Core.Item;
using Basalt.Core.Item.Traits;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Player.Traits;
using Basalt.Core.Profiling;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;


public static class PlayerAuthInput {
    private const float MaxHorizontalMovePerTick = 2.0f;
    private const ulong TeleportGraceTicks = 20;
    private const ulong DefaultFoodUseTicks = 32UL;
    private const ulong BreakToleranceTicks = 5;

    private static readonly ConcurrentDictionary<ulong, ulong> LastInputTickByRuntimeId = new();
    private static readonly ConcurrentDictionary<ulong, PendingItemUse> PendingItemUses = new();
    private static readonly ConcurrentDictionary<ulong, ulong> LastEatSoundTick = new();
    private static readonly ConcurrentDictionary<ulong, BreakState> BreakStates = new();

    private readonly record struct PendingItemUse(int Slot, int StackNetworkId, ulong FinishTick);
    private readonly record struct BreakState(BlockPos Position, ulong StartTick, uint DurationTicks);

    /// <summary>
    /// Removes all tracked state for a player. Call on disconnect.
    /// </summary>
    public static void Cleanup(ulong runtimeId) {
        LastInputTickByRuntimeId.TryRemove(runtimeId, out _);
        PendingItemUses.TryRemove(runtimeId, out _);
        LastEatSoundTick.TryRemove(runtimeId, out _);
        BreakStates.TryRemove(runtimeId, out _);
    }

    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer) {
        using var __zone = Profiler.BeginZone("PlayerAuthInput.Handle");
        PlayerAuthInputPacket packet = new();
        try {
            int offset = 0;
            Binary.BinaryReader reader = new(packetBuffer, ref offset);
            packet = (PlayerAuthInputPacket)Protocol.Io.Packet.Deserialize(reader);
        }
        catch (Exception exception) {
            Logger.Error("PlayerAuthInput deserialize failed: {0}", exception);
            return;
        }

        try {
            if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
                return;
            }

            if (MovedTooFar(player, packet, out ulong tickDelta)) {
                Logger.Warn($"Player {player.Username} moved too fast ({packet.Position.X}, {packet.Position.Y}, {packet.Position.Z}) tickDelta:{tickDelta}");

                server.Network.SendPacket(connection, new CorrectPlayerMovePredictionPacket {
                    PredictionType = PredictionType.Player,
                    Position = player.Location,
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
            TickPendingItemUse(player, packet.Tick);

            if (packet.InputData.HasFlag(PlayerAuthInputFlag.PerformItemInteraction)) {
                InventoryTransaction.HandleUseItemFromAuthInput(
                    player,
                    packet.ItemInteractionData,
                    packet.InteractPitch,
                    packet.InteractYaw);
            }

            MineBlockStackRequestAction? mineBlockRequest = null;
            if (packet.InputData.HasFlag(PlayerAuthInputFlag.PerformItemStackRequest)) {
                mineBlockRequest = GetMineBlockRequest(packet.ItemStackRequest);
                Logger.Debug(
                    "PlayerAuthInput item stack request player:{0} request:{1} actions:{2} mineBlock:{3}",
                    player.Username,
                    packet.ItemStackRequest.RequestId,
                    packet.ItemStackRequest.Actions.Count,
                    mineBlockRequest is not null);

                server.Network.SendPacket(connection, new ItemStackResponsePacket {
                    Responses = [ProcessItemStackRequest(player, packet.ItemStackRequest)]
                });
            }

            if (packet.InputData.HasFlag(PlayerAuthInputFlag.PerformBlockActions)) {
                // Logger.Warn(
                //     "PlayerAuthInput block actions player:{0} count:{1} tick:{2}",
                //     player.Username,
                //     packet.BlockActions.Count,
                //     packet.Tick);

                foreach (PlayerBlockAction action in packet.BlockActions) {
                    HandleBlockAction(player, action, packet.Tick);
                }
            }

            if (packet.InputData.HasFlag(PlayerAuthInputFlag.StartUsingItem)) {
                StartUsingItem(player);
            }
            else if (PendingItemUses.ContainsKey(player.RuntimeId)) {
                PendingItemUses.TryRemove(player.RuntimeId, out _);
                LastEatSoundTick.TryRemove(player.RuntimeId, out _);
                player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
            }

            if (packet.InputData.HasFlag(PlayerAuthInputFlag.StartSprinting)) {
                player.IsSprinting = true;
            }

            else if (packet.InputData.HasFlag(PlayerAuthInputFlag.StopSprinting)) {
                player.IsSprinting = false;
            }

            if (packet.InputData.HasFlag(PlayerAuthInputFlag.StartSneaking)) {
                player.IsSneaking = true;
            }

            else if (packet.InputData.HasFlag(PlayerAuthInputFlag.StopSneaking)) {
                player.IsSneaking = false;
            }

            LastInputTickByRuntimeId[player.RuntimeId] = packet.Tick;
        }
        catch (Exception exception) {
            Logger.Warn("PlayerAuthInput handler failed: {0}", exception);
        }
    }

    private static void StartUsingItem(Player.Player player) {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.GetHeldItem();
        ItemStackFoodTrait? food = heldItem?.GetTrait<ItemStackFoodTrait>();
        if (inventory is null || heldItem is null || food is null) {
            PendingItemUses.TryRemove(player.RuntimeId, out _);
            LastEatSoundTick.TryRemove(player.RuntimeId, out _);
            player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
            return;
        }

        PlayerHungerTrait? hunger = player.GetTrait<PlayerHungerTrait>();
        if (hunger is null || (!food.CanAlwaysEat && hunger.CurrentValue >= hunger.MaximumValue)) {
            PendingItemUses.TryRemove(player.RuntimeId, out _);
            LastEatSoundTick.TryRemove(player.RuntimeId, out _);
            player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
            return;
        }

        if (PendingItemUses.ContainsKey(player.RuntimeId)) {
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

    private static void TickPendingItemUse(Player.Player player, ulong clientTick) {
        if (!PendingItemUses.TryGetValue(player.RuntimeId, out PendingItemUse pending)) {
            return;
        }

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.Container.GetItem(pending.Slot);
        if (inventory is null || heldItem is null || heldItem.NetworkStackId != pending.StackNetworkId) {
            PendingItemUses.TryRemove(player.RuntimeId, out _);
            LastEatSoundTick.TryRemove(player.RuntimeId, out _);
            player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
            return;
        }

        ulong currentTick = GetCurrentTick(player);
        if (currentTick < pending.FinishTick) {
            LastEatSoundTick.TryGetValue(player.RuntimeId, out ulong lastSound);
            if (clientTick - lastSound >= 4) {
                player.Dimension?.Broadcast(new LevelSoundEventPacket {
                    Event = LevelSoundEvent.Eat,
                    Position = player.Position,
                    Data = 0,
                    ActorIdentifier = EntityIdentifier.Player.ToIdentifierString(),
                    BabyMob = false,
                    DisableRelativeVolume = false,
                    UniqueActorId = 0,
                    FireAtPosition = new Optional<Vec3f> { HasValue = false, Value = default }
                });
                LastEatSoundTick[player.RuntimeId] = clientTick;
            }
            return;
        }

        LastEatSoundTick.TryRemove(player.RuntimeId, out _);

        PendingItemUses.TryRemove(player.RuntimeId, out _);
        player.Flags.SetActorFlag(ActorFlag.UsingItem, false);

        ItemStackFoodTrait? food = heldItem.GetTrait<ItemStackFoodTrait>();
        PlayerHungerTrait? hunger = player.GetTrait<PlayerHungerTrait>();
        if (food is null || hunger is null || !hunger.Eat(food.Nutrition, food.SaturationModifier, food.CanAlwaysEat)) {
            return;
        }

        player.Dimension?.Broadcast(new LevelSoundEventPacket {
            Event = LevelSoundEvent.Burp,
            Position = player.Position,
            Data = 0,
            ActorIdentifier = string.Empty,
            BabyMob = false,
            DisableRelativeVolume = false,
            UniqueActorId = 0,
            FireAtPosition = new Optional<Vec3f> { HasValue = false, Value = default }
        });

        // Update inventory and notify the client.
        heldItem.DecrementStack();
        if (heldItem.StackSize == 0) {
            inventory.Container.ClearSlot(pending.Slot);
        }
        else {
            inventory.Container.UpdateSlot(pending.Slot);
        }

        if (!string.IsNullOrWhiteSpace(food.UsingConvertsTo) && ItemType.Get(food.UsingConvertsTo) is ItemType convertedType) {
            ItemStack converted = new(convertedType);
            if (!inventory.Container.AddItem(converted)) {
                _ = player.DropItem(converted);
            }
        }

        player.Attributes.Send();
    }

    private static ulong GetCurrentTick(Player.Player player) {
        return player.Dimension?.World is Basalt.Core.Worlds.Tickable tickable ? tickable.TickValue : 0UL;
    }

    private static ulong GetUseDurationTicks(ItemStack item) {
        // Check raw components for minecraft:use_duration (stored as IntTag).
        Basalt.Protocol.Nbt.CompoundTag? components = item.Type.Properties.Get<Basalt.Protocol.Nbt.CompoundTag>("components");
        if (components?.Get<Basalt.Protocol.Nbt.IntTag>("minecraft:use_duration") is Basalt.Protocol.Nbt.IntTag durationTag) {
            return (ulong)Math.Max(1, durationTag.Value);
        }

        return DefaultFoodUseTicks;
    }

    private static ItemStackResponse ProcessItemStackRequest(Player.Player player, Protocol.Types.ItemStackRequest request) {
        // Check if this request contains only MineBlock actions 
        bool hasOtherActions = false;
        for (int i = 0; i < request.Actions.Count; i++) {
            if (request.Actions[i] is not MineBlockStackRequestAction
                and not EmptyStackRequestAction
                and not CraftResultsDeprecatedStackRequestAction) {
                hasOtherActions = true;
                break;
            }
        }

        if (hasOtherActions) {
            return ItemStackRequest.ProcessRequestFromAuthInput(player, request);
        }

        List<StackResponseContainerInfo> containers = [];

        for (int i = 0; i < request.Actions.Count; i++) {
            if (request.Actions[i] is not MineBlockStackRequestAction mineBlock) {
                continue;
            }

            EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
            ItemStack? item = inventory?.Container.GetItem(mineBlock.HotbarSlot);

            containers.Add(new StackResponseContainerInfo {
                Container = new FullContainerName { ContainerId = (byte)ContainerName.Inventory },
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

        return new ItemStackResponse {
            Status = ItemStackResponseStatus.Ok,
            RequestId = request.RequestId,
            ContainerInfo = containers
        };
    }

    private static MineBlockStackRequestAction? GetMineBlockRequest(Protocol.Types.ItemStackRequest request) {
        for (int i = 0; i < request.Actions.Count; i++) {
            if (request.Actions[i] is MineBlockStackRequestAction mineBlock) {
                return mineBlock;
            }
        }

        return null;
    }

    private static bool MovedTooFar(Player.Player player, PlayerAuthInputPacket packet, out ulong rawTickDelta) {
        float deltaX = packet.Position.X - player.Location.X;
        float deltaZ = packet.Position.Z - player.Location.Z;
        float movedDistanceSquared = deltaX * deltaX + deltaZ * deltaZ;

        ulong previousTick = LastInputTickByRuntimeId.GetOrAdd(player.RuntimeId, packet.Tick);
        rawTickDelta = packet.Tick > previousTick ? packet.Tick - previousTick : 1UL;

        if (packet.Tick <= player.LastTeleportTick + TeleportGraceTicks) {
            return false;
        }

        float tickDelta = Math.Clamp((float)rawTickDelta, 1f, 20f);
        float allowedDistance = MaxHorizontalMovePerTick * tickDelta;

        return movedDistanceSquared > allowedDistance * allowedDistance;
    }

    private static void MovePlayer(Player.Player player, PlayerAuthInputPacket packet) {
        Vec3f previousPosition = player.Location;

        MovementRotation fromRotation = new MovementRotation() {
            HeadYaw = player.HeadYaw,
            Pitch = player.Pitch,
            Yaw = player.Yaw,
        };

        MovementRotation toRotation = new MovementRotation() {
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

        player.Location = missingPosition && hasDelta
            ? new Vec3f {
                X = previousPosition.X + packet.Delta.X,
                Y = previousPosition.Y + packet.Delta.Y,
                Z = previousPosition.Z + packet.Delta.Z
            }
            : packet.Position;

        player.OnMove(new EntityMoveOptions(previousPosition, player.Location, fromRotation, toRotation));

    }

    private static void HandleBlockAction(Player.Player player, PlayerBlockAction action, ulong tick) {
        // Logger.Warn(
        //     "BlockAction player:{0} action:{1} pos:{2},{3},{4} face:{5} tick:{6}",
        //     player.Username,
        //     action.Action,
        //     action.BlockPos.X,
        //     action.BlockPos.Y,
        //     action.BlockPos.Z,
        //     action.Face,
        //     tick);

        switch (action.Action) {
            case PlayerActionType.StartDestroyBlock:
                StartBreakBlock(player, action.BlockPos, tick);
                break;

            case PlayerActionType.ContinueDestroyBlock:
                ContinueBreakBlock(player, action.BlockPos, tick);
                break;

            case PlayerActionType.CrackBlock:
                CrackBlock(player, action.BlockPos, tick);
                break;

            case PlayerActionType.AbortDestroyBlock:
                StopCrackBlock(player, player.BreakingBlock ?? action.BlockPos);
                BreakStates.TryRemove(player.RuntimeId, out _);
                player.BreakingBlock = null;
                break;

            case PlayerActionType.StopDestroyBlock:
                StopCrackBlock(player, player.BreakingBlock ?? action.BlockPos);
                BreakStates.TryRemove(player.RuntimeId, out _);
                player.BreakingBlock = null;
                break;

            case PlayerActionType.PredictDestroyBlock:
                ValidateAndDestroyBlock(player, action, tick);
                break;

            case PlayerActionType.CreativeDestroyBlock:
                DestroyBlock(player, action);
                break;
        }
    }

    private static void StartBreakBlock(Player.Player player, BlockPos blockPosition, ulong tick) {
        if (player.BreakingBlock.HasValue && !SameBlock(player.BreakingBlock.Value, blockPosition)) {
            StopCrackBlock(player, player.BreakingBlock.Value);
        }

        player.BreakingBlock = blockPosition;
        int breakTimeTicks = GetBreakTimeTicksForAnimation(player, blockPosition);

        // Logger.Warn(
        //     "StartBreak player:{0} pos:{1},{2},{3} duration:{4} tick:{5}",
        //     player.Username,
        //     blockPosition.X, blockPosition.Y, blockPosition.Z,
        //     breakTimeTicks, tick);

        BreakStates[player.RuntimeId] = new BreakState(blockPosition, tick, (uint)breakTimeTicks);

        int crackSpeed = breakTimeTicks > 0
            ? Math.Min(65535, 65535 / breakTimeTicks)
            : 65535;

        player.Dimension?.Broadcast(new LevelEventPacket {
            Event = LevelEvent.StartBlockCracking,
            Position = CenterOf(blockPosition),
            Data = Math.Max(1, crackSpeed)
        });
    }

    private static void CrackBlock(Player.Player player, BlockPos blockPosition, ulong tick) {
        if (!player.BreakingBlock.HasValue || !SameBlock(player.BreakingBlock.Value, blockPosition)) {
            StartBreakBlock(player, blockPosition, tick);
        }
    }

    private static void ContinueBreakBlock(Player.Player player, BlockPos blockPosition, ulong tick) {
        if (BreakStates.TryGetValue(player.RuntimeId, out BreakState existing)
            && SameBlock(existing.Position, blockPosition)) {
            return;
        }

        if (player.BreakingBlock.HasValue) {
            StopCrackBlock(player, player.BreakingBlock.Value);
        }

        StartBreakBlock(player, blockPosition, tick);
    }

    private static void ValidateAndDestroyBlock(Player.Player player, PlayerBlockAction action, ulong tick) {
        BlockPos blockPosition = IsZero(action.BlockPos)
            ? (player.BreakingBlock ?? action.BlockPos)
            : action.BlockPos;

        // Creative mode players break blocks instantly without timing validation.
        if (player.Gamemode == Gamemode.Creative) {
            BreakStates.TryRemove(player.RuntimeId, out _);
            player.BreakingBlock = null;
            StopCrackBlock(player, blockPosition);
            DestroyBlock(player, action);
            return;
        }

        bool valid = false;

        if (BreakStates.TryRemove(player.RuntimeId, out BreakState state)) {
            if (SameBlock(state.Position, blockPosition)) {
                ulong elapsed = tick >= state.StartTick ? tick - state.StartTick : 0;

                // Only instant-break blocks (1 tick) pass with zero elapsed time.
                if (state.DurationTicks <= 1) {
                    valid = true;
                }
                else if (elapsed > 0) {
                    valid = elapsed + BreakToleranceTicks >= state.DurationTicks;
                }

                if (!valid) {
                    // Logger.Warn(
                    //     "Block break rejected player:{0} pos:{1},{2},{3} elapsed:{4} duration:{5} tick:{6} startTick:{7}",
                    //     player.Username,
                    //     blockPosition.X, blockPosition.Y, blockPosition.Z,
                    //     elapsed, state.DurationTicks, tick, state.StartTick);
                }
            }
            else {
                Logger.Debug(
                    "Block break rejected player:{0} reason: position-mismatch state:{1},{2},{3} action:{4},{5},{6}",
                    player.Username,
                    state.Position.X, state.Position.Y, state.Position.Z,
                    blockPosition.X, blockPosition.Y, blockPosition.Z);
            }
        }
        else {
            Logger.Debug(
                "Block break rejected player:{0} reason: no-break-state pos:{1},{2},{3}",
                player.Username, blockPosition.X, blockPosition.Y, blockPosition.Z);
        }

        player.BreakingBlock = null;

        if (!valid) {
            StopCrackBlock(player, blockPosition);
            SendRevertBlock(player, blockPosition);
            return;
        }

        StopCrackBlock(player, blockPosition);
        DestroyBlock(player, action);
    }

    private static void SendRevertBlock(Player.Player player, BlockPos blockPosition) {
        if (player.Dimension is null) return;

        Basalt.Core.Blocks.BlockPermutation? perm =
            player.Dimension.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        if (perm is null) return;

        player.Send(new UpdateBlockPacket {
            Position = blockPosition,
            NetworkBlockId = (uint)perm.NetworkId,
            Flags = UpdateBlockFlagsType.Network,
            Layer = UpdateBlockLayerType.Normal
        });
    }

    private static void DestroyBlock(Player.Player player, PlayerBlockAction action) {
        if (IsZero(action.BlockPos) && !player.BreakingBlock.HasValue) {
            // Logger.Warn("PlayerAuthInput destroy skipped player:{0} reason=zero-position-no-target action:{1}", player.Username, action.Action);
            return;
        }

        BlockPos blockPosition = IsZero(action.BlockPos)
            ? player.BreakingBlock!.Value
            : action.BlockPos;

        StopCrackBlock(player, blockPosition);
        player.BreakingBlock = null;

        if (player.Dimension is null) {
            // Logger.Warn("PlayerAuthInput destroy skipped player:{0} reason=no-dimension", player.Username);
            return;
        }

        Basalt.Core.Blocks.BlockPermutation? block =
            player.Dimension.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        if (block is null) {
            // Logger.Warn(
            //     "PlayerAuthInput destroy skipped player:{0} reason=null-block pos:{1},{2},{3}",
            //     player.Username,
            //     blockPosition.X,
            //     blockPosition.Y,
            //     blockPosition.Z);
            return;
        }

        // Logger.Warn(
        //     "PlayerAuthInput destroy attempt player:{0} pos:{1},{2},{3} before:{4} network:{5} action:{6}",
        //     player.Username,
        //     blockPosition.X,
        //     blockPosition.Y,
        //     blockPosition.Z,
        //     block.Type.Identifier,
        //     block.NetworkId,
        //     action.Action);

        Server? server = player.Dimension.World?.Server;
        if (server is not null) {
            Basalt.Core.Blocks.Block breakBlock =
                player.Dimension.GetBlock(blockPosition.X, blockPosition.Y, blockPosition.Z) ??
                new Basalt.Core.Blocks.Block(block);

            EntityInventoryTrait? signalInventory = player.GetTrait<EntityInventoryTrait>();
            ItemStack? signalHeldItem = signalInventory?.GetHeldItem();

            PlayerBreakBlockSignal signal = new(player, blockPosition, action.Face, breakBlock, signalHeldItem);
            server.Emit(signal);
            if (!signal.Emit()) {
                player.Send(new UpdateBlockPacket {
                    Position = blockPosition,
                    NetworkBlockId = (uint)block.NetworkId,
                    Flags = UpdateBlockFlagsType.Network,
                    Layer = UpdateBlockLayerType.Normal
                });

                EntityInventoryTrait? cancelInventory = player.GetTrait<EntityInventoryTrait>();
                if (cancelInventory is not null) {
                    ItemStack? rollbackItem = cancelInventory.GetHeldItem();
                    if (rollbackItem is not null) {
                        cancelInventory.Container.SetItem(cancelInventory.SelectedSlot, rollbackItem.Clone());
                    }
                    cancelInventory.Container.UpdateSlot(cancelInventory.SelectedSlot);
                    cancelInventory.Container.Update();
                    cancelInventory.SyncToPlayer(player);
                }
                return;
            }
        }

        player.Dimension.Broadcast(new LevelEventPacket {
            Event = LevelEvent.ParticlesDestroyBlock,
            Position = CenterOf(blockPosition),
            Data = block.NetworkId
        });

        Basalt.Core.Blocks.BlockPermutation air = Basalt.Core.Blocks.BlockType
            .GetOrAir("minecraft:air")
            .GetPermutation();

        Basalt.Core.Blocks.Block breakingBlock =
            player.Dimension.GetBlock(blockPosition.X, blockPosition.Y, blockPosition.Z) ??
            new Basalt.Core.Blocks.Block(block);

        breakingBlock.OnBreak(new BlockBreakDetails(player, blockPosition));

        player.Dimension.SetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z, air);

        if (block.Type.Liquid) {
            Basalt.Core.Blocks.Traits.FluidKind? fluidKind = Basalt.Core.Blocks.Traits.FluidTrait.GetFluidKind(block);
            if (fluidKind.HasValue) {
                Basalt.Core.Blocks.Traits.FluidTrait.NotifyFluidNeighbors(fluidKind.Value, player.Dimension, blockPosition);
            }
        }
        else {
            Basalt.Core.Blocks.Traits.FluidTrait.NotifyFluidNeighbors(Basalt.Core.Blocks.Traits.FluidKind.Water, player.Dimension, blockPosition);
            Basalt.Core.Blocks.Traits.FluidTrait.NotifyFluidNeighbors(Basalt.Core.Blocks.Traits.FluidKind.Lava, player.Dimension, blockPosition);
        }

        Basalt.Core.Blocks.BlockPermutation after =
            player.Dimension.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        // Logger.Warn(
        //     "PlayerAuthInput destroy result player:{0} pos:{1},{2},{3} after:{4} network:{5}",
        //     player.Username,
        //     blockPosition.X,
        //     blockPosition.Y,
        //     blockPosition.Z,
        //     after.Type.Identifier,
        //     after.NetworkId);

        player.Dimension.Broadcast(new UpdateBlockPacket {
            Position = blockPosition,
            NetworkBlockId = (uint)air.NetworkId,
            Flags = UpdateBlockFlagsType.Network,
            Layer = UpdateBlockLayerType.Normal
        });

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.GetHeldItem();

        if (inventory is not null && heldItem is not null) {
            heldItem.OnBreakBlock(new ItemBreakBlockDetails(
                player,
                inventory.SelectedSlot,
                blockPosition,
                action.Face));
        }
    }

    private static void StopCrackBlock(Player.Player player, BlockPos blockPosition) {
        player.Dimension?.Broadcast(new LevelEventPacket {
            Event = LevelEvent.StopBlockCracking,
            Position = CenterOf(blockPosition),
            Data = 0
        });
    }

    private const float Tps = 20f;
    private const float CompatibleToolMultiplier = 1.5f;
    private const float IncompatibleToolMultiplier = 5.0f;
    private const int MaxBreakTicks = 6000;

    private static int GetBreakTimeTicksForAnimation(Player.Player player, BlockPos blockPosition) {
        Basalt.Core.Blocks.BlockPermutation? block =
            player.Dimension?.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        if (block is null) {
            return 20;
        }

        Basalt.Core.Blocks.BlockType blockType = block.Type;
        float hardness = blockType.Hardness;

        if (hardness < 0f) {
            return MaxBreakTicks;
        }

        if (hardness == 0f) {
            return 1;
        }

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.GetHeldItem();

        ToolCategory requiredCategory = GetBlockToolCategory(blockType);
        int requiredTierLevel = GetBlockRequiredTierLevel(blockType);

        bool categoryMatch = false;
        int toolTierLevel = 0;
        float efficiency = 1f;
        bool hasDiggerMatch = false;

        if (heldItem is not null) {
            ToolCategory itemCategory = GetItemToolCategory(heldItem.Type);
            toolTierLevel = GetItemTierHarvestLevel(heldItem.Type);
            categoryMatch = requiredCategory != ToolCategory.None && itemCategory == requiredCategory;

            // Custom items have digger component so we need to properly
            // calculate the break times
            float diggerSpeed = GetDiggerDestroySpeed(heldItem.Type, blockType, out bool useEfficiency);
            if (diggerSpeed > 0f) {
                hasDiggerMatch = true;
                efficiency = diggerSpeed;

                // Digger match implies tool category compatibility.
                if (!categoryMatch)
                    categoryMatch = true;

                // Digger-based items bypass tier requirements.
                if (toolTierLevel == 0)
                    toolTierLevel = requiredTierLevel;

                // Only apply efficiency enchantment if the digger enables it.
                if (useEfficiency) {
                    ItemStackEnchantmentTrait? enchantments = heldItem.GetTrait<ItemStackEnchantmentTrait>();
                    if (enchantments is not null) {
                        efficiency += enchantments.GetMiningSpeedBonus();
                    }
                }
            }
        }

        bool tierOk = requiredTierLevel == 0 || toolTierLevel >= requiredTierLevel;
        bool compatible = requiredTierLevel == 0 || (categoryMatch && tierOk);

        if (!hasDiggerMatch && categoryMatch) {
            efficiency = GetBaseMiningEfficiency(heldItem!.Type);

            ItemStackEnchantmentTrait? enchantments = heldItem.GetTrait<ItemStackEnchantmentTrait>();
            if (enchantments is not null) {
                efficiency += enchantments.GetMiningSpeedBonus();
            }
        }

        // hardness * 1.5 / speed * 20.
        if (hasDiggerMatch) {
            float seconds = (hardness * CompatibleToolMultiplier) / efficiency;
            int ticks = (int)MathF.Ceiling(seconds * Tps);
            return Math.Clamp(ticks, 1, MaxBreakTicks);
        }

        float multiplier = compatible ? CompatibleToolMultiplier : IncompatibleToolMultiplier;
        float tagSeconds = (hardness * multiplier) / efficiency;
        int tagTicks = (int)MathF.Ceiling(tagSeconds * Tps);
        return Math.Clamp(tagTicks, 1, MaxBreakTicks);
    }

    private static float GetDiggerDestroySpeed(ItemType itemType, Basalt.Core.Blocks.BlockType blockType) {
        return GetDiggerDestroySpeed(itemType, blockType, out _);
    }

    private static float GetDiggerDestroySpeed(ItemType itemType, Basalt.Core.Blocks.BlockType blockType, out bool useEfficiency) {
        useEfficiency = false;
        Item.Components.ItemTypeDiggerComponent? digger =
            itemType.Components.GetComponent<Item.Components.ItemTypeDiggerComponent>();

        if (digger is null) {
            return 0f;
        }

        useEfficiency = digger.UseEfficiency();

        Item.Components.DestroySpeedEntry[] speeds = digger.GetDestroySpeeds();
        for (int i = 0; i < speeds.Length; i++) {
            ref readonly Item.Components.DestroySpeedEntry entry = ref speeds[i];

            if (entry.Block is not null) {
                if (string.Equals(entry.Block, blockType.Identifier, StringComparison.Ordinal)) {
                    return entry.Speed;
                }
                continue;
            }

            if (entry.TagQuery is not null && MatchesTagQuery(entry.TagQuery, blockType)) {
                return entry.Speed;
            }
        }

        return 0f;
    }

    private static bool MatchesTagQuery(string tagQuery, Basalt.Core.Blocks.BlockType blockType) {
        // Parses "query.any_tag('tag1', 'tag2', ...)" format.
        ReadOnlySpan<char> query = tagQuery.AsSpan();
        int start = query.IndexOf('(');
        if (start < 0) {
            return false;
        }

        int end = query.LastIndexOf(')');
        if (end <= start) {
            return false;
        }

        ReadOnlySpan<char> args = query[(start + 1)..end];
        while (args.Length > 0) {
            int quoteStart = args.IndexOf('\'');
            if (quoteStart < 0) break;

            args = args[(quoteStart + 1)..];
            int quoteEnd = args.IndexOf('\'');
            if (quoteEnd < 0) break;

            string tag = args[..quoteEnd].ToString();
            if (blockType.HasTag(tag)) {
                return true;
            }

            args = args[(quoteEnd + 1)..];
        }

        return false;
    }

    private enum ToolCategory { None, Axe, Hoe, Pickaxe, Shovel, Sword }

    private static ToolCategory GetBlockToolCategory(Basalt.Core.Blocks.BlockType blockType) {
        if (blockType.HasTag("minecraft:is_pickaxe_item_destructible")) return ToolCategory.Pickaxe;
        if (blockType.HasTag("minecraft:is_axe_item_destructible")) return ToolCategory.Axe;
        if (blockType.HasTag("minecraft:is_shovel_item_destructible")) return ToolCategory.Shovel;
        if (blockType.HasTag("minecraft:is_hoe_item_destructible")) return ToolCategory.Hoe;
        if (blockType.HasTag("minecraft:is_sword_item_destructible")) return ToolCategory.Sword;
        return ToolCategory.None;
    }

    private static int GetBlockRequiredTierLevel(Basalt.Core.Blocks.BlockType blockType) {
        if (blockType.HasTag("minecraft:diamond_tier_destructible")) return 5;
        if (blockType.HasTag("minecraft:iron_tier_destructible")) return 4;
        if (blockType.HasTag("minecraft:stone_tier_destructible")) return 3;
        return 0;
    }

    private static ToolCategory GetItemToolCategory(ItemType itemType) {
        IReadOnlyList<string> tags = itemType.Tags;
        for (int i = 0; i < tags.Count; i++) {
            switch (tags[i]) {
                case "minecraft:is_pickaxe": return ToolCategory.Pickaxe;
                case "minecraft:is_axe": return ToolCategory.Axe;
                case "minecraft:is_shovel": return ToolCategory.Shovel;
                case "minecraft:is_hoe": return ToolCategory.Hoe;
                case "minecraft:is_sword": return ToolCategory.Sword;
            }
        }
        return ToolCategory.None;
    }

    private static int GetItemTierHarvestLevel(ItemType itemType) {
        IReadOnlyList<string> tags = itemType.Tags;
        for (int i = 0; i < tags.Count; i++) {
            switch (tags[i]) {
                case "minecraft:netherite_tier": return 6;
                case "minecraft:diamond_tier": return 5;
                case "minecraft:iron_tier": return 4;
                case "minecraft:stone_tier": return 3;
                case "minecraft:copper_tier": return 3;
                case "minecraft:golden_tier": return 2;
                case "minecraft:wooden_tier": return 1;
            }
        }
        return 0;
    }

    private static float GetBaseMiningEfficiency(ItemType itemType) {
        IReadOnlyList<string> tags = itemType.Tags;
        for (int i = 0; i < tags.Count; i++) {
            switch (tags[i]) {
                case "minecraft:netherite_tier": return 9f;
                case "minecraft:diamond_tier": return 8f;
                case "minecraft:iron_tier": return 6f;
                case "minecraft:copper_tier": return 5f;
                case "minecraft:stone_tier": return 4f;
                case "minecraft:golden_tier": return 12f;
                case "minecraft:wooden_tier": return 2f;
            }
        }
        return 1f;
    }

    private static Vec3f CenterOf(BlockPos position) {
        return new Vec3f {
            X = position.X + 0.5f,
            Y = position.Y + 0.5f,
            Z = position.Z + 0.5f
        };
    }

    private static bool SameBlock(BlockPos a, BlockPos b) {
        return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    }

    private static bool IsZero(BlockPos position) {
        return position.X == 0 && position.Y == 0 && position.Z == 0;
    }


}










