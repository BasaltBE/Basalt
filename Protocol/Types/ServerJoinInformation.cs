using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ServerJoinInformation
{
    public OptionalValue<GatheringJoinInfo> GatheringJoinInfo { get; set; } = new();
    public OptionalValue<StoreEntryPointInfo> StoreEntryPointInfo { get; set; } = new();
    public OptionalValue<PresenceInfo> PresenceInfo { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        GatheringJoinInfo.Deserialize(ref reader, static (ref BinaryReader r) =>
        {
            GatheringJoinInfo value = new();
            value.Read(ref r);
            return value;
        });
        StoreEntryPointInfo.Deserialize(ref reader, static (ref BinaryReader r) =>
        {
            StoreEntryPointInfo value = new();
            value.Read(ref r);
            return value;
        });
        PresenceInfo.Deserialize(ref reader, static (ref BinaryReader r) =>
        {
            PresenceInfo value = new();
            value.Read(ref r);
            return value;
        });
    }

    public void Write(ref BinaryWriter writer)
    {
        GatheringJoinInfo.Serialize(ref writer, static (ref BinaryWriter w, GatheringJoinInfo value) => value.Write(ref w));
        StoreEntryPointInfo.Serialize(ref writer, static (ref BinaryWriter w, StoreEntryPointInfo value) => value.Write(ref w));
        PresenceInfo.Serialize(ref writer, static (ref BinaryWriter w, PresenceInfo value) => value.Write(ref w));
    }
}
