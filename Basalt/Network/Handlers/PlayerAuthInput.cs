namespace Basalt.Core.Network.Handlers;

using System.Collections.Concurrent;
using Basalt.Core;
using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Enums;
using Basalt.Core.Events;
using Basalt.Core.Item;
using Basalt.Core.Item.Traits;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Player.Traits;
using Basalt.Core.Profiling;

using BedrockProtocol.Enums;
using BedrockProtocol.Nbt;
using BedrockProtocol.Packets;
using BedrockProtocol.Types;

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

    public static void CancelPendingItemUse(Player.Player player) {
        if (!PendingItemUses.TryRemove(player.RuntimeId, out _)) {
            return;
        }

        LastEatSoundTick.TryRemove(player.RuntimeId, out _);
        player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
    }

    public static void Handle(Server server, NetworkConnection connection, PlayerAuthInputPacket packet) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("PlayerAuthInput.Handle") : default;

        try {
            if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
                return;
            }

            player.Grounded = packet.InputData?.Contains(PlayerAuthInputData.VerticalCollision) == true;

            if (!player.InitialAttributesSynced) {
                Logger.Info($"First PlayerAuthInput: unique={player.UniqueId}, runtime={player.RuntimeId}, clientTick={packet.ClientTick.InputTick}");
                player.Attributes.Send();
                player.InitialAttributesSynced = true;
            }

            if (MovedTooFar(player, packet, out ulong tickDelta)) {
                Logger.Warn($"Player {player.Username} moved too fast ({packet.Position.X}, {packet.Position.Y}, {packet.Position.Z}) tickDelta:{tickDelta}");

                server.Network.QueuePacket(connection, new CorrectPlayerMovePredictionPacket {
                    PredictionType = RewindType.Player,
                    Pos = player.Location,
                    PosDelta = new Vec3 { X = 0f, Y = 0f, Z = 0f },
                    Rotation = packet.PlayerRotation,
                    VehicleAngularVelocity = null,
                    OnGround = packet.InputData?.Contains(PlayerAuthInputData.VerticalCollision) == true,
                    Tick = packet.ClientTick
                });

                LastInputTickByRuntimeId[player.RuntimeId] = packet.ClientTick.InputTick;
                return;
            }

            MovePlayer(player, packet);

            if (packet.InputData?.Contains(PlayerAuthInputData.StartUsingItem) == true) {
                StartUsingItem(player);
            }

            TickPendingItemUse(player, packet.ClientTick.InputTick);

            if (packet.InputData?.Contains(PlayerAuthInputData.PerformItemInteraction) == true
                && packet.ItemUseTransaction is { } itemUseTransaction) {
                EntityInventoryTrait? interactionInventory = player.GetTrait<EntityInventoryTrait>();
                if (itemUseTransaction.ItemUseTransaction.ActionType == ItemUseActionType.Use
                    && interactionInventory?.GetHeldItem()?.GetTrait<ItemStackFoodTrait>() is not null) {
                    StartUsingItem(player);
                }

                InventoryTransaction.HandleUseItemFromAuthInput(
                    player,
                    itemUseTransaction,
                    packet.PlayerRotation.X,
                    packet.PlayerRotation.Y,
                    packet.CameraOrientation
                );
            }

            ItemStackRequestMineBlockAction? mineBlockRequest = null;
            if (packet.InputData?.Contains(PlayerAuthInputData.PerformItemStackRequest) == true
                && packet.ItemStackRequest is { } itemStackRequest) {
                mineBlockRequest = GetMineBlockRequest(itemStackRequest);
                Logger.Debug(
                    "PlayerAuthInput item stack request player:{0} request:{1} actions:{2} mineBlock:{3}",
                    player.Username,
                    itemStackRequest.ClientRequestId,
                    itemStackRequest.Actions.Count,
                    mineBlockRequest is not null);

                server.Network.QueuePacket(connection, new ItemStackResponsePacket {
                    Responses = [ProcessItemStackRequest(player, itemStackRequest)]
                });
            }

            if (packet.InputData?.Contains(PlayerAuthInputData.PerformBlockActions) == true
                && packet.PlayerBlockActions is { } blockActions) {
                // Logger.Warn(
                //     "PlayerAuthInput block actions player:{0} count:{1} tick:{2}",
                //     player.Username,
                //     blockActions.Count,
                //     packet.ClientTick.InputTick);

                foreach (PlayerBlockActionData action in blockActions) {
                    HandleBlockAction(player, action, packet.ClientTick.InputTick);
                }
            }

            if (packet.InputData?.Contains(PlayerAuthInputData.StartSprinting) == true) {
                player.IsSprinting = true;
            }

            else if (packet.InputData?.Contains(PlayerAuthInputData.StopSprinting) == true) {
                player.IsSprinting = false;
            }

            if (packet.InputData?.Contains(PlayerAuthInputData.StartSneaking) == true) {
                player.IsSneaking = true;
            }

            else if (packet.InputData?.Contains(PlayerAuthInputData.StopSneaking) == true) {
                player.IsSneaking = false;
            }

            if (packet.InputData?.Contains(PlayerAuthInputData.StartSwimming) == true) {
                player.IsSwimming = true;
                player.Flags.SetActorFlag(ActorFlag.Swimming, true);
            }
            else if (packet.InputData?.Contains(PlayerAuthInputData.StopSwimming) == true) {
                player.IsSwimming = false;
                player.Flags.SetActorFlag(ActorFlag.Swimming, false);
            }

            if (packet.InputData?.Contains(PlayerAuthInputData.StartCrawling) == true) {
                player.Flags.SetActorFlag(ActorFlag.Crawling, true);
            }
            else if (packet.InputData?.Contains(PlayerAuthInputData.StopCrawling) == true) {
                player.Flags.SetActorFlag(ActorFlag.Crawling, false);
            }

            LastInputTickByRuntimeId[player.RuntimeId] = packet.ClientTick.InputTick;
        }
        catch (Exception exception) {
            Logger.Warn("PlayerAuthInput handler failed: {0}", exception);
        }
    }

    private static void StartUsingItem(Player.Player player) {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.GetHeldItem();
        ItemStackFoodTrait? food = heldItem?.GetTrait<ItemStackFoodTrait>();
        PlayerHungerTrait? hunger = player.GetTrait<PlayerHungerTrait>();
        if (inventory is null || heldItem is null || food is null) {
            PendingItemUses.TryRemove(player.RuntimeId, out _);
            LastEatSoundTick.TryRemove(player.RuntimeId, out _);
            player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
            return;
        }

        if (player.Dimension?.World?.Server is Server server) {
            PlayerUseItemSignal signal = new(player, heldItem);
            server.Emit(signal);
            if (!signal.Emit()) {
                PendingItemUses.TryRemove(player.RuntimeId, out _);
                LastEatSoundTick.TryRemove(player.RuntimeId, out _);
                player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
                return;
            }
        }

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
                player.Dimension?.PlaySound(
                    LevelSoundEvent.eat.ToString(),
                    player.Position,
                    actorIdentifier: EntityIdentifier.Player.ToIdentifierString());
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

        player.Dimension?.PlaySound(LevelSoundEvent.burp.ToString(), player.Position);

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
        CompoundTag? components = item.Type.Properties.Get<CompoundTag>("components");
        if (components?.Get<IntTag>("minecraft:use_duration") is IntTag durationTag) {
            return (ulong)Math.Max(1, durationTag.Value);
        }

        return DefaultFoodUseTicks;
    }

    private static ItemStackResponseInfo ProcessItemStackRequest(Player.Player player, BedrockProtocol.Types.ItemStackRequest request) {
        // Check if this request contains only MineBlock actions 
        bool hasOtherActions = false;
        for (int i = 0; i < request.Actions.Count; i++) {
            if (request.Actions[i] is not ItemStackRequestMineBlockAction
                and not ItemStackRequestCraftResultsDeprecatedAction) {
                hasOtherActions = true;
                break;
            }
        }

        if (hasOtherActions) {
            return ItemStackRequest.ProcessRequestFromAuthInput(player, request);
        }

        List<ItemStackResponseContainerInfo> containers = [];

        for (int i = 0; i < request.Actions.Count; i++) {
            if (request.Actions[i] is not ItemStackRequestMineBlockAction mineBlock) {
                continue;
            }

            EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
            ItemStack? item = inventory?.Container.GetItem(mineBlock.Slot);

            int durability = 0;
            if (item?.GetTrait<Basalt.Core.Item.Traits.ItemStackDurabilityTrait>() is { } durabilityTrait) {
                durability = durabilityTrait.GetCurrentDamage();
            }

            containers.Add(new ItemStackResponseContainerInfo {
                FullContainerName = new FullContainerName {
                    // ContainerId = (byte)ContainerName.Inventory
                    ContainerName = ContainerEnumName.InventoryContainer,
                    DynamicID = 0,
                },
                Slots =
                [
                    new ItemStackResponseSlotInfo
                    {
                        RequestedSlot = (byte)mineBlock.Slot,
                        Slot = (byte)mineBlock.Slot,
                        Amount = (byte)(item?.StackSize ?? 0),
                        // ItemStackNetId = item?.NetworkStackId ?? 0,
                        CustomName = new RedactableString() {
                            Unredacted =  string.Empty
                        },
                        DurabilityCorrection = durability
                    }
                ]
            });
        }

        return new ItemStackResponseInfo {
            Result = ItemStackNetResult.Success,
            ClientRequestId = request.ClientRequestId,
            Containers = containers
        };
    }

    private static ItemStackRequestMineBlockAction? GetMineBlockRequest(BedrockProtocol.Types.ItemStackRequest request) {
        for (int i = 0; i < request.Actions.Count; i++) {
            if (request.Actions[i] is ItemStackRequestMineBlockAction mineBlock) {
                return mineBlock;
            }
        }

        return null;
    }

    private static bool MovedTooFar(Player.Player player, PlayerAuthInputPacket packet, out ulong rawTickDelta) {
        bool missingPosition =
            packet.Position.X == 0f &&
            packet.Position.Y == 0f &&
            packet.Position.Z == 0f;

        bool hasDelta =
            packet.PosDelta.X != 0f ||
            packet.PosDelta.Y != 0f ||
            packet.PosDelta.Z != 0f;

        float positionX = missingPosition && hasDelta
            ? player.Location.X + packet.PosDelta.X
            : missingPosition
                ? player.Location.X
                : packet.Position.X;
        float positionZ = missingPosition && hasDelta
            ? player.Location.Z + packet.PosDelta.Z
            : missingPosition
                ? player.Location.Z
                : packet.Position.Z;

        float deltaX = positionX - player.Location.X;
        float deltaZ = positionZ - player.Location.Z;
        float movedDistanceSquared = deltaX * deltaX + deltaZ * deltaZ;

        ulong previousTick = LastInputTickByRuntimeId.GetOrAdd(player.RuntimeId, packet.ClientTick.InputTick);
        rawTickDelta = packet.ClientTick.InputTick > previousTick ? packet.ClientTick.InputTick - previousTick : 1UL;

        if (packet.ClientTick.InputTick <= player.LastTeleportTick + TeleportGraceTicks) {
            return false;
        }

        float tickDelta = Math.Clamp((float)rawTickDelta, 1f, 20f);
        float allowedDistance = MaxHorizontalMovePerTick * tickDelta;

        return movedDistanceSquared > allowedDistance * allowedDistance;
    }

    private static void MovePlayer(Player.Player player, PlayerAuthInputPacket packet) {
        Vec3 previousPosition = player.Location;

        MovementRotation fromRotation = new MovementRotation() {
            HeadYaw = player.HeadYaw,
            Pitch = player.Pitch,
            Yaw = player.Yaw,
        };

        MovementRotation toRotation = new MovementRotation() {
            HeadYaw = packet.PlayerHeadRotation,
            Pitch = packet.PlayerRotation.X,
            Yaw = packet.PlayerRotation.Y,
        };

        player.Pitch = packet.PlayerRotation.X;
        player.Yaw = packet.PlayerRotation.Y;
        player.HeadYaw = packet.PlayerHeadRotation;

        bool missingPosition =
            packet.Position.X == 0f &&
            packet.Position.Y == 0f &&
            packet.Position.Z == 0f;

        bool hasDelta =
            packet.PosDelta.X != 0f ||
            packet.PosDelta.Y != 0f ||
            packet.PosDelta.Z != 0f;

        player.Location = missingPosition && hasDelta
            ? new Vec3 {
                X = previousPosition.X + packet.PosDelta.X,
                Y = previousPosition.Y + packet.PosDelta.Y,
                Z = previousPosition.Z + packet.PosDelta.Z
            }
            : packet.Position;

        if (previousPosition.X == player.Location.X &&
            previousPosition.Y == player.Location.Y &&
            previousPosition.Z == player.Location.Z &&
            fromRotation.Pitch == toRotation.Pitch &&
            fromRotation.Yaw == toRotation.Yaw &&
            fromRotation.HeadYaw == toRotation.HeadYaw) {
            return;
        }

        player.OnMove(new EntityMoveOptions(previousPosition, player.Location, fromRotation, toRotation));

    }

    private static void HandleBlockAction(Player.Player player, PlayerBlockActionData action, ulong tick) {
        // Logger.Warn(
        //     "BlockAction player:{0} action:{1} pos:{2},{3},{4} face:{5} tick:{6}",
        //     player.Username,
        //     action.PlayerActionType,
        //     action.Position.X,
        //     action.Position.Y,
        //     action.Position.Z,
        //     action.Facing,
        //     tick);

        switch (action.PlayerActionType) {
            case PlayerActionType.StartDestroyBlock:
                StartBreakBlock(player, action.Position, tick);
                break;

            case PlayerActionType.ContinueDestroyBlock:
                ContinueBreakBlock(player, action.Position, tick);
                break;

            case PlayerActionType.CrackBlock:
                CrackBlock(player, action.Position, tick);
                break;

            case PlayerActionType.AbortDestroyBlock:
                StopCrackBlock(player, player.BreakingBlock ?? action.Position);
                BreakStates.TryRemove(player.RuntimeId, out _);
                player.BreakingBlock = null;
                break;

            case PlayerActionType.StopDestroyBlock:
                StopCrackBlock(player, player.BreakingBlock ?? action.Position);
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
        if (player.Dimension?.World?.Server is Server server) {
            PlayerStartBreakBlockSignal signal = new(player, blockPosition);
            server.Emit(signal);
            if (!signal.Emit()) {
                player.Send(new UpdateBlockPacket {
                    BlockPosition = blockPosition,
                    BlockRuntimeID = (uint)player.Dimension.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z).NetworkId,
                    Flags = (uint)UpdateBlockFlagsType.Network,
                    Layer = (uint)UpdateBlockLayerType.Normal
                });
                return;
            }
        }

        if (player.BreakingBlock is not null && !SameBlock(player.BreakingBlock, blockPosition)) {
            StopCrackBlock(player, player.BreakingBlock);
        }

        player.BreakingBlock = blockPosition;
        int breakTimeTicks = GetBreakTimeTicksForAnimation(player, blockPosition);

        BlockPermutation? block = player.Dimension?.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        // Logger.Warn(
        //     "StartBreak player:{0} pos:{1},{2},{3} duration:{4} tick:{5}",
        //     player.Username,
        //     blockPosition.X, blockPosition.Y, blockPosition.Z,
        //     breakTimeTicks, tick);

        BreakStates[player.RuntimeId] = new BreakState(blockPosition, tick, (uint)breakTimeTicks);

        int crackSpeed = breakTimeTicks > 0
            ? Math.Min(65535, 65535 / breakTimeTicks)
            : 65535;

        if (block?.Type.Hardness > 0f) {
            player.Dimension?.Broadcast(new LevelEventPacket {
                EventId = (int)LevelEvent.StartBlockCracking,
                Position = CenterOf(blockPosition),
                Data = Math.Max(1, crackSpeed)
            });
        }
    }

    private static void CrackBlock(Player.Player player, BlockPos blockPosition, ulong tick) {
        if (player.BreakingBlock is null || !SameBlock(player.BreakingBlock, blockPosition)) {
            StartBreakBlock(player, blockPosition, tick);
        }
    }

    private static void ContinueBreakBlock(Player.Player player, BlockPos blockPosition, ulong tick) {
        if (BreakStates.TryGetValue(player.RuntimeId, out BreakState existing)
            && SameBlock(existing.Position, blockPosition)) {
            return;
        }

        if (player.BreakingBlock is not null) {
            StopCrackBlock(player, player.BreakingBlock);
        }

        StartBreakBlock(player, blockPosition, tick);
    }

    private static void ValidateAndDestroyBlock(Player.Player player, PlayerBlockActionData action, ulong tick) {
        BlockPos blockPosition = IsZero(action.Position)
            ? (player.BreakingBlock ?? action.Position)
            : action.Position;

        // Creative mode players break blocks instantly without timing validation.
        if (player.Gamemode == GameType.Creative) {
            BreakStates.TryRemove(player.RuntimeId, out _);
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

        if (!valid) {
            player.BreakingBlock = null;
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
            BlockPosition = blockPosition,
            BlockRuntimeID = (uint)perm.NetworkId,
            Flags = (uint)UpdateBlockFlagsType.Network,
            Layer = (uint)UpdateBlockLayerType.Normal
        });
    }

    private static void DestroyBlock(Player.Player player, PlayerBlockActionData action) {
        if (IsZero(action.Position) && player.BreakingBlock is null) {
            // Logger.Warn("PlayerAuthInput destroy skipped player:{0} reason=zero-position-no-target action:{1}", player.Username, action.PlayerActionType);
            return;
        }
        if (player.BreakingBlock is null) return;

        BlockPos blockPosition = IsZero(action.Position)
            ? player.BreakingBlock
            : action.Position;

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
        //     action.PlayerActionType);

        Server? server = player.Dimension.World?.Server;
        BlockPermutation? replacement = null;
        List<ItemStack>? customDrops = null;
        if (server is not null) {
            Block breakBlock =
                player.Dimension.GetBlock(blockPosition.X, blockPosition.Y, blockPosition.Z) ??
                new Block(block);

            EntityInventoryTrait? signalInventory = player.GetTrait<EntityInventoryTrait>();
            ItemStack? signalHeldItem = signalInventory?.GetHeldItem();

            PlayerBreakBlockSignal signal = new(player, blockPosition, action.Facing, breakBlock, signalHeldItem);
            server.Emit(signal);
            if (!signal.Emit()) {
                player.Send(new UpdateBlockPacket {
                    BlockPosition = blockPosition,
                    BlockRuntimeID = (uint)block.NetworkId,
                    Flags = (int)UpdateBlockFlagsType.Network,
                    Layer = (int)UpdateBlockLayerType.Normal
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

            replacement = signal.Replacement;
            customDrops = signal.Drops;
        }

        if (block.Type.Hardness > 0f) {
            player.Dimension.Broadcast(new LevelEventPacket {
                EventId = (int)LevelEvent.ParticlesDestroyBlock,
                Position = CenterOf(blockPosition),
                Data = block.NetworkId
            });
        }

        Basalt.Core.Blocks.BlockPermutation air = Basalt.Core.Blocks.BlockType
            .GetOrAir("minecraft:air")
            .GetPermutation();

        Basalt.Core.Blocks.Block breakingBlock =
            player.Dimension.GetBlock(blockPosition.X, blockPosition.Y, blockPosition.Z) ??
            new Basalt.Core.Blocks.Block(block);

        if (customDrops is not null) {
            breakingBlock.SetDrops(customDrops);
        }

        breakingBlock.OnBreak(new BlockBreakDetails(player, blockPosition));

        player.Dimension.SetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z, air);

        if (replacement is not null) {
            player.Dimension.SetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z, replacement);
        }

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
            BlockPosition = blockPosition,
            BlockRuntimeID = (uint)air.NetworkId,
            Flags = (uint)UpdateBlockFlagsType.Network,
            Layer = (uint)UpdateBlockLayerType.Normal
        });

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.GetHeldItem();

        if (inventory is not null && heldItem is not null) {
            heldItem.OnBreakBlock(new ItemBreakBlockDetails(
                player,
                inventory.SelectedSlot,
                blockPosition,
                action.Facing));
        }
    }

    private static void StopCrackBlock(Player.Player player, BlockPos blockPosition) {
        BlockPermutation? block = player.Dimension?.GetPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);
        if (block?.Type.Hardness <= 0f) {
            return;
        }

        player.Dimension?.Broadcast(new LevelEventPacket {
            EventId = (int)LevelEvent.StopBlockCracking,
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

    private static Vec3 CenterOf(BlockPos position) {
        return new Vec3 {
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
