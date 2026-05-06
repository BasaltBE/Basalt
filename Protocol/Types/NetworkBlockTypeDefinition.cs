using Basalt.Protocol.IO;
using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class NetworkBlockTypeDefinition : DataType
{
    private static readonly ReadWriteOptions NetworkOptions = new(Name: true, Type: true, VarInt: true);

    public string Identifier { get; set; } = string.Empty;
    public CompoundTag Nbt { get; set; } = new();

    public NetworkBlockTypeDefinition() {}

    public NetworkBlockTypeDefinition(string identifier, CompoundTag nbt)
    {
        Identifier = identifier;
        Nbt = nbt;
    }

    public void Read(ref BinaryReader reader)
    {
        Identifier = reader.ReadVarString();
        Nbt = CompoundTag.Read(ref reader, NetworkOptions, canHaveName: true);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarString(Identifier);
        NBT.WriteTag(ref writer, Nbt, NetworkOptions, canHaveName: true);
    }

    public static List<NetworkBlockTypeDefinition> ReadList(ref BinaryReader reader)
    {
        int amount = reader.ReadVarInt();
        List<NetworkBlockTypeDefinition> properties = new(amount);

        for (int i = 0; i < amount; i++)
        {
            NetworkBlockTypeDefinition definition = new();
            definition.Read(ref reader);
            properties.Add(definition);
        }

        return properties;
    }

    public static void WriteList(ref BinaryWriter writer, IReadOnlyList<NetworkBlockTypeDefinition> value)
    {
        writer.WriteVarInt(value.Count);

        for (int i = 0; i < value.Count; i++)
        {
            value[i].Write(ref writer);
        }
    }
}

