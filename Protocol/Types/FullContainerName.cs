using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class FullContainerName : DataType {
    /// <summary>
    /// Container name identifying the slot group.
    /// </summary>
    public byte ContainerId;

    /// <summary>
    /// Optional dynamic container id.
    /// </summary>
    public uint? DynamicContainerId;

    public void Read(BinaryReader reader) {
        ContainerId = reader.ReadUInt8();
        bool isDynamic = reader.ReadBool();
        DynamicContainerId = isDynamic ? reader.ReadUInt32(true) : null;
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(ContainerId);
        if (DynamicContainerId.HasValue) {
            writer.WriteBool(true);
            writer.WriteUInt32(DynamicContainerId.Value, true);
        }
        else {
            writer.WriteBool(false);
        }
    }
}
