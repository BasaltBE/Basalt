using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CraftRecipeOptionalStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 15;
    /// <summary>
    /// Network id of the optional recipe.
    /// </summary>
    public uint RecipeNetworkId;
    /// <summary>
    /// Index into request filter strings.
    /// </summary>
    public int FilterStringIndex;
    public void Read(BinaryReader reader)
    {
        RecipeNetworkId = reader.ReadVarUInt();
        FilterStringIndex = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarUInt(RecipeNetworkId);
        writer.WriteInt32(FilterStringIndex, true);
    }
}
