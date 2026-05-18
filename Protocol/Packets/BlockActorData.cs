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

    public override void Deserialize(BinaryReader reader)
    {
        BlockPos position = Position;
        position.Read(reader);
        Position = position;
        Data = NBT.ReadRootCompoundTag(reader, new ReadWriteOptions(Name: true, Type: true, VarInt: true), canHaveName: true);
    }

    public override void Serialize(BinaryWriter writer)
    {
        Position.Write(writer);
        NBT.WriteTag(writer, Data, new ReadWriteOptions(Name: true, Type: true, VarInt: true), canHaveName: true);
    }
}
