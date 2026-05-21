using Basalt.Binary;
using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// Pretty much same as LegacyItem just some types are different,
/// e.g network runtime id is Uint16 instead of ZigZag
/// </summary>
public sealed class NetworkItemStackDescriptor : DataType
{
    /// <summary>
    /// Network item id.
    /// </summary>
    public int NetworkId;

    /// <summary>
    /// Item count.
    /// </summary>
    public ushort Count;

    /// <summary>
    /// Item metadata value.
    /// </summary>
    public uint Metadata;

    /// <summary>
    /// Stack network id.
    /// </summary>
    public int StackNetworkId;

    /// <summary>
    /// Block runtime id.
    /// </summary>
    public int BlockRuntimeId;

    /// <summary>
    /// Optional item NBT payload.
    /// </summary>
    public CompoundTag? Nbt;

    /// <summary>
    /// Blocks item can be placed on.
    /// </summary>
    public List<string> CanPlaceOn = [];

    /// <summary>
    /// Blocks item can destroy.
    /// </summary>
    public List<string> CanDestroy = [];

    /// <summary>
    /// Shield blocking tick value.
    /// </summary>
    public long BlockingTick;

    public void Read(BinaryReader reader)
    {
        NetworkId = reader.ReadInt16(true);
        Count = reader.ReadUInt16(true);
        Metadata = reader.ReadVarUInt();

        bool hasNetId = reader.ReadBool();
        if (hasNetId)
        {
            // its always 0 for some reason
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
                throw new InvalidOperationException($"Unsupported network item NBT version: {version}");
            }

            Nbt = Io.NBT.ReadTag<CompoundTag>(reader, new Basalt.Protocol.Nbt.TagOptions(Name: true, Type: true, VarInt: false));
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

        if (NetworkId == Io.Constants.ShieldNetworkId && reader.Offset + sizeof(long) <= endOffset)
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

        using BinaryStream extraBuffer = BinaryStream.Rent(16384);
        BinaryWriter extraWriter = extraBuffer.GetWriter();

        if (Nbt is null)
        {
            extraWriter.WriteInt16(0, true);
        }
        else
        {
            extraWriter.WriteInt16(-1, true);
            extraWriter.WriteUInt8(1);
            Io.NBT.WriteTag(extraWriter, Nbt, new Basalt.Protocol.Nbt.TagOptions(Name: true, Type: true, VarInt: false));
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

        if (NetworkId == Io.Constants.ShieldNetworkId)
        { 
            extraWriter.WriteInt64(BlockingTick, true);
        }

        ReadOnlySpan<byte> payload = extraWriter.GetProcessedBytes();
        writer.WriteVarUInt(checked((uint)payload.Length));
        writer.WriteBytes(payload);
    }
}
