using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(6)]
public sealed class ResourcePacksInfoPacket : DataPacket {
    public bool ResourcePackRequired;
    public bool HasAddonPacks;
    public bool HasScripts;
    public bool ForceDisableVibrantVisuals;
    public PackIdVersion WorldTemplateIdVersion = new();
    public PackInfoData[] ResourcePacks = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteBool(ResourcePackRequired);
        writer.WriteBool(HasAddonPacks);
        writer.WriteBool(HasScripts);
        writer.WriteBool(ForceDisableVibrantVisuals);
        WorldTemplateIdVersion.Write(ref writer);
        writer.WriteVarUInt((uint)ResourcePacks.Length);
        foreach (PackInfoData pack in ResourcePacks)
            pack.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ResourcePackRequired = reader.ReadBool();
        HasAddonPacks = reader.ReadBool();
        HasScripts = reader.ReadBool();
        ForceDisableVibrantVisuals = reader.ReadBool();
        WorldTemplateIdVersion.Read(ref reader);
        uint count = reader.ReadVarUInt();
        ResourcePacks = new PackInfoData[(int)count];
        for (int i = 0; i < ResourcePacks.Length; i++) {
            PackInfoData pack = new();
            pack.Read(ref reader);
            ResourcePacks[i] = pack;
        }
    }
}
