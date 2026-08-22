using Basalt.BedrockProtocol.Enums;
using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ClientboundDataStoreUpdate : DataType {
    public ClientboundDataStoreUpdateType Type;
    public DataStoreUpdate Update = new();
    public DataStoreChange Change = new();
    public DataStoreRemoval Removal = new();

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Type);
        switch (Type) {
            case ClientboundDataStoreUpdateType.Update: Update.Write(ref writer); break;
            case ClientboundDataStoreUpdateType.Change: Change.Write(ref writer); break;
            case ClientboundDataStoreUpdateType.Removal: Removal.Write(ref writer); break;
            default: throw new ArgumentOutOfRangeException(nameof(Type));
        }
    }

    public override void Read(ref BinaryReader reader) {
        Type = (ClientboundDataStoreUpdateType)reader.ReadVarUInt();
        switch (Type) {
            case ClientboundDataStoreUpdateType.Update: Update.Read(ref reader); break;
            case ClientboundDataStoreUpdateType.Change: Change.Read(ref reader); break;
            case ClientboundDataStoreUpdateType.Removal: Removal.Read(ref reader); break;
            default: throw new FormatException("Unsupported clientbound data store update type.");
        }
    }
}
