using Basalt.Protocol.Enums;
using Basalt.Protocol.IO;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record BlockActorDataPacket : DataPacket
{
    public BlockPos Position { get; set; }
    public CompoundTag Data { get; set; } = new();

    public override PacketId PacketId => PacketId.BlockActorData;

    public override void Deserialize(ref BinaryReader reader)
    {
        BlockPos position = Position;
        position.Read(ref reader);
        Position = position;
        Data = CompoundTag.Read(ref reader, new ReadWriteOptions(Name: true, Type: true, VarInt: true), canHaveName: true);
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        Position.Write(ref writer);
        NBT.WriteTag(ref writer, Data, new ReadWriteOptions(Name: true, Type: true, VarInt: true), canHaveName: true);
    }
}
