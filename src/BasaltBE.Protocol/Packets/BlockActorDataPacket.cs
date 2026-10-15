using BasaltBE.Nbt;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using NbtCodec = BasaltBE.Nbt.Nbt;

namespace BasaltBE.Protocol.Packets;

[PacketId(56)]
public sealed class BlockActorDataPacket : DataPacket {
    public BlockPos Position = new();
    public CompoundTag ActorData = new();

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        Tag actorData = NbtCodec.Read(ref reader, new Options { VarInt = true, StringLengthVarInt = true });
        ActorData = actorData as CompoundTag
            ?? throw new InvalidDataException("Block actor data must be an NBT compound.");
    }

    public override void Serialize(ref BinaryWriter writer) {
        Position.Write(ref writer);
        NbtCodec.Write(ActorData, ref writer, new Options { VarInt = true, StringLengthVarInt = true });
    }
}
