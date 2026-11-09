using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

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

    public override void Deserialize(ref BinaryReader reader) {
        EnumValues = ReadStrings(ref reader);
        ChainedSubcommandValues = ReadStrings(ref reader);
        PostFixes = ReadStrings(ref reader);
        Enums = ReadValues<AvailableCommandsEnumData>(ref reader);
        ChainedSubcommands = ReadValues<AvailableCommandsChainedSubcommandData>(ref reader);
        Commands = ReadValues<AvailableCommandsCommandData>(ref reader);
        SoftEnums = ReadValues<AvailableCommandsSoftEnumData>(ref reader);
        Constraints = ReadValues<AvailableCommandsConstrainedValueData>(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer) {
        WriteStrings(ref writer, EnumValues);
        WriteStrings(ref writer, ChainedSubcommandValues);
        WriteStrings(ref writer, PostFixes);
        WriteValues(ref writer, Enums);
        WriteValues(ref writer, ChainedSubcommands);
        WriteValues(ref writer, Commands);
        WriteValues(ref writer, SoftEnums);
        WriteValues(ref writer, Constraints);
    }

    private static string[] ReadStrings(ref BinaryReader reader) {
        string[] values = new string[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < values.Length; index++)
            values[index] = reader.ReadVarString();
        return values;
    }

    private static T[] ReadValues<T>(ref BinaryReader reader) where T : DataType, new() {
        T[] values = new T[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < values.Length; index++) {
            T value = new();
            value.Read(ref reader);
            values[index] = value;
        }
        return values;
    }

    private static void WriteStrings(ref BinaryWriter writer, string[] values) {
        writer.WriteVarUInt((uint)values.Length);
        foreach (string value in values)
            writer.WriteVarString(value);
    }

    private static void WriteValues<T>(ref BinaryWriter writer, T[] values) where T : DataType {
        writer.WriteVarUInt((uint)values.Length);
        foreach (T value in values)
            value.Write(ref writer);
    }
}
