using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

/// <summary>
/// Base class for all packets
/// </summary>
public abstract record DataPacket
{
    public abstract void Deserialize(BinaryReader reader);
    public abstract void Serialize(BinaryWriter writer);
}
