using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ClientboundDataDrivenUIShowScreen)]
public sealed record ClientboundDataDrivenUIShowScreenPacket : DataPacket {
    public string ScreenId = string.Empty;
    public uint FormId;
    public uint? DataInstanceId;

    public override void Deserialize(Binary.BinaryReader reader) {
        ScreenId = reader.ReadVarString();
        FormId = reader.ReadUInt32(true);
        DataInstanceId = reader.ReadBool() ? reader.ReadUInt32(true) : null;
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteVarString(ScreenId);
        writer.WriteUInt32(FormId, true);
        writer.WriteBool(DataInstanceId.HasValue);
        if (DataInstanceId.HasValue) {
            writer.WriteUInt32(DataInstanceId.Value, true);
        }
    }
}
