using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CreateStackRequestAction : IStackRequestAction, DataType {
    /// <summary>
    /// Stack request action id.
    /// </summary>
    public byte ActionType => 6;

    /// <summary>
    /// Result slot index used for created items.
    /// </summary>
    public byte ResultsSlot;
    public void Read(BinaryReader reader) => ResultsSlot = reader.ReadUInt8();
    public void Write(BinaryWriter writer) => writer.WriteUInt8(ResultsSlot);
}
