using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(21)]
public sealed class UpdateBlockPacket : DataPacket {
    public BlockPos Position = new();
    public uint BlockRuntimeId;
    public UpdateBlockFlags Flags;
    public uint Layer;

    public override void Serialize(ref BinaryWriter writer) {
        Position.Write(ref writer);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteVarUInt((uint)Flags);
        writer.WriteVarUInt(Layer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        BlockRuntimeId = reader.ReadVarUInt();
        Flags = (UpdateBlockFlags)reader.ReadVarUInt();
        Layer = reader.ReadVarUInt();
    }
}
