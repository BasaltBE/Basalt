using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class BeaconPaymentStackRequestAction : IStackRequestAction, DataType {
    public byte ActionType => 10;
    /// <summary>
    /// Selected primary beacon effect id.
    /// </summary>
    public int PrimaryEffect;
    /// <summary>
    /// Selected secondary beacon effect id.
    /// </summary>
    public int SecondaryEffect;
    public void Read(BinaryReader reader) {
        PrimaryEffect = reader.ReadZigZag();
        SecondaryEffect = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(PrimaryEffect);
        writer.WriteZigZag(SecondaryEffect);
    }
}
