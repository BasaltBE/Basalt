using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ResourcePacksInfoPacket : DataPacket
{
    public bool MustAccept { get; set; }
    public bool HasAddons { get; set; }
    public bool HasScripts { get; set; }
    public bool ForceDisableVibrantVisuals { get; set; }
    public Guid WorldTemplateUuid { get; set; } = Guid.Empty;
    public string WorldTemplateVersion { get; set; } = string.Empty;
    public List<ResourcePackInfo> Packs { get; set; } = [];

    public override PacketId PacketId => PacketId.ResourcePacksInfo;

    public override void Deserialize(ref BinaryReader reader)
    {
        MustAccept = reader.ReadBool();
        HasAddons = reader.ReadBool();
        HasScripts = reader.ReadBool();
        ForceDisableVibrantVisuals = reader.ReadBool();
        WorldTemplateUuid = UUID.Read(ref reader);
        WorldTemplateVersion = reader.ReadVarString();
        int packsLength = reader.ReadUInt16(true);
        Packs = new List<ResourcePackInfo>(packsLength);
        for (int i = 0; i < packsLength; i++)
        {
            ResourcePackInfo pack = new();
            pack.Read(ref reader);
            Packs.Add(pack);
        }
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteBool(MustAccept);
        writer.WriteBool(HasAddons);
        writer.WriteBool(HasScripts);
        writer.WriteBool(ForceDisableVibrantVisuals);
        UUID.Write(ref writer, WorldTemplateUuid);
        writer.WriteVarString(WorldTemplateVersion);
        writer.WriteUInt16((ushort)Packs.Count, true);
        for (int i = 0; i < Packs.Count; i++)
        {
            Packs[i].Write(ref writer);
        }
    }
}
