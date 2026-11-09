using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(187)]
public sealed class UpdateAbilitiesPacket : DataPacket {
    public SerializedAbilitiesData Data = new();

    public override void Deserialize(ref BinaryReader reader) => Data.Read(ref reader);
    public override void Serialize(ref BinaryWriter writer) => Data.Write(ref writer);
}
