using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

public abstract class CommandResponsePacket : DataPacket {
    public CommandOriginData OriginData = new();
    public CommandOutput Output = new();

    public override void Serialize(ref BinaryWriter writer) {
        OriginData.Write(ref writer);
        Output.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        OriginData.Read(ref reader);
        Output.Read(ref reader);
    }
}
