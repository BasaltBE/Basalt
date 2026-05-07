using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record InteractPacket : DataPacket
{
    public InteractActionType ActionType { get; set; }
    public ulong TargetEntityRuntimeId { get; set; }
    public OptionalValue<Vec3f> Position { get; set; } = new();

    public override PacketId PacketId => PacketId.Interact;

    public override void Deserialize(ref BinaryReader reader)
    {
        ActionType = (InteractActionType)reader.ReadUInt8();
        TargetEntityRuntimeId = reader.ReadVarULong();
        Position.Read(ref reader, static (ref BinaryReader r) =>
        {
            Vec3f value = new();
            value.Read(ref r);
            return value;
        });
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteUInt8((byte)ActionType);
        writer.WriteVarULong(TargetEntityRuntimeId);
        Position.Write(ref writer, static (ref BinaryWriter w, Vec3f value) => value.Write(ref w));
    }
}
