using System.Numerics;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Enchantments;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.LootTables;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class PlayerAuthInputHandler {
    private const float MaximumCoordinate = 30_000_000;
    private const ulong BreakToleranceTicks = 5;
    private const uint MaximumBreakDurationTicks = 6000;
    [ThreadStatic]
    private static LevelEventPacket? blockBreakEvent;
    [ThreadStatic]
    private static UpdateBlockPacket? blockUpdate;
    [ThreadStatic]
    private static LevelSoundEventPacket? blockBreakSound;
    [ThreadStatic]
    private static MovePlayerPacket? movementUpdate;
    [ThreadStatic]
    private static CorrectPlayerMovePredictionPacket? movementCorrection;

    public void Handle(NethernetPeer peer, Player? player, PlayerAuthInputPacket packet) {
        if (player is null)
            throw new InvalidDataException("Player auth input arrived before player login.");
        using Lock.Scope tickSync = player.TickSync.EnterScope();
        if (player.Disposed || !player.Spawned || player.Dead)
            return;
        EntityMovementTrait? movement = player.GetTrait<EntityMovementTrait>();
        if (movement is null)
            return;

        if (!float.IsFinite(packet.Position.X) || !float.IsFinite(packet.Position.Y) ||
            !float.IsFinite(packet.Position.Z) || !float.IsFinite(packet.Rotation.X) ||
            !float.IsFinite(packet.Rotation.Y) || !float.IsFinite(packet.HeadRotation) ||
            MathF.Abs(packet.Position.X) > MaximumCoordinate ||
            MathF.Abs(packet.Position.Y) > MaximumCoordinate ||
            MathF.Abs(packet.Position.Z) > MaximumCoordinate) {
            return;
        }
        if (movement.InputTick is ulong previousTick && packet.ClientTick.Tick <= previousTick)
            return;

        Span<bool> inputs = stackalloc bool[(int)PlayerAuthInputData.InternalUpdate + 1];
        inputs.Clear();
        foreach (PlayerAuthInputData input in packet.InputData) {
            if ((uint)input >= (uint)inputs.Length)
                return;
            inputs[(int)input] = true;
        }

        movement.InputTick = packet.ClientTick.Tick;
        if (packet.InputMode is >= InputMode.Mouse and < InputMode.Count)
            player.InputMode = packet.InputMode;

        PlayerInventoryTrait? playerInventory = player.GetTrait<PlayerInventoryTrait>();
        if (inputs[(int)PlayerAuthInputData.StartUsingItem] &&
            playerInventory is { } foodInventory &&
            foodInventory.Inventory.GetItem(foodInventory.SelectedSlot) is { Type.FoodNutrition: > 0 } food)
            player.GetTrait<PlayerHungerTrait>()?.StartEating(foodInventory.SelectedSlot, food);

        if (inputs[(int)PlayerAuthInputData.PerformItemInteraction] && packet.ItemUseTransaction is { } interaction) {
            playerInventory?.QueueTransaction(
                interaction.ItemUseTransaction,
                interaction.LegacyRequestId,
                interaction.LegacySetItemSlots
            );
        }

        float deltaX = packet.Position.X - player.Position.X;
        float feetY = packet.Position.Y - player.HeadOffset;
        float deltaY = feetY - player.Position.Y;
        float deltaZ = packet.Position.Z - player.Position.Z;
        bool moved = deltaX != 0 || deltaY != 0 || deltaZ != 0;
        bool canceled = false;
        bool jumping = inputs[(int)PlayerAuthInputData.Jumping] || inputs[(int)PlayerAuthInputData.StartJumping];
        if (moved && player.Network.Server.Events.HasSubscribers<PlayerMoveEvent>()) {
            PlayerMoveEvent moving = new(player,
                new Vector3(player.Position.X, player.Position.Y, player.Position.Z),
                new Vector3(packet.Position.X, feetY, packet.Position.Z));
            player.Network.Server.Events.Emit(moving);
            canceled = moving.IsCanceled();
        }
        if (!canceled && jumping && !movement.Jumping && player.Network.Server.Events.HasSubscribers<PlayerJumpEvent>()) {
            PlayerJumpEvent jump = new(player);
            player.Network.Server.Events.Emit(jump);
            canceled = jump.IsCanceled();
        }
        if (player.Disposed)
            return;
        if (canceled) {
            CorrectPlayerMovePredictionPacket correction = movementCorrection ?? new();
            movementCorrection = null;
            try {
                correction.PredictionType = RewindType.Player;
                correction.Position = player.GetHeadPosition();
                correction.PositionDelta.X = movement.Motion.X;
                correction.PositionDelta.Y = movement.Motion.Y;
                correction.PositionDelta.Z = movement.Motion.Z;
                correction.Tick = packet.ClientTick.Tick;
                correction.OnGround = movement.VerticalCollision && movement.Motion.Y <= 0;
                player.SendPacket(correction);
            }
            finally {
                movementCorrection ??= correction;
            }
            return;
        }

        bool sneaking = player.Flags.Get(ActorFlag.Sneaking);
        if (inputs[(int)PlayerAuthInputData.StartSneaking] != inputs[(int)PlayerAuthInputData.StopSneaking])
            sneaking = inputs[(int)PlayerAuthInputData.StartSneaking];
        if (sneaking != player.Flags.Get(ActorFlag.Sneaking) && player.Network.Server.Events.HasSubscribers<PlayerSneakEvent>()) {
            PlayerSneakEvent sneak = new(player, sneaking);
            player.Network.Server.Events.Emit(sneak);
            if (player.Disposed)
                return;
            if (sneak.IsCanceled()) {
                sneaking = player.Flags.Get(ActorFlag.Sneaking);
                bool dirty = player.Flags.Dirty;
                try {
                    player.SendPacket(player.Flags.CreateUpdate(player.RuntimeId, packet.ClientTick.Tick));
                }
                finally {
                    player.Flags.Dirty |= dirty;
                }
            }
        }
        player.SetFlag(ActorFlag.Sneaking, sneaking);
        if (inputs[(int)PlayerAuthInputData.StartSprinting] != inputs[(int)PlayerAuthInputData.StopSprinting])
            player.SetFlag(ActorFlag.Sprinting, inputs[(int)PlayerAuthInputData.StartSprinting]);
        if (inputs[(int)PlayerAuthInputData.StartSwimming] || inputs[(int)PlayerAuthInputData.StopSwimming])
            player.SetFlag(ActorFlag.Swimming, !inputs[(int)PlayerAuthInputData.StopSwimming]);
        if (inputs[(int)PlayerAuthInputData.StartGliding] || inputs[(int)PlayerAuthInputData.StopGliding])
            player.SetFlag(ActorFlag.Gliding, !inputs[(int)PlayerAuthInputData.StopGliding]);
        if (inputs[(int)PlayerAuthInputData.StartCrawling] || inputs[(int)PlayerAuthInputData.StopCrawling])
            player.SetFlag(ActorFlag.Crawling, !inputs[(int)PlayerAuthInputData.StopCrawling]);
        player.SetFlag(ActorFlag.Emoting, inputs[(int)PlayerAuthInputData.Emoting]);
        if (!player.Abilities.Get(PlayerAbility.MayFly) || inputs[(int)PlayerAuthInputData.StopFlying])
            player.Abilities.Set(PlayerAbility.Flying, false);
        else if (inputs[(int)PlayerAuthInputData.StartFlying])
            player.Abilities.Set(PlayerAbility.Flying, true);

        movement.Jumping = jumping;
        movement.HorizontalCollision = inputs[(int)PlayerAuthInputData.HorizontalCollision];
        movement.VerticalCollision = inputs[(int)PlayerAuthInputData.VerticalCollision];

        float pitch = Math.Clamp(packet.Rotation.X, -90, 90);
        float yaw = packet.Rotation.Y % 360;
        float headYaw = packet.HeadRotation % 360;
        movement.Motion.X = deltaX;
        movement.Motion.Y = deltaY;
        movement.Motion.Z = deltaZ;
        player.Position.X = packet.Position.X;
        player.Position.Y = feetY;
        player.Position.Z = packet.Position.Z;
        player.Rotation.X = pitch;
        player.Rotation.Y = yaw;
        player.HeadRotation = headYaw;
        player.SetFlag(ActorFlag.Moving, moved);

        float broadcastX = player.Position.X - movement.BroadcastPosition.X;
        float broadcastY = player.Position.Y - movement.BroadcastPosition.Y;
        float broadcastZ = player.Position.Z - movement.BroadcastPosition.Z;
        bool broadcastPosition = broadcastX * broadcastX + broadcastY * broadcastY + broadcastZ * broadcastZ >=
            EntityMovementTrait.PositionBroadcastThreshold * EntityMovementTrait.PositionBroadcastThreshold;
        bool broadcastRotation = MathF.Abs(pitch - movement.BroadcastRotation.X) >= EntityMovementTrait.RotationBroadcastThreshold ||
            MathF.Abs(MathF.IEEERemainder(yaw - movement.BroadcastRotation.Y, 360)) >= EntityMovementTrait.RotationBroadcastThreshold ||
            MathF.Abs(MathF.IEEERemainder(headYaw - movement.BroadcastHeadRotation, 360)) >= EntityMovementTrait.RotationBroadcastThreshold;
        bool onGround = movement.VerticalCollision && movement.Motion.Y <= 0;
        if (broadcastPosition || broadcastRotation || onGround != movement.BroadcastOnGround) {
            MovePlayerPacket update = movementUpdate ?? new();
            movementUpdate = null;
            try {
                update.PlayerRuntimeId = player.RuntimeId;
                update.Position = player.GetHeadPosition();
                update.Rotation.X = player.Rotation.X;
                update.Rotation.Y = player.Rotation.Y;
                update.HeadRotation = headYaw;
                update.OnGround = onGround;
                update.Tick = packet.ClientTick.Tick;
                player.Dimension.Broadcast(update, player, exclude: player);
                movement.BroadcastPosition = new Vector3(player.Position.X, player.Position.Y, player.Position.Z);
                movement.BroadcastRotation = new Vector2(pitch, yaw);
                movement.BroadcastHeadRotation = headYaw;
                movement.BroadcastOnGround = onGround;
            }
            finally {
                movementUpdate ??= update;
            }
        }
        if (moved)
            player.OnMove();

        int? mineBlockNetId = null;
        if (packet.ItemStackRequest is { } itemStackRequest &&
            player.GetTrait<PlayerInventoryTrait>() is { } inventory) {
            foreach (ItemStackRequestActionData action in itemStackRequest.Actions) {
                if (action is MineBlockActionData mineBlock && (uint)mineBlock.Slot < inventory.Inventory.ContainerSize) {
                    mineBlockNetId = inventory.Inventory.GetItem(mineBlock.Slot)?.NetId ?? 0;
                    break;
                }
            }
        }

        bool blockDestroyed = false;
        if (packet.PlayerBlockActions is { Length: > 0 } blockActions) {
            foreach (PlayerBlockActionData action in blockActions)
                HandleBlockAction(player, action, packet.ClientTick.Tick, ref blockDestroyed);
        }

        if (inputs[(int)PlayerAuthInputData.PerformItemStackRequest] && packet.ItemStackRequest is { } request) {
            ItemStackResponseInfo response = ItemStackRequestHandler.ProcessRequest(
                player,
                request,
                mineBlockNetId,
                blockDestroyed,
                out string? failureReason
            );
            if (response.Result != ItemStackNetResult.Success)
                Logger.Scoped("Network").Warn(
                    $"Rejected item stack request {request.ClientRequestId} from {player.Username}: {response.Result}" +
                    (failureReason is null ? "." : $" ({failureReason}).")
                );
            player.SendPacket(new ItemStackResponsePacket {
                Responses = [response]
            });
        }
    }

    private static void HandleBlockAction(Player player, PlayerBlockActionData action, ulong tick, ref bool blockDestroyed) {
        switch (action.PlayerActionType) {
            case PlayerActionType.StartDestroyBlock:
            case PlayerActionType.CrackBlock:
                StartBlockBreak(player, action.Position, tick);
                break;
            case PlayerActionType.ContinueDestroyBlock:
                if (player.BreakingBlock is null || !SameBlock(player.BreakingBlock, action.Position))
                    StartBlockBreak(player, action.Position, tick);
                else if (player.BlockBreakDurationTicks > 1) {
                    LevelEventPacket crack = blockBreakEvent ?? new();
                    blockBreakEvent = null;
                    try {
                        crack.Position.X = action.Position.X + 0.5f;
                        crack.Position.Y = action.Position.Y + 0.5f;
                        crack.Position.Z = action.Position.Z + 0.5f;
                        crack.EventId = (int)LevelEvent.UpdateBlockCracking;
                        crack.Data = (int)Math.Max(1U, ushort.MaxValue / player.BlockBreakDurationTicks);
                        player.Dimension.Broadcast(crack, crack.Position);
                    }
                    finally {
                        blockBreakEvent ??= crack;
                    }
                }
                break;
            case PlayerActionType.AbortDestroyBlock:
            case PlayerActionType.StopDestroyBlock:
                StopBlockBreak(player);
                break;
            case PlayerActionType.PredictDestroyBlock:
                PredictBlockBreak(player, action.Position, tick, ref blockDestroyed);
                break;
            case PlayerActionType.CreativeDestroyBlock:
                if (player.Abilities.Get(PlayerAbility.InstantBuild)) {
                    StopBlockBreak(player);
                    blockDestroyed |= DestroyBlock(player, action.Position, dropItems: false);
                }
                break;
        }
    }

    private static void StartBlockBreak(Player player, BlockPos position, ulong tick) {
        bool canMine = player.Abilities.Get(PlayerAbility.Mine);
        bool validTarget = ValidBlockTarget(player, position);
        if (!canMine || !validTarget) {
            StopBlockBreak(player);
            return;
        }

        if (player.BreakingBlock is { } current && SameBlock(current, position))
            return;
        StopBlockBreak(player);

        BlockPermutation block = player.Dimension.GetPermutation(position.X, position.Y, position.Z);
        BlockType? type = BlockPalette.FindType(block.Identifier);
        if (type is null || type.Hardness < 0)
            return;

        ItemStack? tool = player.GetTrait<PlayerInventoryTrait>() is { } inventory
            ? inventory.Inventory.GetItem(inventory.SelectedSlot)
            : null;
        bool canHarvest = player.Network.Server.LootTables.CanHarvest(block.Identifier, tool);
        float miningSpeed = tool is not null && (type.ToolTypes & tool.Type.ToolType) != 0
            ? tool.Type.MiningSpeed
            : 1;
        if (miningSpeed > 1 && tool is not null) {
            int efficiency = tool.GetEnchantmentLevel(EnchantmentPalette.Efficiency);
            miningSpeed += efficiency * efficiency + 1;
        }
        uint duration = BreakDurationTicks(type.Hardness, miningSpeed, canHarvest);
        if (duration == 0)
            return;

        player.BreakingBlock = new BlockPos { X = position.X, Y = position.Y, Z = position.Z };
        player.BlockBreakStartTick = tick;
        player.BlockBreakDurationTicks = duration;
        if (duration <= 1)
            return;

        LevelEventPacket crack = blockBreakEvent ?? new();
        blockBreakEvent = null;
        try {
            crack.Position.X = position.X + 0.5f;
            crack.Position.Y = position.Y + 0.5f;
            crack.Position.Z = position.Z + 0.5f;
            crack.EventId = (int)LevelEvent.StartBlockCracking;
            crack.Data = unchecked((int)player.RuntimeId);
            player.Dimension.Broadcast(crack, crack.Position);

            crack.EventId = (int)LevelEvent.UpdateBlockCracking;
            crack.Data = (int)Math.Max(1U, ushort.MaxValue / player.BlockBreakDurationTicks);
            player.Dimension.Broadcast(crack, crack.Position);
        }
        finally {
            blockBreakEvent ??= crack;
        }
    }

    private static void PredictBlockBreak(Player player, BlockPos position, ulong tick, ref bool blockDestroyed) {
        BlockPos target = position.X == 0 && position.Y == 0 && position.Z == 0 && player.BreakingBlock is { } active
            ? active
            : position;
        if (!ValidBlockTarget(player, target)) {
            StopBlockBreak(player);
            return;
        }
        if (!player.Abilities.Get(PlayerAbility.Mine)) {
            StopBlockBreak(player);
            SendBlockUpdate(player, target.X, target.Y, target.Z);
            return;
        }
        if (player.Abilities.Get(PlayerAbility.InstantBuild)) {
            StopBlockBreak(player);
            blockDestroyed |= DestroyBlock(player, target, dropItems: false);
            return;
        }

        bool valid = player.BreakingBlock is { } breaking && SameBlock(breaking, target) &&
            tick >= player.BlockBreakStartTick;
        if (valid) {
            ulong elapsed = tick - player.BlockBreakStartTick;
            uint duration = player.BlockBreakDurationTicks;
            valid = duration <= 1 || elapsed >= duration || duration - elapsed <= BreakToleranceTicks;
        }

        if (valid) {
            StopBlockBreak(player);
            blockDestroyed |= DestroyBlock(player, target);
        }
        else {
            StopBlockBreak(player);
            SendBlockUpdate(player, target.X, target.Y, target.Z);
        }
    }

    private static bool DestroyBlock(Player player, BlockPos position, bool dropItems = true) {
        if (!ValidBlockTarget(player, position)) {
            SendBlockUpdate(player, position.X, position.Y, position.Z);
            return false;
        }

        BlockPermutation block = player.Dimension.GetPermutation(position.X, position.Y, position.Z);
        if (block == BlockStorage.Air) {
            SendBlockUpdate(player, position.X, position.Y, position.Z);
            return false;
        }

        bool upperDoor = BlockPalette.IsDoor(block.Identifier) &&
            block.State.Values.GetValueOrDefault("upper_block_bit") is true;

        ItemStack? tool = player.GetTrait<PlayerInventoryTrait>() is { } inventory
            ? inventory.Inventory.GetItem(inventory.SelectedSlot)
            : null;
        int durabilityDamage = dropItems ? tool?.Type.BlockDurabilityDamage ?? 0 : 0;
        if (dropItems && player.Network.Server.Events.HasSubscribers<PlayerItemUseEvent>()) {
            PlayerItemUseEvent itemUse = new(player, tool, PlayerItemUseAction.BreakBlock, durabilityDamage);
            player.Network.Server.Events.Emit(itemUse);
            durabilityDamage = itemUse.DurabilityDamage;
            if (itemUse.IsCanceled()) {
                tool?.DamageDurability(durabilityDamage);
                SendBlockUpdate(player, position.X, position.Y, position.Z);
                return false;
            }
        }

        int x = position.X;
        int y = position.Y;
        int z = position.Z;
        Chunk chunk = player.Dimension.GetChunk(x >> 4, z >> 4)!;
        BlockActor? actor = player.Dimension.GetBlockActor(chunk, x & 15, y, z & 15);
        ItemStack? actorDrop = null;
        if (actor is not null) {
            if (!actor.CanRemove()) {
                SendBlockUpdate(player, x, y, z);
                return false;
            }
            actor.OnBreak(player);
            if (dropItems || actor.DropsInCreative)
                actorDrop = actor.CreateDrop();
        }
        player.Dimension.SetPermutation(x, y, z, BlockStorage.Air);

        LevelEventPacket particles = blockBreakEvent ?? new();
        blockBreakEvent = null;
        try {
            particles.EventId = (int)LevelEvent.ParticlesDestroyBlock;
            particles.Position.X = x + 0.5f;
            particles.Position.Y = y + 0.5f;
            particles.Position.Z = z + 0.5f;
            particles.Data = block.Hash;
            player.Dimension.Broadcast(particles, particles.Position);
        }
        finally {
            blockBreakEvent ??= particles;
        }

        UpdateBlockPacket update = blockUpdate ?? new();
        blockUpdate = null;
        try {
            update.Position.X = x;
            update.Position.Y = y;
            update.Position.Z = z;
            update.BlockRuntimeId = unchecked((uint)BlockStorage.Air.Hash);
            update.Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network;
            update.Layer = 0;

            LevelSoundEventPacket sound = blockBreakSound ?? new();
            blockBreakSound = null;
            try {
                sound.SoundEvent = "break";
                sound.Position.X = x + 0.5f;
                sound.Position.Y = y + 0.5f;
                sound.Position.Z = z + 0.5f;
                sound.Data = block.NetworkId;
                sound.ActorUniqueId = -1;
                player.Dimension.Broadcast(update, x >> 4, z >> 4, additionalPacket: sound);
            }
            finally {
                blockBreakSound ??= sound;
            }
        }
        finally {
            blockUpdate ??= update;
        }

        player.Dimension.UpdateNeighbors(x, y, z);

        if (actorDrop is not null) {
            ItemEntity item = new(actorDrop, player, x, y, z);
            item.Spawn(player.Dimension);
        }
        else if (dropItems && !upperDoor) {
            int fortune = tool?.GetEnchantmentLevel(EnchantmentPalette.Fortune) ?? 0;
            bool silkTouch = tool?.GetEnchantmentLevel(EnchantmentPalette.SilkTouch) > 0;
            ReadOnlySpan<BlockDrop> drops = player.Network.Server.LootTables.Get(block, tool, fortune, silkTouch);
            foreach (BlockDrop drop in drops) {
                ItemType? itemType = ItemPalette.Find(drop.Identifier);
                if (itemType is null || itemType.NetworkId == 0 || itemType.MaxAmount is <= 0 or > ushort.MaxValue)
                    continue;

                int amount = drop.MinAmount == drop.MaxAmount
                    ? drop.MinAmount
                    : (int)Random.Shared.NextInt64(drop.MinAmount, (long)drop.MaxAmount + 1);
                while (amount > 0) {
                    ushort stackSize = (ushort)Math.Min(itemType.MaxAmount, amount);
                    ItemEntity item = new(new ItemStack(itemType, stackSize), player, x, y, z);
                    item.Spawn(player.Dimension);
                    amount -= stackSize;
                }
            }
        }

        if (dropItems && !upperDoor && (tool?.GetEnchantmentLevel(EnchantmentPalette.SilkTouch) ?? 0) == 0 &&
            player.Network.Server.LootTables.CanHarvest(block.Identifier, tool)) {
            int experience = player.Network.Server.ExperienceDrops.GetBlockExperience(block.Identifier);
            player.Network.Server.ExperienceDrops.Drop(
                player.Dimension,
                new Vec3 { X = x + 0.5f, Y = y + 0.5f, Z = z + 0.5f },
                experience
            );
        }

        tool?.DamageDurability(durabilityDamage);
        return true;
    }

    private static void StopBlockBreak(Player player) {
        if (player.BreakingBlock is not { } position)
            return;

        if (player.BlockBreakDurationTicks > 1) {
            LevelEventPacket stop = blockBreakEvent ?? new();
            blockBreakEvent = null;
            try {
                stop.EventId = (int)LevelEvent.StopBlockCracking;
                stop.Position.X = position.X + 0.5f;
                stop.Position.Y = position.Y + 0.5f;
                stop.Position.Z = position.Z + 0.5f;
                stop.Data = unchecked((int)player.RuntimeId);
                player.Dimension.Broadcast(stop, stop.Position);
            }
            finally {
                blockBreakEvent ??= stop;
            }
        }

        player.BreakingBlock = null;
        player.BlockBreakStartTick = 0;
        player.BlockBreakDurationTicks = 0;
    }

    private static bool ValidBlockTarget(Player player, BlockPos position) {
        if (position.Y < player.Dimension.MinimumY || position.Y > player.Dimension.MaximumY ||
            player.Dimension.GetChunk(position.X >> 4, position.Z >> 4) is null)
            return false;

        float deltaX = player.Position.X - position.X - 0.5f;
        float deltaY = player.Position.Y + player.HeadOffset - position.Y - 0.5f;
        float deltaZ = player.Position.Z - position.Z - 0.5f;
        return deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ <= 36;
    }

    private static bool SameBlock(BlockPos left, BlockPos right) =>
        left.X == right.X && left.Y == right.Y && left.Z == right.Z;

    internal static uint BreakDurationTicks(float hardness, float miningSpeed, bool canHarvest) {
        if (!float.IsFinite(hardness) || hardness < 0)
            return 0;
        if (hardness == 0)
            return 1;
        if (!float.IsFinite(miningSpeed) || miningSpeed <= 0)
            miningSpeed = 1;
        float divisor = canHarvest ? 30f : 100f;
        if (hardness >= MaximumBreakDurationTicks * miningSpeed / divisor)
            return MaximumBreakDurationTicks;
        return Math.Max(1U, (uint)MathF.Ceiling(hardness * divisor / miningSpeed));
    }

    private static void SendBlockUpdate(Player player, int x, int y, int z) {
        if (y < player.Dimension.MinimumY || y > player.Dimension.MaximumY ||
            player.Dimension.GetChunk(x >> 4, z >> 4) is not { } chunk)
            return;

        UpdateBlockPacket update = blockUpdate ?? new();
        blockUpdate = null;
        try {
            update.Position.X = x;
            update.Position.Y = y;
            update.Position.Z = z;
            update.BlockRuntimeId = unchecked((uint)chunk.GetPermutation(x & 15, y, z & 15).Hash);
            update.Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network;
            update.Layer = 0;
            try {
                player.SendPacket(update);
            }
            catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
            }
        }
        finally {
            blockUpdate ??= update;
        }
    }
}
