using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(8)]
public sealed class ResourcePackClientResponsePacket : DataPacket {
    public ResourcePackResponse Response;
    public string[] DownloadingPacks = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteInt8((sbyte)((int)Response - 1));
        writer.WriteVarString(Response.ToString().ToLowerInvariant());
        if (Response == ResourcePackResponse.Downloading) {
            writer.WriteVarUInt((uint)DownloadingPacks.Length);
            foreach (string pack in DownloadingPacks) writer.WriteVarString(pack);
        }
    }

    public override void Deserialize(ref BinaryReader reader) {
        Response = (ResourcePackResponse)(reader.ReadInt8() + 1);
        reader.ReadVarString();
        if (Response == ResourcePackResponse.Downloading) {
            DownloadingPacks = new string[checked((int)reader.ReadVarUInt())];
            for (int index = 0; index < DownloadingPacks.Length; index++) DownloadingPacks[index] = reader.ReadVarString();
        } else {
            DownloadingPacks = [];
        }
    }
}
