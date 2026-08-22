using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class NetworkPermissions : DataType {
    public bool ServerAuthSoundEnabled;

    public override void Write(ref BinaryWriter writer) => writer.WriteBool(ServerAuthSoundEnabled);

    public override void Read(ref BinaryReader reader) => ServerAuthSoundEnabled = reader.ReadBool();
}
