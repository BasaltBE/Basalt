using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(44)]
public sealed class AnimatePacket : DataPacket {
    public AnimateAction Action;
    public ulong TargetActorRuntimeId;
    public float Data;
    public ActorSwingSource? SwingSource;
    public HandSlot Hand;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)Action);
        writer.WriteVarULong(TargetActorRuntimeId);
        writer.WriteF32(Data, true);
        writer.WriteBool(SwingSource.HasValue);
        if (SwingSource.HasValue)
            writer.WriteVarString(SwingSource.Value.ToString().ToLowerInvariant());
        writer.WriteUInt8((byte)Hand);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Action = (AnimateAction)reader.ReadUInt8();
        TargetActorRuntimeId = reader.ReadVarULong();
        Data = reader.ReadF32(true);
        SwingSource = reader.ReadBool()
            ? Enum.Parse<ActorSwingSource>(reader.ReadVarString(), true)
            : null;
        Hand = (HandSlot)reader.ReadUInt8();
    }
}
