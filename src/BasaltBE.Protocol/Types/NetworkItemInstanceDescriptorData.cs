using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class NetworkItemInstanceDescriptorData : DataType {
    public ItemStackItemDescriptorData ItemDescriptor = new EmptyItemDescriptorData();
    public ushort StackSize;
    public uint BlockRuntimeId;
    public string UserData = string.Empty;
    public override void Read(ref BinaryReader reader) {
        ItemDescriptor = ItemStackItemDescriptorData.ReadDescriptor(ref reader);
        StackSize = reader.ReadUInt16(true);
        BlockRuntimeId = reader.ReadVarUInt();
        UserData = reader.ReadVarString();
    }
    public override void Write(ref BinaryWriter writer) {
        ItemDescriptor.WriteDescriptor(ref writer);
        writer.WriteUInt16(StackSize, true);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteVarString(UserData);
    }
}
