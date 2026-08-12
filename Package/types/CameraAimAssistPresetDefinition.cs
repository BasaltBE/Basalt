#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraAimAssistPresetDefinition {
    public string Identifier = string.Empty;
    public CameraAimAssistPresetExclusionDefinition? ExclusionSettings;
    public List<string>? LiquidTargetingList;
    public Dictionary<string, string>? ItemSettings;
    public string? DefaultItemSettings;
    public string? HandSettings;

    public void Read(BinaryReader reader) {
        Identifier = reader.ReadVarString();
        if (reader.ReadBool()) {
            CameraAimAssistPresetExclusionDefinition readValue2 = new();
            readValue2.Read(reader);
            ExclusionSettings = readValue2;
        } else {
            ExclusionSettings = default;
        }
        if (reader.ReadBool()) {
            int count4 = checked((int)reader.ReadVarUInt());
            LiquidTargetingList = new List<string>(count4);
            for (int i4 = 0; i4 < count4; i4++) {
                string item4 = default!;
                item4 = reader.ReadVarString();
                LiquidTargetingList.Add(item4);
            }
        } else {
            LiquidTargetingList = default;
        }
        if (reader.ReadBool()) {
            int count6 = checked((int)reader.ReadVarUInt());
            ItemSettings = new Dictionary<string, string>(count6);
            for (int i6 = 0; i6 < count6; i6++) {
                string key6 = default!;
                key6 = reader.ReadVarString();
                string value6 = default!;
                value6 = reader.ReadVarString();
                ItemSettings.Add(key6, value6);
            }
        } else {
            ItemSettings = default;
        }
        if (reader.ReadBool()) {
            DefaultItemSettings = reader.ReadVarString();
        } else {
            DefaultItemSettings = default;
        }
        if (reader.ReadBool()) {
            HandSettings = reader.ReadVarString();
        } else {
            HandSettings = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Identifier);
        writer.WriteBool(ExclusionSettings is not null);
        if (ExclusionSettings is { } optionalValue3) {
            optionalValue3.Write(writer);
        }
        writer.WriteBool(LiquidTargetingList is not null);
        if (LiquidTargetingList is { } optionalValue5) {
            writer.WriteVarUInt(checked((uint)optionalValue5.Count));
            foreach (var item5 in optionalValue5) {
                writer.WriteVarString(item5);
            }
        }
        writer.WriteBool(ItemSettings is not null);
        if (ItemSettings is { } optionalValue7) {
            writer.WriteVarUInt(checked((uint)optionalValue7.Count));
            foreach (var pair7 in optionalValue7) {
                writer.WriteVarString(pair7.Key);
                writer.WriteVarString(pair7.Value);
            }
        }
        writer.WriteBool(DefaultItemSettings is not null);
        if (DefaultItemSettings is { } optionalValue9) {
            writer.WriteVarString(optionalValue9);
        }
        writer.WriteBool(HandSettings is not null);
        if (HandSettings is { } optionalValue11) {
            writer.WriteVarString(optionalValue11);
        }
    }
}
