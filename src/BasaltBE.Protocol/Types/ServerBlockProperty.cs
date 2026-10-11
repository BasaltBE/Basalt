using Basalt.Binary;
using BasaltBE.Nbt;
using BasaltBE.Nbt.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using NbtCodec = BasaltBE.Nbt.Nbt;

namespace BasaltBE.Protocol.Types;

public sealed class ServerBlockProperty : DataType {
    public string BlockName = string.Empty;
    public CompoundTag BlockDefinition = new();

    public override void Read(ref BinaryReader reader) {
        BlockName = reader.ReadVarString();
        Tag tag = NbtCodec.Read(ref reader, new Options { VarInt = true, StringLengthVarInt = true });
        BlockDefinition = tag as CompoundTag ?? throw new InvalidDataException("Block definition must be an NBT compound.");
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(BlockName);
        NbtCodec.Write(BlockDefinition, ref writer, new Options { VarInt = true, StringLengthVarInt = true });
    }
}
