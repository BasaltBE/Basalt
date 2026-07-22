using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CreativeItem : DataType {
    /// <summary>
    /// Creative item network id.
    /// </summary>
    public uint CreativeItemNetworkId;

    /// <summary>
    /// Item descriptor payload.
    /// </summary>
    public LegacyNetworkItemStackDescriptor ItemInstance = new();

    /// <summary>
    /// Creative group index.
    /// </summary>
    public uint GroupIndex;

    public void Read(BinaryReader reader) {
        CreativeItemNetworkId = reader.ReadVarUInt();
        ItemInstance.Read(reader);
        GroupIndex = reader.ReadVarUInt();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(CreativeItemNetworkId);
        ItemInstance.Write(writer);
        writer.WriteVarUInt(GroupIndex);
    }
}
