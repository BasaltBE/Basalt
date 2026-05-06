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
        GatheringJoinInfo.Read(ref reader, static (ref BinaryReader r) =>
        {
            GatheringJoinInfo value = new();
            value.Read(ref r);
            return value;
        });
        StoreEntryPointInfo.Read(ref reader, static (ref BinaryReader r) =>
        {
            StoreEntryPointInfo value = new();
            value.Read(ref r);
            return value;
        });
        PresenceInfo.Read(ref reader, static (ref BinaryReader r) =>
        {
            PresenceInfo value = new();
            value.Read(ref r);
            return value;
        });
    }

    public void Write(ref BinaryWriter writer)
    {
        GatheringJoinInfo.Write(ref writer, static (ref BinaryWriter w, GatheringJoinInfo value) => value.Write(ref w));
        StoreEntryPointInfo.Write(ref writer, static (ref BinaryWriter w, StoreEntryPointInfo value) => value.Write(ref w));
        PresenceInfo.Write(ref writer, static (ref BinaryWriter w, PresenceInfo value) => value.Write(ref w));
    }
}

