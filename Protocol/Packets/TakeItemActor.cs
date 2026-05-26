using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.TakeItemActor)]
public sealed record TakeItemActorPacket : DataPacket
{
    /// <summary>
    /// Runtime id of the item actor.
    /// </summary>
    public ulong ItemEntityRuntimeId;

    /// <summary>
    /// Runtime id of the actor taking the item.
    /// </summary>
    public ulong TakerEntityRuntimeId;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        ItemEntityRuntimeId = reader.ReadVarULong();
        TakerEntityRuntimeId = reader.ReadVarULong();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarULong(ItemEntityRuntimeId);
        writer.WriteVarULong(TakerEntityRuntimeId);
    }
}
