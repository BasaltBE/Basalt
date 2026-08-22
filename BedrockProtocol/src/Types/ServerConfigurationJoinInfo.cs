using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ServerConfigurationJoinInfo : DataType {
    public GatheringsConfigurationJoinInfo? Gathering;
    public ClientStoreEntryPointConfiguration? ClientStoreEntryPoint;
    public PresenceConfiguration? Presence;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteBool(Gathering is not null);
        if (Gathering is not null) Gathering.Write(ref writer);
        writer.WriteBool(ClientStoreEntryPoint is not null);
        if (ClientStoreEntryPoint is not null) ClientStoreEntryPoint.Write(ref writer);
        writer.WriteBool(Presence is not null);
        if (Presence is not null) Presence.Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        Gathering = reader.ReadBool() ? new GatheringsConfigurationJoinInfo() : null;
        if (Gathering is not null) Gathering.Read(ref reader);
        ClientStoreEntryPoint = reader.ReadBool() ? new ClientStoreEntryPointConfiguration() : null;
        if (ClientStoreEntryPoint is not null) ClientStoreEntryPoint.Read(ref reader);
        Presence = reader.ReadBool() ? new PresenceConfiguration() : null;
        if (Presence is not null) Presence.Read(ref reader);
    }
}
