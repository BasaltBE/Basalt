using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class NetworkItemInstanceDescriptorData : DataType {
    public ItemDescriptorData Descriptor = new();
    public ushort StackSize;
    public uint BlockRuntimeId;
    public string UserDataBuffer = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        Descriptor.Write(ref writer);
        writer.WriteUInt16(StackSize, true);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteVarString(UserDataBuffer);
    }

    public override void Read(ref BinaryReader reader) {
        Descriptor.Read(ref reader);
        StackSize = reader.ReadUInt16(true);
        BlockRuntimeId = reader.ReadVarUInt();
        UserDataBuffer = reader.ReadVarString();
    }
}
