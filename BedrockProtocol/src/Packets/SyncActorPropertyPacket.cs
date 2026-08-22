using Basalt.BedrockProtocol.NBT;
using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Nbt = Basalt.BedrockProtocol.NBT.NBT;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(165)]
public sealed class SyncActorPropertyPacket : DataPacket {
    static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);
    public CompoundTag PropertyData = new();

    public override void Serialize(ref BinaryWriter writer) => Nbt.WriteTag(writer, PropertyData, NetworkNbtOptions);
    public override void Deserialize(ref BinaryReader reader) => PropertyData = Nbt.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
}
