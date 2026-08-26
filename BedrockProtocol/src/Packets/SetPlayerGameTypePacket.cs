using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(62)]
public sealed class SetPlayerGameTypePacket : DataPacket {
    public int PlayerGameType;

    public override void Serialize(ref BinaryWriter writer) => writer.WriteZigZag(PlayerGameType);
    public override void Deserialize(ref BinaryReader reader) => PlayerGameType = reader.ReadZigZag();
}
