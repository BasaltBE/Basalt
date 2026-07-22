using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class BlockEntry : DataType {
    private static readonly TagOptions TagOptions = new(Name: true, Type: true, VarInt: true);

    /// <summary>
    /// The name of the block, e.g. "minecraft:stone"
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// The properties / NBT of the block
    /// </summary>
    public CompoundTag Properties = new();

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        Properties = CompoundTag.Read(reader, TagOptions);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        Io.NBT.WriteTag(writer, Properties, TagOptions);
    }
}

