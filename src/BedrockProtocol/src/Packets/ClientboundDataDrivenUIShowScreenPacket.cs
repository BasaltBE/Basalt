using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(333)]
public sealed class ClientboundDataDrivenUIShowScreenPacket : DataPacket {
    public string ScreenId = string.Empty;
    public uint FormId;
    public uint? DataInstanceId;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarString(ScreenId);
        writer.WriteUInt32(FormId, true);
        writer.WriteBool(DataInstanceId.HasValue);
        if (DataInstanceId is uint dataInstanceId) writer.WriteUInt32(dataInstanceId, true);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ScreenId = reader.ReadVarString();
        FormId = reader.ReadUInt32(true);
        DataInstanceId = reader.ReadBool() ? reader.ReadUInt32(true) : null;
    }
}
