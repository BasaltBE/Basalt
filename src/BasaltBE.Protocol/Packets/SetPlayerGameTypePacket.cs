using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(62)]
public sealed class SetPlayerGameTypePacket : DataPacket {
    public GameType GameType;

    public override void Deserialize(ref BinaryReader reader) => GameType = (GameType)reader.ReadZigZag();

    public override void Serialize(ref BinaryWriter writer) => writer.WriteZigZag((int)GameType);
}
