using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(76)]
public sealed class AvailableCommandsPacket : DataPacket {
    public string[] EnumValues = [];
    public string[] ChainedSubcommandValues = [];
    public string[] PostFixes = [];
    public AvailableCommandsEnumData[] Enums = [];
    public AvailableCommandsChainedSubcommandData[] ChainedSubcommands = [];
    public AvailableCommandsCommandData[] Commands = [];
    public AvailableCommandsSoftEnumData[] SoftEnums = [];
    public AvailableCommandsConstrainedValueData[] Constraints = [];

    public override void Serialize(ref BinaryWriter writer) {
        WriteStrings(ref writer, EnumValues);
        WriteStrings(ref writer, ChainedSubcommandValues);
        WriteStrings(ref writer, PostFixes);
        WriteArray(ref writer, Enums);
        WriteArray(ref writer, ChainedSubcommands);
        WriteArray(ref writer, Commands);
        WriteArray(ref writer, SoftEnums);
        WriteArray(ref writer, Constraints);
    }

    public override void Deserialize(ref BinaryReader reader) {
        EnumValues = ReadStrings(ref reader);
        ChainedSubcommandValues = ReadStrings(ref reader);
        PostFixes = ReadStrings(ref reader);
        Enums = ReadArray<AvailableCommandsEnumData>(ref reader);
        ChainedSubcommands = ReadArray<AvailableCommandsChainedSubcommandData>(ref reader);
        Commands = ReadArray<AvailableCommandsCommandData>(ref reader);
        SoftEnums = ReadArray<AvailableCommandsSoftEnumData>(ref reader);
        Constraints = ReadArray<AvailableCommandsConstrainedValueData>(ref reader);
    }

    static void WriteStrings(ref BinaryWriter writer, string[] values) {
        writer.WriteVarUInt((uint)values.Length);
        for (int i = 0; i < values.Length; i++) writer.WriteVarString(values[i]);
    }

    static string[] ReadStrings(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        string[] values = new string[count];
        for (int i = 0; i < count; i++) values[i] = reader.ReadVarString();
        return values;
    }

    static void WriteArray<T>(ref BinaryWriter writer, T[] values) where T : DataType {
        writer.WriteVarUInt((uint)values.Length);
        for (int i = 0; i < values.Length; i++) values[i].Write(ref writer);
    }

    static T[] ReadArray<T>(ref BinaryReader reader) where T : DataType, new() {
        int count = checked((int)reader.ReadVarUInt());
        T[] values = new T[count];
        for (int i = 0; i < count; i++) {
            values[i] = new T();
            values[i].Read(ref reader);
        }
        return values;
    }
}
