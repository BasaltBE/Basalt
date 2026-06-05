using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CraftCreativeStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 14;
    /// <summary>
    /// Creative item network id to create.
    /// </summary>
    public uint CreativeItemNetworkId;
    /// <summary>
    /// Requested craft count value.
    /// </summary>
    public byte NumberOfCrafts;
    public void Read(BinaryReader reader)
    {
        CreativeItemNetworkId = reader.ReadVarUInt();
        NumberOfCrafts = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarUInt(CreativeItemNetworkId);
        writer.WriteUInt8(NumberOfCrafts);
    }
}
