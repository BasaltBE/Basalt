#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ScoreboardIdentityPacketInfo {
    public ScoreboardId ScoreboardId = new();
    public long? PlayerUniqueId;

    public void Read(BinaryReader reader) {
        ScoreboardId.Read(reader);
        if (reader.ReadBool()) {
            PlayerUniqueId = reader.ReadZigZong();
        } else {
            PlayerUniqueId = default;
        }
    }

    public void Write(BinaryWriter writer) {
        ScoreboardId.Write(writer);
        writer.WriteBool(PlayerUniqueId is not null);
        if (PlayerUniqueId is { } optionalValue3) {
            writer.WriteZigZong(optionalValue3);
        }
    }
}
