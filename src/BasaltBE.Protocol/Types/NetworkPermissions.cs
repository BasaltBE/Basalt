using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class NetworkPermissions : DataType {
    public bool ServerAuthSoundEnabled;

    public override void Read(ref BinaryReader reader) => ServerAuthSoundEnabled = reader.ReadBool();

    public override void Write(ref BinaryWriter writer) => writer.WriteBool(ServerAuthSoundEnabled);
}
