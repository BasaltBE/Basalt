using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(40)]
public sealed class SetActorMotionPacket : DataPacket {
    public ulong TargetRuntimeId;
    public Vec3 Motion = new();
    public ulong Tick;

    public override void Deserialize(ref BinaryReader reader) {
        TargetRuntimeId = reader.ReadVarULong();
        Motion.Read(ref reader);
        Tick = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(TargetRuntimeId);
        Motion.Write(ref writer);
        writer.WriteVarULong(Tick);
    }
}
