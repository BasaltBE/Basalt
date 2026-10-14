using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CraftRecipeOptionalActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.CraftRecipeOptional;
    public int RecipeNetId;
    public int FilteredStringIndex;
    public override void Read(ref BinaryReader reader) { RecipeNetId = reader.ReadVarInt(); FilteredStringIndex = reader.ReadInt32(true); }
    public override void Write(ref BinaryWriter writer) { writer.WriteVarInt(RecipeNetId); writer.WriteInt32(FilteredStringIndex, true); }
}
