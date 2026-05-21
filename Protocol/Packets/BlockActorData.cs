using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.BlockActorData)]
public sealed record BlockActorDataPacket : DataPacket
{
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    /// <summary>
    /// Block entity position.
    /// </summary>
    public BlockPos Position;

    /// <summary>
    /// Block entity NBT payload.
    /// </summary>
    public CompoundTag Data = new();

    public override void Deserialize(Binary.BinaryReader reader)
    {
        BlockPos position = Position;
        position.Read(reader);
        Position = position;
        Data = Io.NBT.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        Position.Write(writer);
        Io.NBT.WriteTag(writer, Data, NetworkNbtOptions);
    }
}
