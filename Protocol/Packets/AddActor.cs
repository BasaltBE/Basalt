using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using ProtoAttribute = Basalt.Protocol.Types.Attribute;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.AddActor)]
public sealed record AddActorPacket : DataPacket {
    /// <summary>
    /// Unique id of the actor.
    /// </summary>
    public long EntityUniqueId;

    /// <summary>
    /// Runtime id of the actor.
    /// </summary>
    public ulong EntityRuntimeId;

    /// <summary>
    /// String identifier of the actor type.
    /// </summary>
    public string EntityType = string.Empty;

    /// <summary>
    /// Spawn position of the actor.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Initial velocity of the actor.
    /// </summary>
    public Vec3f Velocity;

    /// <summary>
    /// Pitch rotation of the actor.
    /// </summary>
    public float Pitch;

    /// <summary>
    /// Yaw rotation of the actor.
    /// </summary>
    public float Yaw;

    /// <summary>
    /// Head yaw rotation of the actor.
    /// </summary>
    public float HeadYaw;

    /// <summary>
    /// Body yaw rotation of the actor.
    /// </summary>
    public float BodyYaw;

    /// <summary>
    /// Attributes sent with this actor.
    /// </summary>
    public List<ProtoAttribute> Attributes = [];

    /// <summary>
    /// Metadata entries for this actor.
    /// </summary>
    public List<ActorMetadataItem> EntityMetadata = [];

    /// <summary>
    /// Dynamic properties for this actor.
    /// </summary>
    public EntityProperties EntityProperties = new();

    /// <summary>
    /// Active links attached to this actor.
    /// </summary>
    public List<EntityLink> EntityLinks = [];

    public override void Deserialize(Binary.BinaryReader reader) {
        EntityUniqueId = reader.ReadVarLong();
        EntityRuntimeId = reader.ReadVarULong();
        EntityType = reader.ReadVarString();

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        Vec3f velocity = Velocity;
        velocity.Read(reader);
        Velocity = velocity;

        Pitch = reader.ReadF32(true);
        Yaw = reader.ReadF32(true);
        HeadYaw = reader.ReadF32(true);
        BodyYaw = reader.ReadF32(true);

        Attributes = ProtoAttribute.ReadList(reader);

        int metadataCount = reader.ReadVarInt();
        EntityMetadata = new List<ActorMetadataItem>(metadataCount);
        for (int i = 0; i < metadataCount; i++) {
            ActorMetadataItem item = new();
            item.Read(reader);
            EntityMetadata.Add(item);
        }

        EntityProperties.Read(reader);

        int entityLinksCount = reader.ReadVarInt();
        EntityLinks = new List<EntityLink>(entityLinksCount);
        for (int i = 0; i < entityLinksCount; i++) {
            EntityLink link = new();
            link.Read(reader);
            EntityLinks.Add(link);
        }
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteVarLong(EntityUniqueId);
        writer.WriteVarULong(EntityRuntimeId);
        writer.WriteVarString(EntityType);
        Position.Write(writer);
        Velocity.Write(writer);
        writer.WriteF32(Pitch, true);
        writer.WriteF32(Yaw, true);
        writer.WriteF32(HeadYaw, true);
        writer.WriteF32(BodyYaw, true);
        ProtoAttribute.WriteList(writer, Attributes);

        writer.WriteVarInt(EntityMetadata.Count);
        for (int i = 0; i < EntityMetadata.Count; i++) {
            EntityMetadata[i].Write(writer);
        }

        EntityProperties.Write(writer);

        writer.WriteVarInt(EntityLinks.Count);
        for (int i = 0; i < EntityLinks.Count; i++) {
            EntityLinks[i].Write(writer);
        }
    }
}
