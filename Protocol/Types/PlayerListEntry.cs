using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PlayerListEntry : DataType {
    /// <summary>
    /// UUID of the player.
    /// </summary>
    public Guid Uuid;

    /// <summary>
    /// Unique entity id of the player.
    /// </summary>
    public long EntityUniqueId;

    /// <summary>
    /// Username shown in the list.
    /// </summary>
    public string Username = string.Empty;

    /// <summary>
    /// XUID value.
    /// </summary>
    public string Xuid = string.Empty;

    /// <summary>
    /// Platform chat id value.
    /// </summary>
    public string PlatformChatId = string.Empty;

    /// <summary>
    /// Device operating system value.
    /// </summary>
    public DeviceOS DeviceOS;

    /// <summary>
    /// Skin payload for this player.
    /// </summary>
    public Skin Skin = new();

    /// <summary>
    /// Whether player is a teacher.
    /// </summary>
    public bool Teacher;

    /// <summary>
    /// Whether player is the host.
    /// </summary>
    public bool Host;

    /// <summary>
    /// Whether player is a sub-client.
    /// </summary>
    public bool SubClient;

    /// <summary>
    /// Player color encoded as ARGB int32.
    /// </summary>
    public int PlayerColor;

    public void Read(BinaryReader reader) {
        Uuid = UUID.Read(reader);
        EntityUniqueId = reader.ReadVarLong();
        Username = reader.ReadVarString();
        Xuid = reader.ReadVarString();
        PlatformChatId = reader.ReadVarString();
        DeviceOS = (DeviceOS)reader.ReadInt32(true);
        Skin.Read(reader);
        Teacher = reader.ReadBool();
        Host = reader.ReadBool();
        SubClient = reader.ReadBool();
        PlayerColor = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        Write(writer, default);
    }

    public void Write(BinaryWriter writer, ReadOnlySpan<byte> serializedSkinData) {
        UUID.Write(writer, Uuid);
        writer.WriteVarLong(EntityUniqueId);
        writer.WriteVarString(Username);
        writer.WriteVarString(Xuid);
        writer.WriteVarString(PlatformChatId);
        writer.WriteInt32((int)DeviceOS, true);
        if (!serializedSkinData.IsEmpty) {
            writer.WriteBytes(serializedSkinData);
        }
        else {
            Skin.Write(writer);
        }
        writer.WriteBool(Teacher);
        writer.WriteBool(Host);
        writer.WriteBool(SubClient);
        writer.WriteInt32(PlayerColor, true);
    }
}
