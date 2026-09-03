using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol;

/// <summary>
/// Defines binary read and write operations for a protocol data type.
/// </summary>
public abstract class DataType
{
    /// <summary>
    /// Reads a value from the supplied reader.
    /// </summary>
    /// <param name="reader">The reader positioned at the start of this data type.</param>
    public abstract void Read(ref BinaryReader reader);

    /// <summary>
    /// Writes this data type to the supplied writer.
    /// </summary>
    /// <param name="writer">The writer that receives this data type.</param>
    public abstract void Write(ref BinaryWriter writer);
}
