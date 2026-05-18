using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record UpdateBlockPacket : DataPacket
{
    public BlockPos Position { get; set; }
    public uint NetworkBlockId { get; set; }
    public UpdateBlockFlagsType Flags { get; set; }
    public UpdateBlockLayerType Layer { get; set; }

    public override PacketId PacketId => PacketId.UpdateBlock;

    public override void Deserialize(BinaryReader reader)
    {
        BlockPos position = Position;
        position.Read(reader);
        Position = position;
        NetworkBlockId = reader.ReadVarUInt();
        Flags = (UpdateBlockFlagsType)reader.ReadVarUInt();
        Layer = (UpdateBlockLayerType)reader.ReadVarUInt();
    }

    public override void Serialize(BinaryWriter writer)
    {
        Position.Write(writer);
        writer.WriteVarUInt(NetworkBlockId);
        writer.WriteVarUInt((uint)Flags);
        writer.WriteVarUInt((uint)Layer);
    }
}
