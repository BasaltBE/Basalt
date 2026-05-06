using Basalt.Protocol.IO;
using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class BlockEntry : DataType
{
    private static readonly ReadWriteOptions NetworkOptions = new(Name: true, Type: true, VarInt: true);

    public string Name { get; set; } = string.Empty;
    public CompoundTag Properties { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        Name = reader.ReadVarString();
        Properties = CompoundTag.Read(ref reader, NetworkOptions, canHaveName: true);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarString(Name);
        NBT.WriteTag(ref writer, Properties, NetworkOptions, canHaveName: true);
    }
}

