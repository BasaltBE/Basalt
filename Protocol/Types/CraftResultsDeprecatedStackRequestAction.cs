using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CraftResultsDeprecatedStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 19;
    public List<NetworkItemStackDescriptor> ResultItems { get; set; } = [];
    public byte TimesCrafted { get; set; }

    public void Read(BinaryReader reader)
    {
        int count = checked((int)reader.ReadVarUInt());
        ResultItems = new(count);
        for (int i = 0; i < count; i++)
        {
            NetworkItemStackDescriptor item = new();
            item.Read(reader);
            ResultItems.Add(item);
        }

        TimesCrafted = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarUInt((uint)ResultItems.Count);
        for (int i = 0; i < ResultItems.Count; i++)
        {
            ResultItems[i].Write(writer);
        }

        writer.WriteUInt8(TimesCrafted);
    }
}
