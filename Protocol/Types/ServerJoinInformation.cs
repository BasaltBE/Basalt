using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ServerJoinInformation : DataType
{
    public Optional<GatheringJoinInfo> GatheringJoinInfo = new();
    public Optional<StoreEntryPointInfo> StoreEntryPointInfo = new();
    public Optional<PresenceInfo> PresenceInfo = new();

    public void Read(BinaryReader reader)
    {
        GatheringJoinInfo.Read(reader);
        StoreEntryPointInfo.Read(reader);
        PresenceInfo.Read(reader);
    }

    public void Write(BinaryWriter writer)
    {
        GatheringJoinInfo.Write(writer);
        StoreEntryPointInfo.Write(writer);
        PresenceInfo.Write(writer);
    }
}



