using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class CreativeItemEntryPayload : DataType {
    public int CreativeNetId;
    public CreativeItemStack ItemInstance = new();
    public uint GroupIndex;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)CreativeNetId));
        ItemInstance.Write(ref writer);
        writer.WriteVarUInt(GroupIndex);
    }

    public override void Read(ref BinaryReader reader) {
        CreativeNetId = checked((int)reader.ReadVarUInt());
        ItemInstance.Read(ref reader);
        GroupIndex = reader.ReadVarUInt();
    }
}
