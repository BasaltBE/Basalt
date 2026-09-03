using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(112)]
public sealed class SetScoreboardIdentityPacket : DataPacket {
    public ScoreboardIdentityPacketType Action;
    public ScoreboardIdentityInfo[] Entries = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)Action);
        writer.WriteVarUInt((uint)Entries.Length);
        for (int i = 0; i < Entries.Length; i++) Entries[i].Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Action = (ScoreboardIdentityPacketType)reader.ReadUInt8();
        int count = checked((int)reader.ReadVarUInt());
        Entries = new ScoreboardIdentityInfo[count];
        for (int i = 0; i < count; i++) {
            Entries[i] = new ScoreboardIdentityInfo();
            Entries[i].Read(ref reader);
        }
    }
}
