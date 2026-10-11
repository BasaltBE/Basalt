using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class VoxelShapeRegistryHandle : DataType {
    public ushort Value;

    public override void Read(ref BinaryReader reader) => Value = reader.ReadUInt16(true);

    public override void Write(ref BinaryWriter writer) => writer.WriteUInt16(Value, true);
}
