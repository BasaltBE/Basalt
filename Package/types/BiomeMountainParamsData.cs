#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeMountainParamsData {
    public uint SteepBlock;
    public bool NorthSlopes;
    public bool SouthSlopes;
    public bool WestSlopes;
    public bool EastSlopes;
    public bool TopSlideEnabled;

    public void Read(BinaryReader reader) {
        SteepBlock = reader.ReadUInt32(true);
        NorthSlopes = reader.ReadBool();
        SouthSlopes = reader.ReadBool();
        WestSlopes = reader.ReadBool();
        EastSlopes = reader.ReadBool();
        TopSlideEnabled = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(SteepBlock, true);
        writer.WriteBool(NorthSlopes);
        writer.WriteBool(SouthSlopes);
        writer.WriteBool(WestSlopes);
        writer.WriteBool(EastSlopes);
        writer.WriteBool(TopSlideEnabled);
    }
}
