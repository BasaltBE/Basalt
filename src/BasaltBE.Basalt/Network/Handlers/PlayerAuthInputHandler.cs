using System.Numerics;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.LootTables;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
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
        if (player.Disposed || !player.Spawned)
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
        if (inputs[(int)PlayerAuthInputData.PerformItemInteraction] && packet.ItemUseTransaction is { } interaction)
            player.GetTrait<PlayerInventoryTrait>()?.QueueTransaction(interaction.ItemUseTransaction, interaction.LegacyRequestId);

        float deltaX = packet.Position.X - player.Position.X;
        float feetY = packet.Position.Y - player.HeadOffset;
        float deltaY = feetY - player.Position.Y;
        float deltaZ = packet.Position.Z - player.Position.Z;
        bool moved = deltaX != 0 || deltaY != 0 || deltaZ != 0;
        bool rejected = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ > 1;
        bool jumping = inputs[(int)PlayerAuthInputData.Jumping] || inputs[(int)PlayerAuthInputData.StartJumping];
        if (!rejected && moved && player.Network.Server.Events.HasSubscribers<PlayerMoveEvent>()) {
            PlayerMoveEvent moving = new(player,
                new Vector3(player.Position.X, player.Position.Y, player.Position.Z),
                new Vector3(packet.Position.X, feetY, packet.Position.Z));
            player.Network.Server.Events.Emit(moving);
            rejected = moving.IsCanceled();
        }
        if (!rejected && jumping && !movement.Jumping && player.Network.Server.Events.HasSubscribers<PlayerJumpEvent>()) {
            PlayerJumpEvent jump = new(player);
            player.Network.Server.Events.Emit(jump);
            rejected = jump.IsCanceled();
        }
        if (player.Disposed)
            return;
        if (rejected) {
            CorrectPlayerMovePredictionPacket correction = movementCorrection ?? new();
            movementCorrection = null;
            try {
                correction.Position = player.GetHeadPosition();
                correction.Rotation.X = player.Rotation.X;
                correction.Rotation.Y = player.Rotation.Y;
                correction.Tick = packet.ClientTick.Tick;
                correction.OnGround = movement.VerticalCollision && movement.Motion.Y <= 0;
                player.Network.SendPacket(player, correction);
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
                    player.Network.SendPacket(player, player.Flags.CreateUpdate(player.RuntimeId, packet.ClientTick.Tick));
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

        if (packet.PlayerBlockActions is { Length: > 0 } blockActions) {
            foreach (PlayerBlockActionData action in blockActions)
                HandleBlockAction(player, action, packet.ClientTick.Tick);
        }

        if (inputs[(int)PlayerAuthInputData.PerformItemStackRequest] && packet.ItemStackRequest is { } request) {
            ItemStackResponseInfo response = ItemStackRequestHandler.ProcessRequest(player, request);
            player.Network.SendPacket(player, new ItemStackResponsePacket {
                Responses = [response]
            });
        }
    }

    private static void HandleBlockAction(Player player, PlayerBlockActionData action, ulong tick) {
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
                PredictBlockBreak(player, action.Position, tick);
                break;
            case PlayerActionType.CreativeDestroyBlock:
                if (player.Abilities.Get(PlayerAbility.InstantBuild)) {
                    StopBlockBreak(player);
                    DestroyBlock(player, action.Position, dropItems: false);
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

        bool requiresTool = RequiresTool(type);
        uint duration = BreakDurationTicks(type.Hardness, requiresTool);
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

    private static void PredictBlockBreak(Player player, BlockPos position, ulong tick) {
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
            DestroyBlock(player, target, dropItems: false);
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
            DestroyBlock(player, target);
        }
        else {
            StopBlockBreak(player);
            SendBlockUpdate(player, target.X, target.Y, target.Z);
        }
    }

    private static void DestroyBlock(Player player, BlockPos position, bool dropItems = true) {
        if (!ValidBlockTarget(player, position)) {
            SendBlockUpdate(player, position.X, position.Y, position.Z);
            return;
        }

        BlockPermutation block = player.Dimension.GetPermutation(position.X, position.Y, position.Z);
        if (block == BlockStorage.Air) {
            SendBlockUpdate(player, position.X, position.Y, position.Z);
            return;
        }

        int x = position.X;
        int y = position.Y;
        int z = position.Z;
        Chunk chunk = player.Dimension.GetChunk(x >> 4, z >> 4)!;
        player.Dimension.RemoveBlockActor(x, y, z);
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
                sound.Data = block.Hash;
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

        if (dropItems) {
            ItemStack? tool = player.GetTrait<PlayerInventoryTrait>() is { } inventory
                ? inventory.Inventory.GetItem(inventory.SelectedSlot)
                : null;
            ReadOnlySpan<BlockDrop> drops = player.Network.Server.LootTables.Get(block.Identifier, tool);
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
        if (position.Y is < -64 or >= 320 || player.Dimension.GetChunk(position.X >> 4, position.Z >> 4) is null)
            return false;

        float deltaX = player.Position.X - position.X - 0.5f;
        float deltaY = player.Position.Y + player.HeadOffset - position.Y - 0.5f;
        float deltaZ = player.Position.Z - position.Z - 0.5f;
        return deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ <= 36;
    }

    private static bool SameBlock(BlockPos left, BlockPos right) =>
        left.X == right.X && left.Y == right.Y && left.Z == right.Z;

    internal static uint BreakDurationTicks(float hardness, bool requiresTool) {
        if (!float.IsFinite(hardness) || hardness < 0)
            return 0;
        if (hardness == 0)
            return 1;
        float multiplier = requiresTool ? 5f : 1.5f;
        if (hardness >= MaximumBreakDurationTicks / (multiplier * 20f))
            return MaximumBreakDurationTicks;
        return Math.Max(1U, (uint)MathF.Ceiling(hardness * multiplier * 20f));
    }

    private static bool RequiresTool(BlockType type) {
        foreach (string tag in type.Tags) {
            if (tag == "minecraft:is_pickaxe_item_destructible" ||
                tag.EndsWith("_tier_destructible", StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static void SendBlockUpdate(Player player, int x, int y, int z) {
        if (y is < -64 or >= 320 || player.Dimension.GetChunk(x >> 4, z >> 4) is not { } chunk)
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
                player.Network.SendPacket(player, update);
            }
            catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
            }
        }
        finally {
            blockUpdate ??= update;
        }
    }
}
