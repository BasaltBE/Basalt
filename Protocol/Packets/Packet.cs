using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public abstract record DataPacket
{
    public abstract PacketId PacketId { get; }

    public abstract void Deserialize(BinaryReader reader);
    public abstract void Serialize(BinaryWriter writer);
}
