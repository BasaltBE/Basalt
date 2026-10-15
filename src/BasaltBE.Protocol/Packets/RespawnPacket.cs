using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(45)]
public sealed class RespawnPacket : DataPacket {
    public Vec3 Position = new();
    public PlayerRespawnState State;
    public ulong RuntimeId;

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        State = (PlayerRespawnState)reader.ReadUInt8();
        RuntimeId = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer) {
        Position.Write(ref writer);
        writer.WriteUInt8((byte)State);
        writer.WriteVarULong(RuntimeId);
    }
}
