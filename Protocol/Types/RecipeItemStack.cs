using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class RecipeItemStack : DataType {
    public int NetworkId;
    public ushort Count = 1;
    public uint Metadata;
    public int BlockRuntimeId;

    public void Read(BinaryReader reader) {
        NetworkId = reader.ReadZigZag();
        if (NetworkId == 0) return;

        Count = reader.ReadUInt16(true);
        Metadata = reader.ReadVarUInt();
        BlockRuntimeId = reader.ReadZigZag();

        int extrasLength = checked((int)reader.ReadVarUInt());
        if (extrasLength > 0) {
            reader.Advance(extrasLength);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(NetworkId);
        if (NetworkId == 0) return;

        writer.WriteUInt16(Count, true);
        writer.WriteVarUInt(Metadata);
        writer.WriteZigZag(BlockRuntimeId);

        writer.WriteVarUInt(10);
        writer.WriteInt16(0, true);
        writer.WriteUInt32(0, true);
        writer.WriteUInt32(0, true);
    }
}
