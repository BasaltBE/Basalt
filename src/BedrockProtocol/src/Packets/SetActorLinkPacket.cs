using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(41)]
public sealed class SetActorLinkPacket : DataPacket {
    public ActorLink Link = new();

    public override void Serialize(ref BinaryWriter writer) => Link.Write(ref writer);
    public override void Deserialize(ref BinaryReader reader) => Link.Read(ref reader);
}
