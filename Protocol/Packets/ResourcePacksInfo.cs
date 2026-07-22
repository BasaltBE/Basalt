using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ResourcePacksInfo)]
public sealed record ResourcePacksInfoPacket : DataPacket {
    /// <summary>
    /// Whether the client must accept the resource packs or not. 
    /// If this is true, vanilla forces the client to accept or else it will disconnect
    /// </summary>
    public bool MustAccept;

    /// <summary>
    /// Whether the server has any addons.
    /// </summary>
    public bool HasAddons;

    /// <summary>
    /// Whether the server has any scripts.
    /// </summary>
    public bool HasScripts;

    /// <summary>
    /// Whether the server forces vibrant visuals to be disabled.
    /// </summary>
    public bool ForceDisableVibrantVisuals;

    /// <summary>
    /// The UUID of the world template.
    /// </summary>
    public Guid WorldTemplateUuid = Guid.Empty;

    /// <summary>
    /// The version of the world template.
    /// </summary>
    public string WorldTemplateVersion = string.Empty;

    /// <summary>
    /// List of resource packs that the server has. 
    /// </summary>
    public List<ResourcePackInfo> Packs = [];

    public override void Deserialize(BinaryReader reader) {
        MustAccept = reader.ReadBool();
        HasAddons = reader.ReadBool();
        HasScripts = reader.ReadBool();
        ForceDisableVibrantVisuals = reader.ReadBool();
        WorldTemplateUuid = UUID.Read(reader);
        WorldTemplateVersion = reader.ReadVarString();
        int packsLength = reader.ReadUInt16(true);
        Packs = new List<ResourcePackInfo>(packsLength);
        for (int i = 0; i < packsLength; i++) {
            ResourcePackInfo pack = new();
            pack.Read(reader);
            Packs.Add(pack);
        }
    }

    public override void Serialize(BinaryWriter writer) {
        writer.WriteBool(MustAccept);
        writer.WriteBool(HasAddons);
        writer.WriteBool(HasScripts);
        writer.WriteBool(ForceDisableVibrantVisuals);
        UUID.Write(writer, WorldTemplateUuid);
        writer.WriteVarString(WorldTemplateVersion);
        writer.WriteUInt16((ushort)Packs.Count, true);
        for (int i = 0; i < Packs.Count; i++) {
            Packs[i].Write(writer);
        }
    }
}
