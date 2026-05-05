using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ResourcePackClientResponsePacket : DataPacket
{
    public ResourcePackResponse Response { get; set; }
    public List<string> PacksToDownload { get; set; } = [];

    public override PacketId PacketId => PacketId.ResourcePackClientResponse;

    public override void Deserialize(ref BinaryReader reader)
    {
        Response = (ResourcePackResponse)reader.ReadUInt8();
        int length = reader.ReadUInt16(true);
        PacksToDownload = new List<string>(length);

        for (int i = 0; i < length; i++)
        {
            PacksToDownload.Add(reader.ReadVarString());
        }
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteUInt8((byte)Response);
        writer.WriteUInt16((ushort)PacksToDownload.Count, true);

        for (int i = 0; i < PacksToDownload.Count; i++)
        {
            writer.WriteVarString(PacksToDownload[i]);
        }
    }
}
