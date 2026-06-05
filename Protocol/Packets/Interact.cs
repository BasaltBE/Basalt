using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.Interact)]
public sealed record InteractPacket : DataPacket
{
    /// <summary>
    /// Interaction action type.
    /// </summary>
    public InteractActionType ActionType;

    /// <summary>
    /// Target entity runtime id.
    /// </summary>
    public ulong TargetEntityRuntimeId;

    /// <summary>
    /// Optional interaction position.
    /// </summary>
    public OptionalValue<Vec3f> Position = new();

    public override void Deserialize(Binary.BinaryReader reader)
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
            value.Read(reader);
            Position = new OptionalValue<Vec3f> { HasValue = true, Value = value };
        }
        else
        {
            Position = new OptionalValue<Vec3f> { HasValue = false };
        }
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteUInt8((byte)ActionType);
        writer.WriteVarULong(TargetEntityRuntimeId);

        if (ActionType == InteractActionType.MouseOverEntity && Position.HasValue && Position.Value is { } value)
        {
            value.Write(writer);
        }
    }
}
