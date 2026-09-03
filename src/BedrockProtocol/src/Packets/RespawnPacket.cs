using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(45)]
public sealed class RespawnPacket : DataPacket {
    public Vec3 Position = new();
    public PlayerRespawnState State;
    public ulong PlayerRuntimeId;

    public override void Serialize(ref BinaryWriter writer) {
        Position.Write(ref writer);
        writer.WriteUInt8((byte)State);
        writer.WriteVarULong(PlayerRuntimeId);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        State = (PlayerRespawnState)reader.ReadUInt8();
        PlayerRuntimeId = reader.ReadVarULong();
    }
}
