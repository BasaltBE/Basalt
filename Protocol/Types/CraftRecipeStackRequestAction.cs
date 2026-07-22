using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CraftRecipeStackRequestAction : IStackRequestAction, DataType {
    public byte ActionType => 12;
    /// <summary>
    /// Network id of the recipe to craft.
    /// </summary>
    public uint RecipeNetworkId;
    /// <summary>
    /// Requested craft count value.
    /// </summary>
    public byte NumberOfCrafts;
    public void Read(BinaryReader reader) {
        RecipeNetworkId = reader.ReadVarUInt();
        NumberOfCrafts = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(RecipeNetworkId);
        writer.WriteUInt8(NumberOfCrafts);
    }
}
