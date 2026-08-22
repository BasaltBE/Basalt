using Basalt.BedrockProtocol.NBT;
using Nbt = Basalt.BedrockProtocol.NBT.NBT;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(313)]
public sealed class JigsawStructureDataPacket : DataPacket {
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    public CompoundTag StructureData = new();

    public override void Serialize(ref BinaryWriter writer) => Nbt.WriteTag(writer, StructureData, NetworkNbtOptions);

    public override void Deserialize(ref BinaryReader reader) => StructureData = Nbt.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
}
