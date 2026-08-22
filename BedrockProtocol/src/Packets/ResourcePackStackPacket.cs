using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(7)]
public sealed class ResourcePackStackPacket : DataPacket {
    public bool TexturePackRequired;
    public PackInstanceId[] TexturePackList = Array.Empty<PackInstanceId>();
    public string BaseGameVersion = string.Empty;
    public Experiments Experiments = new();
    public bool IncludeEditorPacks;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteBool(TexturePackRequired);
        writer.WriteVarUInt((uint)TexturePackList.Length);

        foreach (PackInstanceId pack in TexturePackList) {
            pack.Write(ref writer);
        }

        writer.WriteVarString(BaseGameVersion);
        Experiments.Write(ref writer);
        writer.WriteBool(IncludeEditorPacks);
    }

    public override void Deserialize(ref BinaryReader reader) {
        TexturePackRequired = reader.ReadBool();
        int count = checked((int)reader.ReadVarUInt());
        TexturePackList = new PackInstanceId[count];

        for (int index = 0; index < count; index++) {
            PackInstanceId pack = new();
            pack.Read(ref reader);
            TexturePackList[index] = pack;
        }

        BaseGameVersion = reader.ReadVarString();
        Experiments.Read(ref reader);
        IncludeEditorPacks = reader.ReadBool();
    }
}
