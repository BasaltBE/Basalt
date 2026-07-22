using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.AddPlayer)]
public sealed record AddPlayerPacket : DataPacket {
    /// <summary>
    /// UUID of the player.
    /// </summary>
    public Guid Uuid;

    /// <summary>
    /// Username of the player.
    /// </summary>
    public string Username = string.Empty;

    /// <summary>
    /// Runtime id of the player.
    /// </summary>
    public ulong EntityRuntimeId;

    /// <summary>
    /// Platform chat id value.
    /// </summary>
    public string PlatformChatId = string.Empty;

    /// <summary>
    /// Spawn position of the player.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Initial player velocity.
    /// </summary>
    public Vec3f Velocity;

    /// <summary>
    /// Pitch rotation of the player.
    /// </summary>
    public float Pitch;

    /// <summary>
    /// Yaw rotation of the player.
    /// </summary>
    public float Yaw;

    /// <summary>
    /// Head yaw rotation of the player.
    /// </summary>
    public float HeadYaw;

    /// <summary>
    /// Held item shown for the player.
    /// </summary>
    public ItemInstance HeldItem = new();

    /// <summary>
    /// Game type of the player.
    /// </summary>
    public int GameType;

    /// <summary>
    /// Metadata entries for this player.
    /// </summary>
    public List<ActorMetadataItem> EntityMetadata = [];

    /// <summary>
    /// Dynamic properties for this player.
    /// </summary>
    public EntityProperties EntityProperties = new();

    /// <summary>
    /// Ability data for this player.
    /// </summary>
    public AbilityData AbilityData = new();

    /// <summary>
    /// Active links attached to this player.
    /// </summary>
    public List<EntityLink> EntityLinks = [];

    /// <summary>
    /// Device id of the player.
    /// </summary>
    public string DeviceId = string.Empty;

    /// <summary>
    /// Device operating system of the player.
    /// </summary>
    public DeviceOS DeviceOS;

    public override void Deserialize(Binary.BinaryReader reader) {
        Uuid = UUID.Read(reader);
        Username = reader.ReadVarString();
        EntityRuntimeId = reader.ReadVarULong();
        PlatformChatId = reader.ReadVarString();

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        Vec3f velocity = Velocity;
        velocity.Read(reader);
        Velocity = velocity;

        Pitch = reader.ReadF32(true);
        Yaw = reader.ReadF32(true);
        HeadYaw = reader.ReadF32(true);
        HeldItem.Read(reader);
        GameType = reader.ReadVarInt();

        int metadataCount = reader.ReadVarInt();
        EntityMetadata = new List<ActorMetadataItem>(metadataCount);
        for (int i = 0; i < metadataCount; i++) {
            ActorMetadataItem item = new();
            item.Read(reader);
            EntityMetadata.Add(item);
        }

        EntityProperties.Read(reader);
        AbilityData.Read(reader);

        int entityLinksCount = reader.ReadVarInt();
        EntityLinks = new List<EntityLink>(entityLinksCount);
        for (int i = 0; i < entityLinksCount; i++) {
            EntityLink link = new();
            link.Read(reader);
            EntityLinks.Add(link);
        }

        DeviceId = reader.ReadVarString();
        DeviceOS = (DeviceOS)reader.ReadInt32(true);
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        UUID.Write(writer, Uuid);
        writer.WriteVarString(Username);
        writer.WriteVarULong(EntityRuntimeId);
        writer.WriteVarString(PlatformChatId);
        Position.Write(writer);
        Velocity.Write(writer);
        writer.WriteF32(Pitch, true);
        writer.WriteF32(Yaw, true);
        writer.WriteF32(HeadYaw, true);
        HeldItem.Write(writer);
        writer.WriteVarInt(GameType);

        writer.WriteVarInt(EntityMetadata.Count);
        for (int i = 0; i < EntityMetadata.Count; i++) {
            EntityMetadata[i].Write(writer);
        }

        EntityProperties.Write(writer);
        AbilityData.Write(writer);

        writer.WriteVarInt(EntityLinks.Count);
        for (int i = 0; i < EntityLinks.Count; i++) {
            EntityLinks[i].Write(writer);
        }

        writer.WriteVarString(DeviceId);
        writer.WriteInt32((int)DeviceOS, true);
    }
}
