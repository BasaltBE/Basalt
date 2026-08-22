using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(118)]
public sealed class SpawnParticleEffectPacket : DataPacket {
    public byte DimensionId;
    public long ActorId;
    public Vec3 Position = new();
    public string EffectName = string.Empty;
    public string? MolangVariables;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8(DimensionId);
        writer.WriteVarLong(ActorId);
        Position.Write(ref writer);
        writer.WriteVarString(EffectName);
        writer.WriteBool(MolangVariables is not null);
        if (MolangVariables is string molangVariables) writer.WriteVarString(molangVariables);
    }

    public override void Deserialize(ref BinaryReader reader) {
        DimensionId = reader.ReadUInt8();
        ActorId = reader.ReadVarLong();
        Position.Read(ref reader);
        EffectName = reader.ReadVarString();
        MolangVariables = reader.ReadBool() ? reader.ReadVarString() : null;
    }
}
