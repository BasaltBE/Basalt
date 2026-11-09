using Basalt.Binary;
using BasaltBE.Nbt;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using NbtCodec = BasaltBE.Nbt.Nbt;

namespace BasaltBE.Protocol.Packets;

[PacketId(313)]
public sealed class JigsawStructureDataPacket : DataPacket {
    public CompoundTag StructureData = new();

    public override void Serialize(ref BinaryWriter writer) =>
        NbtCodec.Write(StructureData, ref writer, new Options { VarInt = true, StringLengthVarInt = true });

    public override void Deserialize(ref BinaryReader reader) {
        Tag tag = NbtCodec.Read(ref reader, new Options { VarInt = true, StringLengthVarInt = true });
        StructureData = tag as CompoundTag ?? throw new InvalidDataException("Jigsaw structure data must be an NBT compound.");
    }
}
