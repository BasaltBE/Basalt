using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(52)]
public sealed class CraftingDataPacket : DataPacket {
    public ShapedRecipePayload[] ShapedRecipes = [];
    public ShapelessRecipePayload[] ShapelessRecipes = [];
    public MultiRecipePayload[] MultiRecipes = [];
    public ShapelessRecipePayload[] UserDataShapelessRecipes = [];
    public ShapelessRecipePayload[] ShapelessChemistryRecipes = [];
    public ShapedRecipePayload[] ShapedChemistryRecipes = [];
    public SmithingTransformRecipePayload[] SmithingTransformRecipes = [];
    public SmithingTrimRecipePayload[] SmithingTrimRecipes = [];
    public PotionMixDataEntry[] PotionMixes = [];
    public ContainerMixDataEntry[] ContainerMixes = [];
    public MaterialReducerDataEntry[] MaterialReducers = [];
    public bool ClearRecipes;

    public override void Serialize(ref BinaryWriter writer) {
        WriteArray(ref writer, ShapedRecipes);
        WriteArray(ref writer, ShapelessRecipes);
        WriteArray(ref writer, MultiRecipes);
        WriteArray(ref writer, UserDataShapelessRecipes);
        WriteArray(ref writer, ShapelessChemistryRecipes);
        WriteArray(ref writer, ShapedChemistryRecipes);
        WriteArray(ref writer, SmithingTransformRecipes);
        WriteArray(ref writer, SmithingTrimRecipes);
        WriteArray(ref writer, PotionMixes);
        WriteArray(ref writer, ContainerMixes);
        WriteArray(ref writer, MaterialReducers);
        writer.WriteBool(ClearRecipes);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ShapedRecipes = ReadArray<ShapedRecipePayload>(ref reader);
        ShapelessRecipes = ReadArray<ShapelessRecipePayload>(ref reader);
        MultiRecipes = ReadArray<MultiRecipePayload>(ref reader);
        UserDataShapelessRecipes = ReadArray<ShapelessRecipePayload>(ref reader);
        ShapelessChemistryRecipes = ReadArray<ShapelessRecipePayload>(ref reader);
        ShapedChemistryRecipes = ReadArray<ShapedRecipePayload>(ref reader);
        SmithingTransformRecipes = ReadArray<SmithingTransformRecipePayload>(ref reader);
        SmithingTrimRecipes = ReadArray<SmithingTrimRecipePayload>(ref reader);
        PotionMixes = ReadArray<PotionMixDataEntry>(ref reader);
        ContainerMixes = ReadArray<ContainerMixDataEntry>(ref reader);
        MaterialReducers = ReadArray<MaterialReducerDataEntry>(ref reader);
        ClearRecipes = reader.ReadBool();
    }

    static void WriteArray<T>(ref BinaryWriter writer, T[] values) where T : DataType {
        writer.WriteVarUInt((uint)values.Length);
        for (int i = 0; i < values.Length; i++) values[i].Write(ref writer);
    }

    static T[] ReadArray<T>(ref BinaryReader reader) where T : DataType, new() {
        int count = checked((int)reader.ReadVarUInt());
        T[] values = new T[count];
        for (int i = 0; i < count; i++) {
            values[i] = new T();
            values[i].Read(ref reader);
        }
        return values;
    }
}
