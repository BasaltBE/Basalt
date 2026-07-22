using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class FloatEntityProperty : DataType {
    /// <summary>
    /// Property index id.
    /// </summary>
    public uint Index;

    /// <summary>
    /// Float property value.
    /// </summary>
    public float Value;

    public void Read(BinaryReader reader) {
        Index = reader.ReadVarUInt();
        Value = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(Index);
        writer.WriteF32(Value, true);
    }
}
