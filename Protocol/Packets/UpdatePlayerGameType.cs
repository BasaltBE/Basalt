using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.UpdatePlayerGameType)]
public sealed record UpdatePlayerGameTypePacket : DataPacket {
    public Gamemode GameType;
    public long PlayerUniqueId;
    public ulong Tick;

    public override void Deserialize(Binary.BinaryReader reader) {
        GameType = (Gamemode)reader.ReadZigZag();
        PlayerUniqueId = reader.ReadZigZong();
        Tick = reader.ReadVarULong();
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteZigZag((int)GameType);
        writer.WriteZigZong(PlayerUniqueId);
        writer.WriteVarULong(Tick);
    }
}
