using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.UpdateAbilities)]
public sealed record UpdateAbilitiesPacket : DataPacket {
    /// <summary>
    /// Unique entity id of the player whose abilities are updated.
    /// </summary>
    public long EntityUniqueId;

    /// <summary>
    /// Player permission level shown by the client.
    /// </summary>
    public PlayerPermissionLevel PlayerPermission = PlayerPermissionLevel.Member;

    /// <summary>
    /// Command permission level granted to the player.
    /// </summary>
    public CommandPermissionLevel CommandPermission = CommandPermissionLevel.Any;

    /// <summary>
    /// Ability layers applied to the player.
    /// </summary>
    public List<AbilityLayer> Layers = [];

    public override void Deserialize(BinaryReader reader) {
        EntityUniqueId = reader.ReadInt64(true);
        PlayerPermission = (PlayerPermissionLevel)reader.ReadUInt8();
        CommandPermission = (CommandPermissionLevel)reader.ReadUInt8();
        int count = reader.ReadUInt8();
        Layers = new List<AbilityLayer>(count);
        for (int i = 0; i < count; i++) {
            AbilityLayer layer = new();
            layer.Read(reader);
            Layers.Add(layer);
        }
    }

    public override void Serialize(BinaryWriter writer) {
        writer.WriteInt64(EntityUniqueId, true);
        writer.WriteUInt8((byte)PlayerPermission);
        writer.WriteUInt8((byte)CommandPermission);
        writer.WriteUInt8((byte)Layers.Count);
        for (int i = 0; i < Layers.Count; i++) {
            Layers[i].Write(writer);
        }
    }
}
