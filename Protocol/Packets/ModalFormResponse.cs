using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ModalFormResponse)]
public sealed record ModalFormResponsePacket : DataPacket {
    public int FormId;
    public string? Data;
    public bool Canceled;
    public ModalFormCanceledReason? Reason;

    public override void Deserialize(Binary.BinaryReader reader) {
        FormId = reader.ReadVarInt();
        bool hasResponse = reader.ReadBool();
        Data = hasResponse ? reader.ReadVarString() : null;
        Canceled = reader.ReadBool();
        Reason = Canceled ? (ModalFormCanceledReason)reader.ReadUInt8() : null;
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteVarInt(FormId);
        bool hasResponse = Data is not null;
        writer.WriteBool(hasResponse);
        if (hasResponse) {
            writer.WriteVarString(Data!);
        }

        writer.WriteBool(Canceled);
        if (Canceled) {
            writer.WriteUInt8((byte)(Reason ?? ModalFormCanceledReason.Closed));
        }
    }
}
