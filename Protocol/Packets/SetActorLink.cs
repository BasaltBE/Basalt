using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.SetActorLink)]
public sealed record SetActorLinkPacket : DataPacket {
    /// <summary>
    /// Link payload for the actor relationship.
    /// </summary>
    public EntityLink EntityLink = new();

    public override void Deserialize(Binary.BinaryReader reader) {
        EntityLink.Read(reader);
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        EntityLink.Write(writer);
    }
}
