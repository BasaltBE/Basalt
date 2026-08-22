using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(21)]
public sealed class UpdateBlockPacket : DataPacket {
    public BlockPos Position = new();
    public uint BlockRuntimeId;
    public uint Flags;
    public uint Layer;

    public override void Serialize(ref BinaryWriter writer) {
        Position.Write(ref writer);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteVarUInt(Flags);
        writer.WriteVarUInt(Layer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        BlockRuntimeId = reader.ReadVarUInt();
        Flags = reader.ReadVarUInt();
        Layer = reader.ReadVarUInt();
    }
}
