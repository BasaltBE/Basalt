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
        LastInputTickByRuntimeId[player.RuntimeId] = packet.Tick;
    }
}
