using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.BlockEvent)]
public sealed record BlockEventPacket : DataPacket {
    /// <summary>
    /// Block position for this event.
    /// </summary>
    public BlockPos Position;

    /// <summary>
    /// Block event type.
    /// </summary>
    public BlockEventType Type;

    /// <summary>
    /// Event-specific data value.
    /// </summary>
    public int Data;

    public override void Deserialize(Binary.BinaryReader reader) {
        BlockPos position = Position;
        position.Read(reader);
        Position = position;
        Type = (BlockEventType)reader.ReadVarInt();
        Data = reader.ReadVarInt();
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        Position.Write(writer);
        writer.WriteZigZag((int)Type);
        writer.WriteZigZag(Data);
    }
}
