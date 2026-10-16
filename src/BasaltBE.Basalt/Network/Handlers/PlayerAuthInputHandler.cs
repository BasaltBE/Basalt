using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Events.Players;
using System.Numerics;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class PlayerAuthInputHandler
{
    private const float MaximumCoordinate = 30_000_000;
    [ThreadStatic]
    private static MovePlayerPacket? movementUpdate;
    [ThreadStatic]
    private static CorrectPlayerMovePredictionPacket? movementCorrection;

    public void Handle(NethernetPeer peer, Player? player, PlayerAuthInputPacket packet)
    {
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
            MathF.Abs(packet.Position.Z) > MaximumCoordinate)
        {
            return;
        }
        if (movement.InputTick is ulong previousTick && packet.ClientTick.Tick <= previousTick)
            return;

        Span<bool> inputs = stackalloc bool[(int)PlayerAuthInputData.InternalUpdate + 1];
        inputs.Clear();
        foreach (PlayerAuthInputData input in packet.InputData)
        {
            if ((uint)input >= (uint)inputs.Length)
                return;
            inputs[(int)input] = true;
        }

        movement.InputTick = packet.ClientTick.Tick;
        if (inputs[(int)PlayerAuthInputData.PerformItemInteraction] && packet.ItemUseTransaction is { } interaction)
            player.GetTrait<PlayerInventoryTrait>()?.QueueTransaction(interaction.ItemUseTransaction, interaction.LegacyRequestId);

        float deltaX = packet.Position.X - player.Position.X;
        float feetY = packet.Position.Y - player.HeadOffset;
        float deltaY = feetY - player.Position.Y;
        float deltaZ = packet.Position.Z - player.Position.Z;
        bool moved = deltaX != 0 || deltaY != 0 || deltaZ != 0;
        bool rejected = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ > 1;
        bool jumping = inputs[(int)PlayerAuthInputData.Jumping] || inputs[(int)PlayerAuthInputData.StartJumping];
        if (!rejected && moved && player.Network.Server.Events.HasSubscribers<PlayerMoveEvent>())
        {
            PlayerMoveEvent moving = new(player,
                new Vector3(player.Position.X, player.Position.Y, player.Position.Z),
                new Vector3(packet.Position.X, feetY, packet.Position.Z));
            player.Network.Server.Events.Emit(moving);
            rejected = moving.IsCanceled();
        }
        if (!rejected && jumping && !movement.Jumping && player.Network.Server.Events.HasSubscribers<PlayerJumpEvent>())
        {
            PlayerJumpEvent jump = new(player);
            player.Network.Server.Events.Emit(jump);
            rejected = jump.IsCanceled();
        }
        if (player.Disposed)
            return;
        if (rejected)
        {
            CorrectPlayerMovePredictionPacket correction = movementCorrection ?? new();
            movementCorrection = null;
            try
            {
                correction.Position = player.GetHeadPosition();
                correction.Rotation.X = player.Rotation.X;
                correction.Rotation.Y = player.Rotation.Y;
                correction.Tick = packet.ClientTick.Tick;
                correction.OnGround = movement.VerticalCollision && movement.Motion.Y <= 0;
                player.Network.SendPacket(player, correction);
            }
            finally
            {
                movementCorrection ??= correction;
            }
            return;
        }

        bool sneaking = player.Flags.Get(ActorFlag.Sneaking);
        if (inputs[(int)PlayerAuthInputData.StartSneaking] != inputs[(int)PlayerAuthInputData.StopSneaking])
            sneaking = inputs[(int)PlayerAuthInputData.StartSneaking];
        if (sneaking != player.Flags.Get(ActorFlag.Sneaking) && player.Network.Server.Events.HasSubscribers<PlayerSneakEvent>())
        {
            PlayerSneakEvent sneak = new(player, sneaking);
            player.Network.Server.Events.Emit(sneak);
            if (player.Disposed)
                return;
            if (sneak.IsCanceled())
            {
                sneaking = player.Flags.Get(ActorFlag.Sneaking);
                bool dirty = player.Flags.Dirty;
                try
                {
                    player.Network.SendPacket(player, player.Flags.CreateUpdate(player.RuntimeId, packet.ClientTick.Tick));
                }
                finally
                {
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
        if (broadcastPosition || broadcastRotation || onGround != movement.BroadcastOnGround)
        {
            MovePlayerPacket update = movementUpdate ?? new();
            movementUpdate = null;
            try
            {
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
            finally
            {
                movementUpdate ??= update;
            }
        }
        if (moved)
            player.OnMove();

        if (inputs[(int)PlayerAuthInputData.PerformItemStackRequest] && packet.ItemStackRequest is { } request)
        {
            ItemStackResponseInfo response = ItemStackRequestHandler.ProcessRequest(player, request);
            player.Network.SendPacket(player, new ItemStackResponsePacket
            {
                Responses = [response]
            });
        }
    }
}
