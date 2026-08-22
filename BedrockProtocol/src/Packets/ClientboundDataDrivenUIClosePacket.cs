using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(334)]
public sealed class ClientboundDataDrivenUIClosePacket : DataPacket {
    public uint? FormId;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteBool(FormId.HasValue);
        if (FormId is uint formId) writer.WriteUInt32(formId, true);
    }

    public override void Deserialize(ref BinaryReader reader) => FormId = reader.ReadBool() ? reader.ReadUInt32(true) : null;
}
