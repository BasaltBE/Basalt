#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PlayerScoreboardId {
    public long PlayerUniqueId;

    public void Read(BinaryReader reader) {
        PlayerUniqueId = reader.ReadZigZong();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZong(PlayerUniqueId);
    }
}
