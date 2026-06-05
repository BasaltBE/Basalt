using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ChangeDimension)]
public sealed record ChangeDimensionPacket : DataPacket
{
    public DimensionType Dimension;
    public Vec3f Position;
    public bool Respawn;
    public bool HasLoadingScreen;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        Dimension = (DimensionType)reader.ReadZigZag();

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        Respawn = reader.ReadBool();
        HasLoadingScreen = reader.ReadBool();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteZigZag((int)Dimension);
        Position.Write(writer);
        writer.WriteBool(Respawn);
        writer.WriteBool(HasLoadingScreen);
    }
}
