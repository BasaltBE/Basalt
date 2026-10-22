using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(151)]
public sealed class UpdatePlayerGameTypePacket : DataPacket {
    public GameType GameType;
    public long TargetPlayerUniqueId;
    public ulong Tick;

    public override void Deserialize(ref BinaryReader reader) {
        GameType = (GameType)reader.ReadZigZag();
        TargetPlayerUniqueId = reader.ReadZigZong();
        Tick = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZag((int)GameType);
        writer.WriteZigZong(TargetPlayerUniqueId);
        writer.WriteVarULong(Tick);
    }
}
