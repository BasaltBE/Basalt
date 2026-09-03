using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class UpdateSubChunkNetworkBlockInfo : DataType {
    public BlockPos Position = new();
    public uint RuntimeId;
    public uint UpdateFlags;
    public ulong EntityUniqueId;
    public uint Message;

    public override void Write(ref BinaryWriter writer) {
        Position.Write(ref writer);
        writer.WriteVarUInt(RuntimeId);
        writer.WriteVarUInt(UpdateFlags);
        writer.WriteVarULong(EntityUniqueId);
        writer.WriteVarUInt(Message);
    }

    public override void Read(ref BinaryReader reader) {
        Position.Read(ref reader);
        RuntimeId = reader.ReadVarUInt();
        UpdateFlags = reader.ReadVarUInt();
        EntityUniqueId = reader.ReadVarULong();
        Message = reader.ReadVarUInt();
    }
}
