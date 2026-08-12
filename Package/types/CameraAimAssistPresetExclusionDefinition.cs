#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraAimAssistPresetExclusionDefinition {
    public List<string> Blocks = [];
    public List<string> Entities = [];
    public List<string> BlockTags = [];
    public List<string> EntityTypeFamilies = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        Blocks = new List<string>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            string item0 = default!;
            item0 = reader.ReadVarString();
            Blocks.Add(item0);
        }
        int count2 = checked((int)reader.ReadVarUInt());
        Entities = new List<string>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            string item2 = default!;
            item2 = reader.ReadVarString();
            Entities.Add(item2);
        }
        int count4 = checked((int)reader.ReadVarUInt());
        BlockTags = new List<string>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            string item4 = default!;
            item4 = reader.ReadVarString();
            BlockTags.Add(item4);
        }
        int count6 = checked((int)reader.ReadVarUInt());
        EntityTypeFamilies = new List<string>(count6);
        for (int i6 = 0; i6 < count6; i6++) {
            string item6 = default!;
            item6 = reader.ReadVarString();
            EntityTypeFamilies.Add(item6);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)Blocks.Count));
        foreach (var item1 in Blocks) {
            writer.WriteVarString(item1);
        }
        writer.WriteVarUInt(checked((uint)Entities.Count));
        foreach (var item3 in Entities) {
            writer.WriteVarString(item3);
        }
        writer.WriteVarUInt(checked((uint)BlockTags.Count));
        foreach (var item5 in BlockTags) {
            writer.WriteVarString(item5);
        }
        writer.WriteVarUInt(checked((uint)EntityTypeFamilies.Count));
        foreach (var item7 in EntityTypeFamilies) {
            writer.WriteVarString(item7);
        }
    }
}
