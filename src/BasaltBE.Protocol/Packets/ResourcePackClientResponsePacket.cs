using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(8)]
public sealed class ResourcePackClientResponsePacket : DataPacket {
    public ResourcePackClientResponse Response = new ResourcePackClientResponseCancel();

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Response.Type - 1);
        writer.WriteVarString(Response.Type.ToString());
        Response.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        uint variant = reader.ReadVarUInt();
        Response = variant switch {
            0 => new ResourcePackClientResponseCancel(),
            1 => new ResourcePackClientResponseDownloading(),
            2 => new ResourcePackClientResponseDownloadingFinished(),
            3 => new ResourcePackClientResponseStackFinished(),
            _ => throw new InvalidDataException($"Unknown resource pack response variant '{variant}'.")
        };
        string typeName = reader.ReadVarString();
        if (!Enum.TryParse(typeName, true, out ResourcePackResponse type) || type != Response.Type)
            throw new InvalidDataException($"Unknown resource pack response '{typeName}'.");
        Response.Read(ref reader);
    }
}
