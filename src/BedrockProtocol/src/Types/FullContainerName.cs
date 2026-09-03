using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class FullContainerName : DataType {
    public ContainerEnumName ContainerName;
    public uint? DynamicId;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)ContainerName);
        writer.WriteBool(DynamicId.HasValue);
        if (DynamicId.HasValue) writer.WriteUInt32(DynamicId.Value, true);
    }

    public override void Read(ref BinaryReader reader) {
        ContainerName = (ContainerEnumName)reader.ReadUInt8();
        DynamicId = reader.ReadBool() ? reader.ReadUInt32(true) : null;
    }
}
