using Basalt.BedrockProtocol.NBT;
using Nbt = Basalt.BedrockProtocol.NBT.NBT;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ServerBlockProperty : DataType {
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    public string BlockName = string.Empty;
    public CompoundTag BlockDefinition = new();

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(BlockName);
        Nbt.WriteTag(writer, BlockDefinition, NetworkNbtOptions);
    }

    public override void Read(ref BinaryReader reader) {
        BlockName = reader.ReadVarString();
        BlockDefinition = Nbt.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
    }
}
