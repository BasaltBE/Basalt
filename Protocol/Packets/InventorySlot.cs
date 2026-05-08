using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record InventorySlotPacket : DataPacket
{
    public uint WindowId { get; set; }
    public uint Slot { get; set; }
    public OptionalValue<FullContainerName> Container { get; set; } = new();
    public OptionalValue<ItemInstance> StorageItem { get; set; } = new();
    public ItemInstance NewItem { get; set; } = new();

    public override PacketId PacketId => PacketId.InventorySlot;

    public override void Deserialize(ref BinaryReader reader)
    {
        WindowId = reader.ReadVarUInt();
        Slot = reader.ReadVarUInt();
        Container.Read(ref reader, static (ref BinaryReader r) =>
        {
            FullContainerName container = new();
            container.Read(ref r);
            return container;
        });
        StorageItem.Read(ref reader, static (ref BinaryReader r) =>
        {
            ItemInstance item = new();
            ReadNetworkItemInstance(ref r, item);
            return item;
        });
        ReadNetworkItemInstance(ref reader, NewItem);
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(WindowId);
        writer.WriteVarUInt(Slot);
        Container.Write(ref writer, static (ref BinaryWriter w, FullContainerName value) => value.Write(ref w));
        StorageItem.Write(ref writer, static (ref BinaryWriter w, ItemInstance value) => WriteNetworkItemInstance(ref w, value));
        WriteNetworkItemInstance(ref writer, NewItem);
    }

    private static void ReadNetworkItemInstance(ref BinaryReader reader, ItemInstance item)
    {
        item.Stack.NetworkId = reader.ReadInt16(true);
        item.Stack.StackSize = reader.ReadUInt16(true);
        item.Stack.Metadata = reader.ReadVarUInt();
        bool hasNetId = reader.ReadBool();
        if (hasNetId)
        {
            _ = reader.ReadVarUInt();
            item.StackNetworkId = reader.ReadZigZag();
        }
        else
        {
            item.StackNetworkId = 0;
        }

        item.Stack.NetworkBlockId = checked((int)reader.ReadVarUInt());

        if (item.Stack.NetworkId == 0)
        {
            _ = reader.ReadVarUInt();
            item.Stack.ExtraData = null;
            return;
        }

        int extrasLength = checked((int)reader.ReadVarUInt());
        if (extrasLength == 0)
        {
            item.Stack.ExtraData = null;
            return;
        }

        int extrasEndOffset = reader.Offset + extrasLength;
        ItemInstanceUserData extraData = new();
        extraData.Read(ref reader, item.Stack.NetworkId);
        item.Stack.ExtraData = extraData;
        if (reader.Offset < extrasEndOffset)
        {
            reader.Seek(extrasEndOffset);
        }
    }

    private static void WriteNetworkItemInstance(ref BinaryWriter writer, ItemInstance item)
    {
        writer.WriteInt16((short)item.Stack.NetworkId, true);
        writer.WriteUInt16(item.Stack.StackSize, true);
        writer.WriteVarUInt(item.Stack.Metadata);
        bool hasNetId = item.StackNetworkId != 0;
        writer.WriteBool(hasNetId);
        if (hasNetId)
        {
            writer.WriteVarUInt(0);
            writer.WriteZigZag(item.StackNetworkId);
        }

        writer.WriteVarUInt((uint)item.Stack.NetworkBlockId);
        if (item.Stack.NetworkId == 0)
        {
            writer.WriteVarUInt(0);
            return;
        }

        if (item.Stack.ExtraData is null)
        {
            writer.WriteVarUInt(0);
            return;
        }

        byte[] payloadBuffer = new byte[8192];
        BinaryWriter payloadWriter = new(payloadBuffer);
        item.Stack.ExtraData.Write(ref payloadWriter, item.Stack.NetworkId);
        ReadOnlySpan<byte> payload = payloadWriter.GetBuffer();
        writer.WriteVarUInt((uint)payload.Length);
        writer.WriteBytes(payload);
    }
}
