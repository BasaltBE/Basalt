using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class AvailableCommandsOverloadData : DataType {
    public bool IsChaining;
    public AvailableCommandsParamData[] Parameters = [];

    public override void Read(ref BinaryReader reader) {
        IsChaining = reader.ReadBool();
        Parameters = new AvailableCommandsParamData[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Parameters.Length; index++) {
            AvailableCommandsParamData parameter = new();
            parameter.Read(ref reader);
            Parameters[index] = parameter;
        }
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteBool(IsChaining);
        writer.WriteVarUInt((uint)Parameters.Length);
        foreach (AvailableCommandsParamData parameter in Parameters)
            parameter.Write(ref writer);
    }
}
