using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CraftCreativeStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 14;
    public uint CreativeItemNetworkId { get; set; }
    public byte NumberOfCrafts { get; set; }

    public void Read(ref BinaryReader reader)
    {
        CreativeItemNetworkId = reader.ReadVarUInt();
        NumberOfCrafts = reader.ReadUInt8();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(CreativeItemNetworkId);
        writer.WriteUInt8(NumberOfCrafts);
    }
}
