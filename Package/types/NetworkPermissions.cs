#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class NetworkPermissions {
    public bool ServerAuthSoundEnabled;

    public void Read(BinaryReader reader) {
        ServerAuthSoundEnabled = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteBool(ServerAuthSoundEnabled);
    }
}
