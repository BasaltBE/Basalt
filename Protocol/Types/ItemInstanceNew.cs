using Basalt.Binary;
using Basalt.Protocol.IO;
using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// SOme sort of a new ITemStance, 
/// mainly thanks to gophertunnel for the full structure
/// </summary>
public sealed class ItemInstanceNew : DataType
{
    public int NetworkId { get; set; }
    public ushort Count { get; set; }
    public uint Metadata { get; set; }
    public int StackNetworkId { get; set; }
    public int BlockRuntimeId { get; set; }
    public CompoundTag? Nbt { get; set; }
    public List<string> CanPlaceOn { get; set; } = [];
    public List<string> CanDestroy { get; set; } = [];
    public long BlockingTick { get; set; }

    public void Read(BinaryReader reader)
    {
        NetworkId = reader.ReadInt16(true);
        Count = reader.ReadUInt16(true);
        Metadata = reader.ReadVarUInt();

        bool hasNetId = reader.ReadBool();
        if (hasNetId)
        {
            _ = reader.ReadVarUInt();
            StackNetworkId = reader.ReadVarInt();
        }
        else
        {
            StackNetworkId = 0;
        }

        BlockRuntimeId = checked((int)reader.ReadVarUInt());
        int extraLength = checked((int)reader.ReadVarUInt());
        if (extraLength == 0)
        {
            return;
        }

        int endOffset = reader.Offset + extraLength;
        short marker = reader.ReadInt16(true);
        if (marker == -1)
        {
            byte version = reader.ReadUInt8();
            if (version != 1)
            {
                throw new InvalidOperationException($"Unsupported item instance new NBT version: {version}");
            }

            Nbt = NBT.Read<CompoundTag>(reader, new ReadWriteOptions(Name: true, Type: true, VarInt: false), canHaveName: true);
        }
        else
        {
            Nbt = null;
        }

        int canPlaceOnCount = checked((int)reader.ReadUInt32(true));
        CanPlaceOn = new List<string>(Math.Max(canPlaceOnCount, 0));
        for (int i = 0; i < canPlaceOnCount; i++)
        {
            CanPlaceOn.Add(reader.ReadString16(true));
        }

        int canDestroyCount = checked((int)reader.ReadUInt32(true));
        CanDestroy = new List<string>(Math.Max(canDestroyCount, 0));
        for (int i = 0; i < canDestroyCount; i++)
        {
            CanDestroy.Add(reader.ReadString16(true));
        }

        if (NetworkId == ProtocolInfo.ShieldNetworkId && reader.Offset + sizeof(long) <= endOffset)
        {
            BlockingTick = reader.ReadInt64(true);
        }

        if (reader.Offset < endOffset)
        {
            reader.Seek(endOffset);
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteInt16(unchecked((short)NetworkId), true);
        writer.WriteUInt16(Count, true);
        writer.WriteVarUInt(Metadata);

        bool hasNetId = StackNetworkId != 0;
        writer.WriteBool(hasNetId);
        if (hasNetId)
        {
            writer.WriteVarUInt(0);
            writer.WriteVarInt(StackNetworkId);
        }

        writer.WriteVarUInt(unchecked((uint)BlockRuntimeId));

        if (NetworkId == 0)
        {
            writer.WriteVarUInt(0);
            return;
        }

        // It will dispose automaticly at the end of the scope
        using BinaryStream extrBuffer = BinaryStream.Rent(16384);
        BinaryWriter extraWriter = extrBuffer.GetWriter();

        if (Nbt is null)
        {
            extraWriter.WriteInt16(0, true);
        }
        else
        {
            extraWriter.WriteInt16(-1, true);
            extraWriter.WriteUInt8(1);
            NBT.WriteTag(extraWriter, Nbt, new ReadWriteOptions(Name: true, Type: true, VarInt: false), canHaveName: true);
        }

        extraWriter.WriteUInt32(checked((uint)CanPlaceOn.Count), true);
        for (int i = 0; i < CanPlaceOn.Count; i++)
        {
            extraWriter.WriteString16(CanPlaceOn[i], true);
        }

        extraWriter.WriteUInt32(checked((uint)CanDestroy.Count), true);
        for (int i = 0; i < CanDestroy.Count; i++)
        {
            extraWriter.WriteString16(CanDestroy[i], true);
        }

        if (NetworkId == ProtocolInfo.ShieldNetworkId)
        {
            extraWriter.WriteInt64(BlockingTick, true);
        }

        ReadOnlySpan<byte> payload = extraWriter.GetProcessedBytes();
        writer.WriteVarUInt(checked((uint)payload.Length));
        writer.WriteBytes(payload);
    }
}
