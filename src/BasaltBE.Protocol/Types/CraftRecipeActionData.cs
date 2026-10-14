using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CraftRecipeActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.CraftRecipe;
    public int RecipeNetId;
    public byte NumberOfRequestedCrafts;
    public override void Read(ref BinaryReader reader) { RecipeNetId = reader.ReadVarInt(); NumberOfRequestedCrafts = reader.ReadUInt8(); }
    public override void Write(ref BinaryWriter writer) { writer.WriteVarInt(RecipeNetId); writer.WriteUInt8(NumberOfRequestedCrafts); }
}
