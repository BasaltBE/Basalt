using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(106)]
public sealed class RemoveObjectivePacket : DataPacket {
    public string ObjectiveName = string.Empty;

    public override void Serialize(ref BinaryWriter writer) => writer.WriteVarString(ObjectiveName);
    public override void Deserialize(ref BinaryReader reader) => ObjectiveName = reader.ReadVarString();
}
