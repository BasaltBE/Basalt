using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(15)]
public sealed class AddItemActorPacket : DataPacket {
    public long ActorUniqueId;
    public ulong ActorRuntimeId;
    public NetworkItemStackDescriptor Item = new();
    public Vec3 Position = new();
    public Vec3 Velocity = new();
    public ActorDataList EntityData = new();
    public bool FromFishing;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZong(ActorUniqueId);
        writer.WriteVarULong(ActorRuntimeId);
        Item.Write(ref writer);
        Position.Write(ref writer);
        Velocity.Write(ref writer);
        EntityData.Write(ref writer);
        writer.WriteBool(FromFishing);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ActorUniqueId = reader.ReadZigZong();
        ActorRuntimeId = reader.ReadVarULong();
        Item.Read(ref reader);
        Position.Read(ref reader);
        Velocity.Read(ref reader);
        EntityData.Read(ref reader);
        FromFishing = reader.ReadBool();
    }
}
