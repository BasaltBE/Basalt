using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class AvailableCommandsOverloadData : DataType {
    public bool IsChaining;
    public AvailableCommandsParamData[] Parameters = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteBool(IsChaining);
        writer.WriteVarUInt((uint)Parameters.Length);
        for (int i = 0; i < Parameters.Length; i++) Parameters[i].Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        IsChaining = reader.ReadBool();
        int count = checked((int)reader.ReadVarUInt());
        Parameters = new AvailableCommandsParamData[count];
        for (int i = 0; i < count; i++) {
            Parameters[i] = new AvailableCommandsParamData();
            Parameters[i].Read(ref reader);
        }
    }
}
