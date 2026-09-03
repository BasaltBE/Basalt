using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(39)]
public sealed class SetActorDataPacket : DataPacket {
    public ulong ActorRuntimeId;
    public ActorDataList ActorData = new();
    public PropertySyncData SynchedProperties = new();
    public ulong Tick;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(ActorRuntimeId);
        ActorData.Write(ref writer);
        SynchedProperties.Write(ref writer);
        writer.WriteVarULong(Tick);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ActorRuntimeId = reader.ReadVarULong();
        ActorData.Read(ref reader);
        SynchedProperties.Read(ref reader);
        Tick = reader.ReadVarULong();
    }
}
