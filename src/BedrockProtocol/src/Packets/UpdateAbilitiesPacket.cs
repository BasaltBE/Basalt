using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(187)]
public sealed class UpdateAbilitiesPacket : DataPacket {
    public SerializedAbilitiesData Data = new();

    public override void Serialize(ref BinaryWriter writer) => Data.Write(ref writer);
    public override void Deserialize(ref BinaryReader reader) => Data.Read(ref reader);
}
