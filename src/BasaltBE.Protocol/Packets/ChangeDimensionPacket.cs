using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(61)]
public sealed class ChangeDimensionPacket : DataPacket {
    public DimensionType Dimension;
    public Vec3 Position = new();
    public bool Respawn;
    public uint? LoadingScreenId;

    public override void Deserialize(ref BinaryReader reader) {
        Dimension = (DimensionType)reader.ReadVarInt();
        Position.Read(ref reader);
        Respawn = reader.ReadBool();
        LoadingScreenId = reader.ReadBool() ? reader.ReadUInt32(true) : null;
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarInt((int)Dimension);
        Position.Write(ref writer);
        writer.WriteBool(Respawn);
        writer.WriteBool(LoadingScreenId is not null);
        if (LoadingScreenId is uint loadingScreenId)
            writer.WriteUInt32(loadingScreenId, true);
    }
}
