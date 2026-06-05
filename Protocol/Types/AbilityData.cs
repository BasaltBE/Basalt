using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class AbilityData : DataType
{
    /// <summary>
    /// Unique entity id bound to this ability payload.
    /// </summary>
    public long EntityUniqueId;

    /// <summary>
    /// Player permission level.
    /// </summary>
    public PlayerPermissionLevel PlayerPermission = PlayerPermissionLevel.Member;

    /// <summary>
    /// Command permission level.
    /// </summary>
    public CommandPermissionLevel CommandPermission = CommandPermissionLevel.Any;

    /// <summary>
    /// Ability layers for the player.
    /// </summary>
    public List<AbilityLayer> Layers = [];

    public void Read(BinaryReader reader)
    {
        EntityUniqueId = reader.ReadInt64(true);
        PlayerPermission = (PlayerPermissionLevel)reader.ReadUInt8();
        CommandPermission = (CommandPermissionLevel)reader.ReadUInt8();

        int count = reader.ReadUInt8();
        Layers = new List<AbilityLayer>(count);
        for (int i = 0; i < count; i++)
        {
            AbilityLayer layer = new();
            layer.Read(reader);
            Layers.Add(layer);
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteInt64(EntityUniqueId, true);
        writer.WriteUInt8((byte)PlayerPermission);
        writer.WriteUInt8((byte)CommandPermission);
        writer.WriteUInt8((byte)Layers.Count);
        for (int i = 0; i < Layers.Count; i++)
        {
            Layers[i].Write(writer);
        }
    }
}
