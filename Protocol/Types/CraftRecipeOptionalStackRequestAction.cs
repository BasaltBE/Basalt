using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CraftRecipeOptionalStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 15;
    public uint RecipeNetworkId { get; set; }
    public int FilterStringIndex { get; set; }

    public void Read(ref BinaryReader reader)
    {
        RecipeNetworkId = reader.ReadVarUInt();
        FilterStringIndex = reader.ReadInt32(true);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(RecipeNetworkId);
        writer.WriteInt32(FilterStringIndex, true);
    }
}
