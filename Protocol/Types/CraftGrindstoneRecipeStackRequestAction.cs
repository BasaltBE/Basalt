using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CraftGrindstoneRecipeStackRequestAction : IStackRequestAction, DataType {
    public byte ActionType => 16;
    /// <summary>
    /// Network id of the grindstone recipe.
    /// </summary>
    public uint RecipeNetworkId;
    /// <summary>
    /// Requested craft count value.
    /// </summary>
    public byte NumberOfCrafts;
    /// <summary>
    /// Recipe cost value.
    /// </summary>
    public int Cost;
    public void Read(BinaryReader reader) {
        RecipeNetworkId = reader.ReadVarUInt();
        NumberOfCrafts = reader.ReadUInt8();
        Cost = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(RecipeNetworkId);
        writer.WriteUInt8(NumberOfCrafts);
        writer.WriteZigZag(Cost);
    }
}
