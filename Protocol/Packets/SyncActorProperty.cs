using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.SyncActorProperty)]
public sealed record SyncActorPropertyPacket : DataPacket
{
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    /// <summary>
    /// Property data payload as NBT.
    /// </summary>
    public CompoundTag PropertyData = new();

    public override void Deserialize(Binary.BinaryReader reader)
    {
        PropertyData = CompoundTag.Read(reader, NetworkNbtOptions);
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        Io.NBT.WriteTag(writer, PropertyData, NetworkNbtOptions);
    }
}
