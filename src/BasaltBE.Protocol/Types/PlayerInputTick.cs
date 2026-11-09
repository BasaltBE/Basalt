using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PlayerInputTick : DataType {
    public ulong Tick;
    public override void Read(ref BinaryReader reader) => Tick = reader.ReadVarULong();
    public override void Write(ref BinaryWriter writer) => writer.WriteVarULong(Tick);
}
