using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(32)]
public sealed class MobArmorEquipmentPacket : DataPacket {
    public ulong TargetRuntimeId;
    public NetworkItemStackDescriptor Head = new();
    public NetworkItemStackDescriptor Torso = new();
    public NetworkItemStackDescriptor Legs = new();
    public NetworkItemStackDescriptor Feet = new();
    public NetworkItemStackDescriptor Body = new();

    public override void Deserialize(ref BinaryReader reader) {
        TargetRuntimeId = reader.ReadVarULong();
        Head.Read(ref reader);
        Torso.Read(ref reader);
        Legs.Read(ref reader);
        Feet.Read(ref reader);
        Body.Read(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(TargetRuntimeId);
        Head.Write(ref writer);
        Torso.Write(ref writer);
        Legs.Write(ref writer);
        Feet.Write(ref writer);
        Body.Write(ref writer);
    }
}
