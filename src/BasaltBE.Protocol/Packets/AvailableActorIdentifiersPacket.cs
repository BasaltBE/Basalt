using BasaltBE.Nbt;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(119)]
public sealed class AvailableActorIdentifiersPacket : DataPacket {
    public CompoundTag IdentifierList = new();

    public override void Serialize(ref BinaryWriter writer) =>
        Nbt.Nbt.Write(IdentifierList, ref writer, new Options { VarInt = true, StringLengthVarInt = true });

    public override void Deserialize(ref BinaryReader reader) {
        Tag identifierList = Nbt.Nbt.Read(ref reader, new Options { VarInt = true, StringLengthVarInt = true });
        IdentifierList = identifierList as CompoundTag
            ?? throw new InvalidDataException("Actor identifier list must be a compound tag.");
    }
}
