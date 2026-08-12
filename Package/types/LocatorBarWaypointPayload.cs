#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class LocatorBarWaypointPayload {
    public WaypointHandle GroupHandle = new();
    public Payload ServerWaypointPayload = new();
    public ServerWaypointGroupAction ActionFlag = new();

    public void Read(BinaryReader reader) {
        GroupHandle.Read(reader);
        ServerWaypointPayload.Read(reader);
        ActionFlag = (ServerWaypointGroupAction)reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        GroupHandle.Write(writer);
        ServerWaypointPayload.Write(writer);
        writer.WriteUInt8((byte)ActionFlag);
    }
}
