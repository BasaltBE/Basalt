using Basalt.Core;
using Basalt.Entity.Traits.Attribute;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class SetLocalPlayerAsInitialized
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        SetLocalPlayerAsInitializedPacket packet = new();
        packet.Deserialize(packetBuffer);

        if (!server.Players.TryGetValue(connection, out Player? player))
        {
            Logger.Warn("SetLocalPlayerAsInitialized received for unknown player session.");
            return;
        }

        SetActorDataPacket actorData = new()
        {
            RuntimeId = player.RuntimeId,
            Tick = player.Dimension?.World?.CurrentTick ?? 0,
            Metadata = []
        };

        actorData.Metadata.Add(new ActorMetadataItem
        {
            Id = ActorDataId.Reserved0,
            Type = ActorDataType.Long,
            Value = player.Flags.Lower64()
        });

        actorData.Metadata.Add(new ActorMetadataItem
        {
            Id = ActorDataId.Reserved092,
            Type = ActorDataType.Long,
            Value = player.Flags.Upper64()
        });

        EntityHealthTrait? health = player.GetTrait<EntityHealthTrait>();
        if (health is not null)
        {
            UpdateAttributesPacket attributes = new()
            {
                RuntimeId = player.RuntimeId,
                Tick = player.Dimension?.World?.CurrentTick ?? 0,
                Attributes =
                [
                    health.GetAttribute()
                ]
            };

            server.Network.SendPacket(connection, attributes);
        }

        server.Network.SendPacket(connection, actorData);

        Logger.Info($"Player {player.Username} has spawned.");
    }
}
