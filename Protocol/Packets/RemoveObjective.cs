using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.RemoveObjective)]
public sealed record RemoveObjectivePacket : DataPacket
{
    public string ObjectiveName = string.Empty;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        ObjectiveName = reader.ReadVarString();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarString(ObjectiveName);
    }
}
