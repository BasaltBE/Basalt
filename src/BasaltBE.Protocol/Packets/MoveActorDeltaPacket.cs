using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(111)]
public sealed class MoveActorDeltaPacket : DataPacket {
    public MoveActorDeltaData MoveData = new();

    public override void Deserialize(ref BinaryReader reader) => MoveData.Read(ref reader);

    public override void Serialize(ref BinaryWriter writer) => MoveData.Write(ref writer);
}
