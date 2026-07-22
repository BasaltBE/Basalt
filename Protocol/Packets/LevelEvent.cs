using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.LevelEvent)]
public sealed record LevelEventPacket : DataPacket {
    /// <summary>
    /// Level event id.
    /// </summary>
    public LevelEvent Event;

    /// <summary>
    /// Event world position.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Event-specific data value.
    /// </summary>
    public int Data;

    public override void Deserialize(Binary.BinaryReader reader) {
        Event = (LevelEvent)reader.ReadZigZag();

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        Data = reader.ReadZigZag();
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteZigZag((int)Event);
        Position.Write(writer);
        writer.WriteZigZag(Data);
    }
}
