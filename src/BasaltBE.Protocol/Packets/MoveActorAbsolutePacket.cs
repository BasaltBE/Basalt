using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(18)]
public sealed class MoveActorAbsolutePacket : DataPacket {
    public MoveActorAbsoluteData MoveData = new();

    public override void Deserialize(ref BinaryReader reader) => MoveData.Read(ref reader);

    public override void Serialize(ref BinaryWriter writer) => MoveData.Write(ref writer);
}
