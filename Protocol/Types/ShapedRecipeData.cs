using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ShapedRecipeData : DataType
{
    public string RecipeId = string.Empty;
    public int Width;
    public int Height;
    public List<ItemDescriptorCount> Input = [];
    public List<RecipeItemStack> Output = [];
    public byte[] Uuid = new byte[16];
    public string Block = "crafting_table";
    public int Priority;
    public bool AssumeSymmetry;
    public RecipeUnlockingRequirement UnlockRequirement = new();
    public uint RecipeNetworkId;

    public void Read(BinaryReader reader)
    {
        RecipeId = reader.ReadVarString();
        Width = reader.ReadZigZag();
        Height = reader.ReadZigZag();

        int inputCount = Width * Height;
        Input = new List<ItemDescriptorCount>(inputCount);
        for (int i = 0; i < inputCount; i++)
        {
            ItemDescriptorCount descriptor = new();
            descriptor.Read(reader);
            Input.Add(descriptor);
        }

        int outputCount = checked((int)reader.ReadVarUInt());
        Output = new List<RecipeItemStack>(outputCount);
        for (int i = 0; i < outputCount; i++)
        {
            RecipeItemStack item = new();
            item.Read(reader);
            Output.Add(item);
        }

        reader.ReadBytes(16).CopyTo(Uuid.AsSpan());
        Block = reader.ReadVarString();
        Priority = reader.ReadZigZag();
        AssumeSymmetry = reader.ReadBool();
        UnlockRequirement.Read(reader);
        RecipeNetworkId = reader.ReadVarUInt();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(RecipeId);
        writer.WriteZigZag(Width);
        writer.WriteZigZag(Height);

        int inputCount = Width * Height;
        for (int i = 0; i < inputCount; i++)
        {
            if (i < Input.Count)
            {
                Input[i].Write(writer);
            }
            else
            {
                writer.WriteUInt8(0);
                writer.WriteZigZag(0);
            }
        }

        writer.WriteVarUInt((uint)Output.Count);
        for (int i = 0; i < Output.Count; i++)
        {
            Output[i].Write(writer);
        }

        writer.WriteBytes(Uuid);
        writer.WriteVarString(Block);
        writer.WriteZigZag(Priority);
        writer.WriteBool(AssumeSymmetry);
        UnlockRequirement.Write(writer);
        writer.WriteVarUInt(RecipeNetworkId);
    }
}
