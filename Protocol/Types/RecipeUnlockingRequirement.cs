using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class RecipeUnlockingRequirement : DataType {
    public const byte ContextNone = 0;
    public const byte ContextAlwaysUnlocked = 1;
    public const byte ContextPlayerInWater = 2;
    public const byte ContextPlayerHasManyItems = 3;

    public byte Context = ContextAlwaysUnlocked;
    public List<ItemDescriptorCount> Ingredients = [];

    public void Read(BinaryReader reader) {
        Context = reader.ReadUInt8();
        if (Context == ContextNone) {
            int count = checked((int)reader.ReadVarUInt());
            Ingredients = new List<ItemDescriptorCount>(count);
            for (int i = 0; i < count; i++) {
                ItemDescriptorCount descriptor = new();
                descriptor.Read(reader);
                Ingredients.Add(descriptor);
            }
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(Context);
        if (Context == ContextNone) {
            writer.WriteVarUInt((uint)Ingredients.Count);
            for (int i = 0; i < Ingredients.Count; i++) {
                Ingredients[i].Write(writer);
            }
        }
    }
}
