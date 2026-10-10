using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(7)]
public sealed class ResourcePackStackPacket : DataPacket {
    public bool TexturePackRequired;
    public PackInstanceId[] TexturePacks = [];
    public string BaseGameVersion = string.Empty;
    public Experiments Experiments = new();
    public bool IncludeEditorPacks;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteBool(TexturePackRequired);
        writer.WriteVarUInt((uint)TexturePacks.Length);
        foreach (PackInstanceId pack in TexturePacks)
            pack.Write(ref writer);
        writer.WriteVarString(BaseGameVersion);
        Experiments.Write(ref writer);
        writer.WriteBool(IncludeEditorPacks);
    }

    public override void Deserialize(ref BinaryReader reader) {
        TexturePackRequired = reader.ReadBool();
        uint count = reader.ReadVarUInt();
        TexturePacks = new PackInstanceId[(int)count];
        for (int i = 0; i < TexturePacks.Length; i++) {
            PackInstanceId pack = new();
            pack.Read(ref reader);
            TexturePacks[i] = pack;
        }
        BaseGameVersion = reader.ReadVarString();
        Experiments.Read(ref reader);
        IncludeEditorPacks = reader.ReadBool();
    }
}
