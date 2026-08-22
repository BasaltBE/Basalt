using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(101)]
public sealed class ModalFormResponsePacket : DataPacket {
    public uint FormId;
    public string? JsonResponse;
    public ModalFormCancelReason? FormCancelReason;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt(FormId);
        writer.WriteBool(JsonResponse is not null);
        if (JsonResponse is string jsonResponse) writer.WriteVarString(jsonResponse);
        writer.WriteBool(FormCancelReason is not null);
        if (FormCancelReason is ModalFormCancelReason formCancelReason) writer.WriteUInt8((byte)formCancelReason);
    }

    public override void Deserialize(ref BinaryReader reader) {
        FormId = reader.ReadVarUInt();
        JsonResponse = reader.ReadBool() ? reader.ReadVarString() : null;
        FormCancelReason = reader.ReadBool() ? (ModalFormCancelReason)reader.ReadUInt8() : null;
    }
}
