using Basalt.BedrockProtocol.NBT;
using Nbt = Basalt.BedrockProtocol.NBT.NBT;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(119)]
public sealed class AvailableActorIdentifiersPacket : DataPacket {
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    public CompoundTag IdentifierList = new();

    public override void Serialize(ref BinaryWriter writer) {
        Nbt.WriteTag(writer, IdentifierList, NetworkNbtOptions);
    }

    public override void Deserialize(ref BinaryReader reader) {
        IdentifierList = Nbt.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
    }
}
