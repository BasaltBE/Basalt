using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class NormalTransactionData : DataType {
    public override void Write(ref BinaryWriter writer) { }

    public override void Read(ref BinaryReader reader) { }
}
