using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(151)]
public sealed class UpdatePlayerGameTypePacket : DataPacket {
    public int PlayerGameType;
    public long TargetPlayer;
    public ulong Tick;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZag(PlayerGameType);
        writer.WriteVarLong(TargetPlayer);
        writer.WriteVarULong(Tick);
    }

    public override void Deserialize(ref BinaryReader reader) {
        PlayerGameType = reader.ReadZigZag();
        TargetPlayer = reader.ReadVarLong();
        Tick = reader.ReadVarULong();
    }
}
