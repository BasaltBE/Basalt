using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SyncedPlayerMovementSettings {
    public int RewindHistorySize;
    public bool ServerAuthoritativeBlockBreaking;

    public void Read(BinaryReader reader) {
        RewindHistorySize = reader.ReadZigZag();
        ServerAuthoritativeBlockBreaking = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(RewindHistorySize);
        writer.WriteBool(ServerAuthoritativeBlockBreaking);
    }
}
