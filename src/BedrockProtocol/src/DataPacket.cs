using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol;

/// <summary>
/// Defines a packet that can be serialized into caller-provided memory and read from a binary reader.
/// </summary>
public abstract class DataPacket
{
    /// <summary>
    /// Serializes this packet to the supplied writer.
    /// </summary>
    /// <param name="writer">The writer that receives this packet.</param>
    public abstract void Serialize(ref BinaryWriter writer);

    /// <summary>
    /// Deserializes this packet from the supplied reader.
    /// </summary>
    /// <param name="reader">The reader positioned at the start of this packet.</param>
    public abstract void Deserialize(ref BinaryReader reader);
}
