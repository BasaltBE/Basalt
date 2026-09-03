using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class PropertySyncFloatEntry : DataType {
    public uint PropertyIndex;
    public float Data;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt(PropertyIndex);
        writer.WriteF32(Data, true);
    }

    public override void Read(ref BinaryReader reader) {
        PropertyIndex = reader.ReadVarUInt();
        Data = reader.ReadF32(true);
    }
}
