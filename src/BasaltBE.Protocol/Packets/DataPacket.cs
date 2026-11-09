using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

public abstract class DataPacket {
    public abstract void Serialize(ref BinaryWriter writer);
    public abstract void Deserialize(ref BinaryReader reader);
}
