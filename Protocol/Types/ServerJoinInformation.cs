using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ServerJoinInformation : DataType
{
    public Optional<GatheringJoinInfo> GatheringJoinInfo { get; set; } = new();
    public Optional<StoreEntryPointInfo> StoreEntryPointInfo { get; set; } = new();
    public Optional<PresenceInfo> PresenceInfo { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        GatheringJoinInfo.Read(ref reader);
        StoreEntryPointInfo.Read(ref reader);
        PresenceInfo.Read(ref reader);
    }

    public void Write(ref BinaryWriter writer)
    {
        GatheringJoinInfo.Write(ref writer);
        StoreEntryPointInfo.Write(ref writer);
        PresenceInfo.Write(ref writer);
    }
}



