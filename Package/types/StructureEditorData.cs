using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class StructureEditorData {
    public RedactableString StructureName = new();
    public string DataField = string.Empty;
    public bool ShouldIncludePlayers;
    public bool ShouldShowBoundingBox;
    public StructureBlockType StructureBlockType;
    public StructureSettings StructureSettings = new();
    public StructureRedstoneSaveMode RedstoneSaveMode;

    public void Read(BinaryReader reader) {
        StructureName.Read(reader);
        DataField = reader.ReadVarString();
        ShouldIncludePlayers = reader.ReadBool();
        ShouldShowBoundingBox = reader.ReadBool();
        StructureBlockType = (global::BedrockProtocol.Enums.StructureBlockType)reader.ReadZigZag();
        StructureSettings.Read(reader);
        RedstoneSaveMode = (global::BedrockProtocol.Enums.StructureRedstoneSaveMode)reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        StructureName.Write(writer);
        writer.WriteVarString(DataField);
        writer.WriteBool(ShouldIncludePlayers);
        writer.WriteBool(ShouldShowBoundingBox);
        writer.WriteZigZag((int)StructureBlockType);
        StructureSettings.Write(writer);
        writer.WriteUInt8((byte)RedstoneSaveMode);
    }
}
