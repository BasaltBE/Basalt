using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(40)]
public sealed class SetActorMotionPacket : DataPacket {
    public ulong ActorRuntimeId;
    public Vec3 Motion = new();
    public ulong Tick;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(ActorRuntimeId);
        Motion.Write(ref writer);
        writer.WriteVarULong(Tick);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ActorRuntimeId = reader.ReadVarULong();
        Motion.Read(ref reader);
        Tick = reader.ReadVarULong();
    }
}
