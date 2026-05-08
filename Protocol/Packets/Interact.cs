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
        if (reader.Remaining > 0)
        {
            TargetEntityRuntimeId = reader.ReadVarULong();
        }
        else
        {
            TargetEntityRuntimeId = 0;
        }

        if (ActionType == InteractActionType.MouseOverEntity && reader.Remaining >= 12)
        {
            Vec3f value = new();
            value.Read(ref reader);
            Position = new OptionalValue<Vec3f> { HasValue = true, Value = value };
        }
        else
        {
            Position = new OptionalValue<Vec3f> { HasValue = false };
        }
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteUInt8((byte)ActionType);

        writer.WriteVarULong(TargetEntityRuntimeId);

        if (ActionType == InteractActionType.MouseOverEntity && Position.HasValue && Position.Value is { } value)
        {
            value.Write(ref writer);
        }
    }
}
