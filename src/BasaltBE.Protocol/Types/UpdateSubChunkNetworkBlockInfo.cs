using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class UpdateSubChunkNetworkBlockInfo : DataType {
    public BlockPos Position = new();
    public uint RuntimeId;
    public UpdateBlockFlags Flags;
    public ulong SyncEntityUniqueId;
    public uint SyncMessage;

    public override void Read(ref BinaryReader reader) {
        Position.Read(ref reader);
        RuntimeId = reader.ReadVarUInt();
        Flags = (UpdateBlockFlags)reader.ReadVarUInt();
        SyncEntityUniqueId = reader.ReadVarULong();
        SyncMessage = reader.ReadVarUInt();
    }

    public override void Write(ref BinaryWriter writer) {
        ArgumentNullException.ThrowIfNull(Position);
        Position.Write(ref writer);
        writer.WriteVarUInt(RuntimeId);
        writer.WriteVarUInt((uint)Flags);
        writer.WriteVarULong(SyncEntityUniqueId);
        writer.WriteVarUInt(SyncMessage);
    }
}
