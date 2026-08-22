using Basalt.Binary;
using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Nbt = Basalt.BedrockProtocol.NBT.NBT;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(56)]
public sealed class BlockActorDataPacket : DataPacket {
    static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);
    public BlockPos Position = new();
    public CompoundTag ActorData = new();

    public override void Serialize(ref BinaryWriter writer) {
        Position.Write(ref writer);
        Nbt.WriteTag(writer, ActorData, NetworkNbtOptions);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        ActorData = Nbt.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
    }
}
