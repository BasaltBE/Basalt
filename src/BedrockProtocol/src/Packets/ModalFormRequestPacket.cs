using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(100)]
public sealed class ModalFormRequestPacket : DataPacket {
    public uint FormId;
    public string FormUiJson = string.Empty;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt(FormId);
        writer.WriteVarString(FormUiJson);
    }

    public override void Deserialize(ref BinaryReader reader) {
        FormId = reader.ReadVarUInt();
        FormUiJson = reader.ReadVarString();
    }
}
