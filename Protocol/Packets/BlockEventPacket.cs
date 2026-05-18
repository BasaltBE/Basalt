using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record BlockEventPacket : DataPacket
{
    public BlockPos Position { get; set; }
    public BlockEventType Type { get; set; }
    public int Data { get; set; }

    public override PacketId PacketId => PacketId.BlockEvent;

    public override void Deserialize(BinaryReader reader)
    {
        BlockPos position = Position;
        position.Read(reader);
        Position = position;
        Type = (BlockEventType)reader.ReadVarInt();
        Data = reader.ReadVarInt();
    }

    public override void Serialize(BinaryWriter writer)
    {
        Position.Write(writer);
        writer.WriteZigZag((int)Type);
        writer.WriteZigZag(Data);
    }
}
