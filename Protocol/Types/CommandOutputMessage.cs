using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// Single message returned by command output.
/// </summary>
public sealed class CommandOutputMessage : DataType
{
    /// <summary>
    /// Whether this output message represents a successful command result.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Message or translation key sent as command output.
    /// </summary>
    public string Message = string.Empty;

    /// <summary>
    /// Translation parameters for the output message.
    /// </summary>
    public List<string> Parameters = [];

    public void Read(BinaryReader reader)
    {
        Message = reader.ReadVarString();
        Success = reader.ReadBool();
        int count = checked((int)reader.ReadVarUInt());
        Parameters = new(count);
        for (int i = 0; i < count; i++)
        {
            Parameters.Add(reader.ReadVarString());
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(Message);
        writer.WriteBool(Success);
        writer.WriteVarUInt((uint)Parameters.Count);
        for (int i = 0; i < Parameters.Count; i++)
        {
            writer.WriteVarString(Parameters[i]);
        }
    }
}
