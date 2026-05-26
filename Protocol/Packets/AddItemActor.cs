using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.AddItemActor)]
public sealed record AddItemActorPacket : DataPacket
{
    /// <summary>
    /// Unique id of the item actor.
    /// </summary>
    public long EntityUniqueId;

    /// <summary>
    /// Runtime id of the item actor.
    /// </summary>
    public ulong EntityRuntimeId;

    /// <summary>
    /// Spawned item stack.
    /// </summary>
    public ItemInstance Item = new();

    /// <summary>
    /// Spawn position of the item actor.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Initial velocity of the item actor.
    /// </summary>
    public Vec3f Velocity;

    /// <summary>
    /// Metadata entries for the item actor.
    /// </summary>
    public List<ActorMetadataItem> EntityMetadata = [];

    /// <summary>
    /// Whether this item came from fishing.
    /// </summary>
    public bool FromFishing;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        EntityUniqueId = reader.ReadVarLong();
        EntityRuntimeId = reader.ReadVarULong();
        Item.Read(reader);

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        Vec3f velocity = Velocity;
        velocity.Read(reader);
        Velocity = velocity;

        int metadataCount = reader.ReadVarInt();
        EntityMetadata = new List<ActorMetadataItem>(metadataCount);
        for (int i = 0; i < metadataCount; i++)
        {
            ActorMetadataItem item = new();
            item.Read(reader);
            EntityMetadata.Add(item);
        }

        FromFishing = reader.ReadBool();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarLong(EntityUniqueId);
        writer.WriteVarULong(EntityRuntimeId);
        Item.Write(writer);
        Position.Write(writer);
        Velocity.Write(writer);

        writer.WriteVarInt(EntityMetadata.Count);
        for (int i = 0; i < EntityMetadata.Count; i++)
        {
            EntityMetadata[i].Write(writer);
        }

        writer.WriteBool(FromFishing);
    }
}
