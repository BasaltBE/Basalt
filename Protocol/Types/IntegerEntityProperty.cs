using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class IntegerEntityProperty : DataType {
    /// <summary>
    /// Property index id.
    /// </summary>
    public uint Index;

    /// <summary>
    /// Integer property value.
    /// </summary>
    public int Value;

    public void Read(BinaryReader reader) {
        Index = reader.ReadVarUInt();
        Value = reader.ReadVarInt();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(Index);
        writer.WriteVarInt(Value);
    }
}
