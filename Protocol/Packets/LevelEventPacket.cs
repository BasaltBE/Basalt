using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record LevelEventPacket : DataPacket
{
    public LevelEvent Event { get; set; }
    public Vec3f Position { get; set; }
    public int Data { get; set; }

    public override PacketId PacketId => PacketId.LevelEvent;

    public override void Deserialize(BinaryReader reader)
    {
        Event = (LevelEvent)reader.ReadZigZag();
        Vec3f position = Position;
        position.Read(reader);
        Position = position;
        Data = reader.ReadZigZag();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteZigZag((int)Event);
        Position.Write(writer);
        writer.WriteZigZag(Data);
    }
}
