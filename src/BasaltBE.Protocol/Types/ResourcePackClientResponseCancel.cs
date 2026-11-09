using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ResourcePackClientResponseCancel : ResourcePackClientResponse {
    public override ResourcePackResponse Type => ResourcePackResponse.Cancel;
    public override void Read(ref BinaryReader reader) { }
    public override void Write(ref BinaryWriter writer) { }
}
