#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AdventureSettings {
    public bool NoPvM;
    public bool NoMvP;
    public bool ImmutableWorld;
    public bool ShowNameTags;
    public bool AutoJump;

    public void Read(BinaryReader reader) {
        NoPvM = reader.ReadBool();
        NoMvP = reader.ReadBool();
        ImmutableWorld = reader.ReadBool();
        ShowNameTags = reader.ReadBool();
        AutoJump = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteBool(NoPvM);
        writer.WriteBool(NoMvP);
        writer.WriteBool(ImmutableWorld);
        writer.WriteBool(ShowNameTags);
        writer.WriteBool(AutoJump);
    }
}
