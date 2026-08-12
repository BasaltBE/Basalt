#nullable enable

using System;
using BedrockProtocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ServerBlockProperty {
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    public string BlockName = string.Empty;
    public CompoundTag BlockDefinition = new();

    public void Read(BinaryReader reader) {
        BlockName = reader.ReadVarString();
        BlockDefinition = NBT.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(BlockName);
        NBT.WriteTag(writer, BlockDefinition, NetworkNbtOptions);
    }
}
