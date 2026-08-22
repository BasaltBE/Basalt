using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(36)]
public sealed class PlayerActionPacket : DataPacket {
    public ulong PlayerRuntimeId;
    public PlayerActionType Action;
    public BlockPos BlockPosition = new();
    public BlockPos ResultPosition = new();
    public int Face;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(PlayerRuntimeId);
        writer.WriteVarInt((int)Action);
        BlockPosition.Write(ref writer);
        ResultPosition.Write(ref writer);
        writer.WriteVarInt(Face);
    }

    public override void Deserialize(ref BinaryReader reader) {
        PlayerRuntimeId = reader.ReadVarULong();
        Action = (PlayerActionType)reader.ReadVarInt();
        BlockPosition.Read(ref reader);
        ResultPosition.Read(ref reader);
        Face = reader.ReadVarInt();
    }
}
