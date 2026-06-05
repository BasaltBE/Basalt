using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemEntry : DataType
{
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    /// <summary>
    /// Item identifier name.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Item runtime id.
    /// </summary>
    public short RuntimeId;

    /// <summary>
    /// Whether this item uses component data.
    /// </summary>
    public bool ComponentBased;

    /// <summary>
    /// Item version value.
    /// </summary>
    public int Version;

    /// <summary>
    /// Item component NBT payload.
    /// </summary>
    public CompoundTag Data = new();

    public void Read(BinaryReader reader)
    {
        Name = reader.ReadVarString();
        RuntimeId = reader.ReadInt16(true);
        ComponentBased = reader.ReadBool();
        Version = reader.ReadZigZag();
        Data = CompoundTag.Read(reader, NetworkNbtOptions);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(Name);
        writer.WriteInt16(RuntimeId, true);
        writer.WriteBool(ComponentBased);
        writer.WriteZigZag(Version);
        Io.NBT.WriteTag(writer, Data, NetworkNbtOptions);
    }
}
