using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ResourcePackClientResponseDownloading : ResourcePackClientResponse {
    public override ResourcePackResponse Type => ResourcePackResponse.Downloading;
    public string[] PackIds = [];

    public override void Read(ref BinaryReader reader) {
        uint count = reader.ReadVarUInt();
        if (count > reader.Remaining)
            throw new InvalidDataException("Resource pack response has an invalid pack count.");
        PackIds = new string[(int)count];
        for (int i = 0; i < PackIds.Length; i++)
            PackIds[i] = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)PackIds.Length);
        foreach (string packId in PackIds)
            writer.WriteVarString(packId);
    }
}
