using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ResourcePackClientResponse)]
public sealed record ResourcePackClientResponsePacket : DataPacket {
    /// <summary>
    /// The client's response to the resource pack request. 
    /// Whether they accepted, refused, have all the packs or even completed.
    /// </summary>
    public ResourcePackResponse Response;

    /// <summary>
    /// List of resource packs that the client wants to download.
    /// </summary>
    public List<string> PacksToDownload = [];


    public override void Deserialize(BinaryReader reader) {
        Response = (ResourcePackResponse)reader.ReadUInt8();
        int length = reader.ReadUInt16(true);
        PacksToDownload = new List<string>(length);

        for (int i = 0; i < length; i++) {
            PacksToDownload.Add(reader.ReadVarString());
        }
    }

    public override void Serialize(BinaryWriter writer) {
        writer.WriteUInt8((byte)Response);
        writer.WriteUInt16((ushort)PacksToDownload.Count, true);

        for (int i = 0; i < PacksToDownload.Count; i++) {
            writer.WriteVarString(PacksToDownload[i]);
        }
    }
}
