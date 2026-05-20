using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class BeaconPaymentStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 10;
    public int PrimaryEffect { get; set; }
    public int SecondaryEffect { get; set; }

    public void Read(BinaryReader reader)
    {
        PrimaryEffect = reader.ReadZigZag();
        SecondaryEffect = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteZigZag(PrimaryEffect);
        writer.WriteZigZag(SecondaryEffect);
    }
}
