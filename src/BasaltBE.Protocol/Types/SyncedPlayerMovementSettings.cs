using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class SyncedPlayerMovementSettings : DataType {
    public int RewindHistorySize;
    public bool ServerAuthoritativeBlockBreaking;

    public override void Read(ref BinaryReader reader) {
        RewindHistorySize = reader.ReadZigZag();
        ServerAuthoritativeBlockBreaking = reader.ReadBool();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag(RewindHistorySize);
        writer.WriteBool(ServerAuthoritativeBlockBreaking);
    }
}
