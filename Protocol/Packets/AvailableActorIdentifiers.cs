using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record AvailableActorIdentifiersPacket : DataPacket
{
    private static readonly ReadWriteOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);
    public CompoundTag Data { get; set; } = new();

    public override PacketId PacketId => PacketId.AvailableActorIdentifiers;

    public override void Deserialize(ref BinaryReader reader)
    {
        Data = Basalt.Protocol.IO.NBT.Read<CompoundTag>(ref reader, NetworkNbtOptions, canHaveName: true);
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        Basalt.Protocol.IO.NBT.WriteTag(ref writer, Data, NetworkNbtOptions, canHaveName: true);
    }
}
