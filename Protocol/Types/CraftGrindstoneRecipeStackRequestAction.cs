using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CraftGrindstoneRecipeStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 16;
    public uint RecipeNetworkId { get; set; }
    public byte NumberOfCrafts { get; set; }
    public int Cost { get; set; }

    public void Read(BinaryReader reader)
    {
        RecipeNetworkId = reader.ReadVarUInt();
        NumberOfCrafts = reader.ReadUInt8();
        Cost = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarUInt(RecipeNetworkId);
        writer.WriteUInt8(NumberOfCrafts);
        writer.WriteZigZag(Cost);
    }
}
