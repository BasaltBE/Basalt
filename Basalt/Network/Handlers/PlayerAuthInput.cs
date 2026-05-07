using Basalt.Core;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class PlayerAuthInput
{
    private const float MaxHorizontalDeltaPerTick = 2.0f;
    private const float MaxHorizontalDeltaPerTickSquared = MaxHorizontalDeltaPerTick * MaxHorizontalDeltaPerTick;

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

        if (horizontalDistanceSquared > MaxHorizontalDeltaPerTickSquared)
        {
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
            return;
        }

        player.Position = packet.Position;
    }
}
