using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PlayerPartyInfo {
    public string PartyId = string.Empty;
    public bool IsPartyLeader;

    public void Read(BinaryReader reader) {
        PartyId = reader.ReadVarString();
        IsPartyLeader = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(PartyId);
        writer.WriteBool(IsPartyLeader);
    }
}
