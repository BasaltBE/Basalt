using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(6)]
public sealed class ResourcePacksInfoPacket : DataPacket {
    public bool ResourcePackRequired;
    public bool HasAddonPacks;
    public bool HasScripts;
    public bool ForceDisableVibrantVisuals;
    public PackIdVersion WorldTemplateIdAndVersion = new();
    public PackInfoData[] ResourcePacks = Array.Empty<PackInfoData>();

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteBool(ResourcePackRequired);
        writer.WriteBool(HasAddonPacks);
        writer.WriteBool(HasScripts);
        writer.WriteBool(ForceDisableVibrantVisuals);
        WorldTemplateIdAndVersion.Write(ref writer);
        writer.WriteVarUInt((uint)ResourcePacks.Length);

        foreach (PackInfoData pack in ResourcePacks) {
            pack.Write(ref writer);
        }
    }

    public override void Deserialize(ref BinaryReader reader) {
        ResourcePackRequired = reader.ReadBool();
        HasAddonPacks = reader.ReadBool();
        HasScripts = reader.ReadBool();
        ForceDisableVibrantVisuals = reader.ReadBool();
        WorldTemplateIdAndVersion.Read(ref reader);
        int count = checked((int)reader.ReadVarUInt());
        ResourcePacks = new PackInfoData[count];

        for (int index = 0; index < count; index++) {
            PackInfoData pack = new();
            pack.Read(ref reader);
            ResourcePacks[index] = pack;
        }
    }
}
