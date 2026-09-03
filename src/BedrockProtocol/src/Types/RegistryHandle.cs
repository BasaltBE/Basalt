using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class RegistryHandle : DataType {
    public ushort Value;

    public override void Write(ref BinaryWriter writer) => writer.WriteUInt16(Value, true);

    public override void Read(ref BinaryReader reader) => Value = reader.ReadUInt16(true);
}
