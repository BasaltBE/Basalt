using Basalt.Protocol.IO;
using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemInstanceUserData : DataType<int?>
{
    private const ushort NbtMarker = 0xFFFF;
    // i dont why it is like that but it is
    private const byte NbtVersion = 0x01;

    public CompoundTag? Nbt { get; set; }
    public List<string> CanPlaceOn { get; set; } = [];
    public List<string> CanDestroy { get; set; } = [];
    public long? Ticking { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Read(ref reader, null);
    }

    public void Write(ref BinaryWriter writer)
    {
        Write(ref writer, null);
    }

    public void Read(ref BinaryReader reader, int? networkId)
    {
        ushort marker = reader.ReadUInt16(true);
        if (marker == NbtMarker)
        {
            byte version = reader.ReadUInt8();
            if (version != NbtVersion)
            {
                throw new InvalidOperationException($"Unsupported item NBT formatting version: {version}");
            }

            TagType type = (TagType)reader.ReadInt8();
            if (type != TagType.Compound)
            {
                throw new InvalidOperationException($"Expected Compound tag for item NBT, got {type}.");
            }

            Nbt = CompoundTag.Read(ref reader);
        }
        else
        {
            Nbt = null;
        }

        int canPlaceOnCount = reader.ReadInt32(true);
        CanPlaceOn = new(Math.Max(canPlaceOnCount, 0));
        for (int i = 0; i < canPlaceOnCount; i++)
        {
            CanPlaceOn.Add(reader.ReadString32(true));
        }

        int canDestroyCount = reader.ReadInt32(true);
        CanDestroy = new(Math.Max(canDestroyCount, 0));
        for (int i = 0; i < canDestroyCount; i++)
        {
            CanDestroy.Add(reader.ReadString32(true));
        }

        if (networkId == ProtocolInfo.ShieldNetworkId)
        {
            if (reader.Remaining >= sizeof(long))
            {
                Ticking = reader.ReadInt64(true);
            }
            else
            {
                Ticking = null;
            }
        }
        else
        {
            Ticking = null;
        }
    }

    public void Write(ref BinaryWriter writer, int? networkId)
    {
        if (Nbt is null)
        {
            writer.WriteUInt16(0, true);
        }
        else
        {
            writer.WriteUInt16(NbtMarker, true);
            writer.WriteUInt8(NbtVersion);
            NBT.WriteTag(ref writer, Nbt, new ReadWriteOptions(Name: true, Type: true, VarInt: false), canHaveName: true);
        }

        writer.WriteInt32(CanPlaceOn.Count, true);
        for (int i = 0; i < CanPlaceOn.Count; i++)
        {
            writer.WriteString32(CanPlaceOn[i], true);
        }

        writer.WriteInt32(CanDestroy.Count, true);
        for (int i = 0; i < CanDestroy.Count; i++)
        {
            writer.WriteString32(CanDestroy[i], true);
        }

        if (networkId == ProtocolInfo.ShieldNetworkId)
        {
            writer.WriteInt64(Ticking ?? 0, true);
        }
    }

}
