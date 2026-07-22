using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CraftingDataEntry : DataType {
    public CraftingDataRecipeType RecipeType;
    public ShapedRecipeData? Shaped;
    public ShapelessRecipeData? Shapeless;
    public MultiRecipeData? Multi;
    public SmithingTransformRecipeData? SmithingTransform;
    public SmithingTrimRecipeData? SmithingTrim;

    public void Read(BinaryReader reader) {
        RecipeType = (CraftingDataRecipeType)reader.ReadZigZag();
        switch (RecipeType) {
            case CraftingDataRecipeType.Shapeless:
            case CraftingDataRecipeType.ShulkerBox:
            case CraftingDataRecipeType.ShapelessChemistry:
                Shapeless = new ShapelessRecipeData();
                Shapeless.Read(reader);
                break;

            case CraftingDataRecipeType.Shaped:
            case CraftingDataRecipeType.ShapedChemistry:
                Shaped = new ShapedRecipeData();
                Shaped.Read(reader);
                break;

            case CraftingDataRecipeType.Multi:
                Multi = new MultiRecipeData();
                Multi.Read(reader);
                break;

            case CraftingDataRecipeType.SmithingTransform:
                SmithingTransform = new SmithingTransformRecipeData();
                SmithingTransform.Read(reader);
                break;

            case CraftingDataRecipeType.SmithingTrim:
                SmithingTrim = new SmithingTrimRecipeData();
                SmithingTrim.Read(reader);
                break;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag((int)RecipeType);
        switch (RecipeType) {
            case CraftingDataRecipeType.Shapeless:
            case CraftingDataRecipeType.ShulkerBox:
            case CraftingDataRecipeType.ShapelessChemistry:
                Shapeless?.Write(writer);
                break;

            case CraftingDataRecipeType.Shaped:
            case CraftingDataRecipeType.ShapedChemistry:
                Shaped?.Write(writer);
                break;

            case CraftingDataRecipeType.Multi:
                Multi?.Write(writer);
                break;

            case CraftingDataRecipeType.SmithingTransform:
                SmithingTransform?.Write(writer);
                break;

            case CraftingDataRecipeType.SmithingTrim:
                SmithingTrim?.Write(writer);
                break;
        }
    }
}
