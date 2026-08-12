#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraAimAssistCategoryPriorities {
    public Dictionary<string, int> Entities = [];
    public Dictionary<string, int> Blocks = [];
    public Dictionary<string, int> BlockTags = [];
    public Dictionary<string, int> EntityTypeFamilies = [];
    public int EntityDefault;
    public int BlockDefault;

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        Entities = new Dictionary<string, int>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            string key0 = default!;
            key0 = reader.ReadVarString();
            int value0 = default!;
            value0 = reader.ReadInt32(true);
            Entities.Add(key0, value0);
        }
        int count2 = checked((int)reader.ReadVarUInt());
        Blocks = new Dictionary<string, int>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            string key2 = default!;
            key2 = reader.ReadVarString();
            int value2 = default!;
            value2 = reader.ReadInt32(true);
            Blocks.Add(key2, value2);
        }
        int count4 = checked((int)reader.ReadVarUInt());
        BlockTags = new Dictionary<string, int>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            string key4 = default!;
            key4 = reader.ReadVarString();
            int value4 = default!;
            value4 = reader.ReadInt32(true);
            BlockTags.Add(key4, value4);
        }
        int count6 = checked((int)reader.ReadVarUInt());
        EntityTypeFamilies = new Dictionary<string, int>(count6);
        for (int i6 = 0; i6 < count6; i6++) {
            string key6 = default!;
            key6 = reader.ReadVarString();
            int value6 = default!;
            value6 = reader.ReadInt32(true);
            EntityTypeFamilies.Add(key6, value6);
        }
        EntityDefault = reader.ReadInt32(true);
        BlockDefault = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)Entities.Count));
        foreach (var pair1 in Entities) {
            writer.WriteVarString(pair1.Key);
            writer.WriteInt32(pair1.Value, true);
        }
        writer.WriteVarUInt(checked((uint)Blocks.Count));
        foreach (var pair3 in Blocks) {
            writer.WriteVarString(pair3.Key);
            writer.WriteInt32(pair3.Value, true);
        }
        writer.WriteVarUInt(checked((uint)BlockTags.Count));
        foreach (var pair5 in BlockTags) {
            writer.WriteVarString(pair5.Key);
            writer.WriteInt32(pair5.Value, true);
        }
        writer.WriteVarUInt(checked((uint)EntityTypeFamilies.Count));
        foreach (var pair7 in EntityTypeFamilies) {
            writer.WriteVarString(pair7.Key);
            writer.WriteInt32(pair7.Value, true);
        }
        writer.WriteInt32(EntityDefault, true);
        writer.WriteInt32(BlockDefault, true);
    }
}
