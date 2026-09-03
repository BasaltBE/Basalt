using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(61)]
public sealed class ChangeDimensionPacket : DataPacket {
    public int DimensionId;
    public Vec3 Position = new();
    public bool Respawn;
    public uint? LoadingScreenId;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarInt(DimensionId);
        Position.Write(ref writer);
        writer.WriteBool(Respawn);
        writer.WriteBool(LoadingScreenId.HasValue);
        if (LoadingScreenId is uint loadingScreenId) writer.WriteUInt32(loadingScreenId, true);
    }

    public override void Deserialize(ref BinaryReader reader) {
        DimensionId = reader.ReadVarInt();
        Position.Read(ref reader);
        Respawn = reader.ReadBool();
        LoadingScreenId = reader.ReadBool() ? reader.ReadUInt32(true) : null;
    }
}
