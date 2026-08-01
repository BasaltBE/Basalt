using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public struct BlockChangeEntry {
    public BlockPos Position;
    public uint BlockRuntimeId;
    public uint Flags;
    public ulong SyncedUpdateEntityUniqueId;
    public uint SyncedUpdateType;

    public static BlockChangeEntry Read(BinaryReader reader) {
        BlockChangeEntry entry = new();
        entry.Position = new BlockPos {
            X = reader.ReadZigZag(),
            Y = reader.ReadZigZag(),
            Z = reader.ReadZigZag()
        };
        entry.BlockRuntimeId = reader.ReadVarUInt();
        entry.Flags = reader.ReadVarUInt();
        entry.SyncedUpdateEntityUniqueId = reader.ReadVarULong();
        entry.SyncedUpdateType = reader.ReadVarUInt();
        return entry;
    }

    public readonly void Write(BinaryWriter writer) {
        writer.WriteZigZag(Position.X);
        writer.WriteZigZag(Position.Y);
        writer.WriteZigZag(Position.Z);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteVarUInt(Flags);
        writer.WriteVarULong(SyncedUpdateEntityUniqueId);
        writer.WriteVarUInt(SyncedUpdateType);
    }
}
