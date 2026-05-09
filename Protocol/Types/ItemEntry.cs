using Basalt.Protocol.IO;
using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemEntry : DataType
{
    private static readonly ReadWriteOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    public string Name { get; set; } = string.Empty;
    public short RuntimeId { get; set; }
    public bool ComponentBased { get; set; }
    public int Version { get; set; }
    public CompoundTag Data { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        Name = reader.ReadVarString();
        RuntimeId = reader.ReadInt16(true);
        ComponentBased = reader.ReadBool();
        Version = reader.ReadZigZag();
        Data = CompoundTag.Read(ref reader, NetworkNbtOptions, canHaveName: true);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarString(Name);
        writer.WriteInt16(RuntimeId, true);
        writer.WriteBool(ComponentBased);
        writer.WriteZigZag(Version);
        NBT.WriteTag(ref writer, Data, NetworkNbtOptions, canHaveName: true);
    }
}
