using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

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
        writer.WriteVarUInt((uint)ShapedRecipes.Length);
        for (int i = 0; i < ShapedRecipes.Length; i++)
            ShapedRecipes[i].Write(ref writer);

        writer.WriteVarUInt((uint)ShapelessRecipes.Length);
        for (int i = 0; i < ShapelessRecipes.Length; i++)
            ShapelessRecipes[i].Write(ref writer);

        writer.WriteVarUInt((uint)MultiRecipes.Length);
        for (int i = 0; i < MultiRecipes.Length; i++)
            MultiRecipes[i].Write(ref writer);

        writer.WriteVarUInt((uint)UserDataShapelessRecipes.Length);
        for (int i = 0; i < UserDataShapelessRecipes.Length; i++)
            UserDataShapelessRecipes[i].Write(ref writer);

        writer.WriteVarUInt((uint)ShapelessChemistryRecipes.Length);
        for (int i = 0; i < ShapelessChemistryRecipes.Length; i++)
            ShapelessChemistryRecipes[i].Write(ref writer);

        writer.WriteVarUInt((uint)ShapedChemistryRecipes.Length);
        for (int i = 0; i < ShapedChemistryRecipes.Length; i++)
            ShapedChemistryRecipes[i].Write(ref writer);

        writer.WriteVarUInt((uint)SmithingTransformRecipes.Length);
        for (int i = 0; i < SmithingTransformRecipes.Length; i++)
            SmithingTransformRecipes[i].Write(ref writer);

        writer.WriteVarUInt((uint)SmithingTrimRecipes.Length);
        for (int i = 0; i < SmithingTrimRecipes.Length; i++)
            SmithingTrimRecipes[i].Write(ref writer);

        writer.WriteVarUInt((uint)PotionMixes.Length);
        for (int i = 0; i < PotionMixes.Length; i++)
            PotionMixes[i].Write(ref writer);

        writer.WriteVarUInt((uint)ContainerMixes.Length);
        for (int i = 0; i < ContainerMixes.Length; i++)
            ContainerMixes[i].Write(ref writer);

        writer.WriteVarUInt((uint)MaterialReducers.Length);
        for (int i = 0; i < MaterialReducers.Length; i++)
            MaterialReducers[i].Write(ref writer);

        writer.WriteBool(ClearRecipes);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ShapedRecipes = new ShapedRecipePayload[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < ShapedRecipes.Length; i++) {
            ShapedRecipes[i] = new ShapedRecipePayload();
            ShapedRecipes[i].Read(ref reader);
        }

        ShapelessRecipes = new ShapelessRecipePayload[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < ShapelessRecipes.Length; i++) {
            ShapelessRecipes[i] = new ShapelessRecipePayload();
            ShapelessRecipes[i].Read(ref reader);
        }

        MultiRecipes = new MultiRecipePayload[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < MultiRecipes.Length; i++) {
            MultiRecipes[i] = new MultiRecipePayload();
            MultiRecipes[i].Read(ref reader);
        }

        UserDataShapelessRecipes = new ShapelessRecipePayload[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < UserDataShapelessRecipes.Length; i++) {
            UserDataShapelessRecipes[i] = new ShapelessRecipePayload();
            UserDataShapelessRecipes[i].Read(ref reader);
        }

        ShapelessChemistryRecipes = new ShapelessRecipePayload[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < ShapelessChemistryRecipes.Length; i++) {
            ShapelessChemistryRecipes[i] = new ShapelessRecipePayload();
            ShapelessChemistryRecipes[i].Read(ref reader);
        }

        ShapedChemistryRecipes = new ShapedRecipePayload[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < ShapedChemistryRecipes.Length; i++) {
            ShapedChemistryRecipes[i] = new ShapedRecipePayload();
            ShapedChemistryRecipes[i].Read(ref reader);
        }

        SmithingTransformRecipes = new SmithingTransformRecipePayload[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < SmithingTransformRecipes.Length; i++) {
            SmithingTransformRecipes[i] = new SmithingTransformRecipePayload();
            SmithingTransformRecipes[i].Read(ref reader);
        }

        SmithingTrimRecipes = new SmithingTrimRecipePayload[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < SmithingTrimRecipes.Length; i++) {
            SmithingTrimRecipes[i] = new SmithingTrimRecipePayload();
            SmithingTrimRecipes[i].Read(ref reader);
        }

        PotionMixes = new PotionMixDataEntry[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < PotionMixes.Length; i++) {
            PotionMixes[i] = new PotionMixDataEntry();
            PotionMixes[i].Read(ref reader);
        }

        ContainerMixes = new ContainerMixDataEntry[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < ContainerMixes.Length; i++) {
            ContainerMixes[i] = new ContainerMixDataEntry();
            ContainerMixes[i].Read(ref reader);
        }

        MaterialReducers = new MaterialReducerDataEntry[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < MaterialReducers.Length; i++) {
            MaterialReducers[i] = new MaterialReducerDataEntry();
            MaterialReducers[i].Read(ref reader);
        }

        ClearRecipes = reader.ReadBool();
    }
}
