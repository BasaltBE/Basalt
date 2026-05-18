using Basalt.Protocol.IO;
using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemInstanceUserData : DataType<int?>
{
    private const short NbtMarker = -1;
    // i dont why it is like that but it is
    private const byte NbtVersion = 0x01;

    public CompoundTag? Nbt { get; set; }
    public List<string> CanPlaceOn { get; set; } = [];
    public List<string> CanDestroy { get; set; } = [];
    public long? Ticking { get; set; }

    public void Read(BinaryReader reader)
    {
        Read(reader, null);
    }

    public void Write(BinaryWriter writer)
    {
        Write(writer, null);
    }

    public void Read(BinaryReader reader, int? networkId)
    {
        short marker = reader.ReadInt16(true);
        if (marker == NbtMarker)
        {
            byte version = reader.ReadUInt8();
            if (version != NbtVersion)
            {
                throw new InvalidOperationException($"Unsupported item NBT formatting version: {version}");
            }

            Nbt = NBT.Read<CompoundTag>(reader, new ReadWriteOptions(Name: true, Type: true, VarInt: false), canHaveName: true);
        }
        else if (marker > 0)
        {
            Nbt = NBT.Read<CompoundTag>(reader, new ReadWriteOptions(Name: true, Type: true, VarInt: false), canHaveName: true);
        }
        else
        {
            Nbt = null;
        }

        int canPlaceOnCount = checked((int)reader.ReadUInt32(true));
        CanPlaceOn = new(canPlaceOnCount);
        for (int i = 0; i < canPlaceOnCount; i++)
        {
            CanPlaceOn.Add(reader.ReadString16(true));
        }

        int canDestroyCount = checked((int)reader.ReadUInt32(true));
        CanDestroy = new(canDestroyCount);
        for (int i = 0; i < canDestroyCount; i++)
        {
            CanDestroy.Add(reader.ReadString16(true));
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

    public void Write(BinaryWriter writer, int? networkId)
    {
        if (Nbt is null)
        {
            writer.WriteInt16(0, true);
        }
        else
        {
            writer.WriteInt16(NbtMarker, true);
            writer.WriteUInt8(NbtVersion);
            NBT.WriteTag(writer, Nbt, new ReadWriteOptions(Name: true, Type: true, VarInt: false), canHaveName: true);
        }

        writer.WriteUInt32(checked((uint)CanPlaceOn.Count), true);
        for (int i = 0; i < CanPlaceOn.Count; i++)
        {
            writer.WriteString16(CanPlaceOn[i], true);
        }

        writer.WriteUInt32(checked((uint)CanDestroy.Count), true);
        for (int i = 0; i < CanDestroy.Count; i++)
        {
            writer.WriteString16(CanDestroy[i], true);
        }

        if (networkId == ProtocolInfo.ShieldNetworkId)
        {
            writer.WriteInt64(Ticking ?? 0, true);
        }
    }

}
