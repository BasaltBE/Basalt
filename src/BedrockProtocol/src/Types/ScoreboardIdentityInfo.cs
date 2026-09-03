using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ScoreboardIdentityInfo : DataType {
    public ScoreboardId ScoreboardId = new();
    public long? PlayerUniqueId;

    public override void Write(ref BinaryWriter writer) {
        ScoreboardId.Write(ref writer);
        writer.WriteBool(PlayerUniqueId.HasValue);
        if (PlayerUniqueId is long playerUniqueId) writer.WriteVarLong(playerUniqueId);
    }

    public override void Read(ref BinaryReader reader) {
        ScoreboardId.Read(ref reader);
        PlayerUniqueId = reader.ReadBool() ? reader.ReadVarLong() : null;
    }
}
