using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SubChunkHeightmapData {
    public HeightMapDataType HeightMapType;
    public List<List<sbyte>>? SubchunkHeightMap;
    public HeightMapDataType RenderHeightMapType;
    public List<List<sbyte>>? SubchunkRenderHeightMap;

    public void Read(BinaryReader reader) {
        HeightMapType = (global::BedrockProtocol.Enums.HeightMapDataType)reader.ReadUInt8();
        if (reader.ReadBool()) {
            int count2 = 16;
            SubchunkHeightMap = new List<List<sbyte>>(count2);
            for (int i2 = 0; i2 < count2; i2++) {
                List<sbyte> item2 = default!;
                int count1002 = 16;
                item2 = new List<sbyte>(count1002);
                for (int i1002 = 0; i1002 < count1002; i1002++) {
                    sbyte item1002 = default!;
                    item1002 = reader.ReadInt8();
                    item2.Add(item1002);
                }
                SubchunkHeightMap.Add(item2);
            }
        } else {
            SubchunkHeightMap = default;
        }
        RenderHeightMapType = (global::BedrockProtocol.Enums.HeightMapDataType)reader.ReadUInt8();
        if (reader.ReadBool()) {
            int count6 = 16;
            SubchunkRenderHeightMap = new List<List<sbyte>>(count6);
            for (int i6 = 0; i6 < count6; i6++) {
                List<sbyte> item6 = default!;
                int count1006 = 16;
                item6 = new List<sbyte>(count1006);
                for (int i1006 = 0; i1006 < count1006; i1006++) {
                    sbyte item1006 = default!;
                    item1006 = reader.ReadInt8();
                    item6.Add(item1006);
                }
                SubchunkRenderHeightMap.Add(item6);
            }
        } else {
            SubchunkRenderHeightMap = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)HeightMapType);
        writer.WriteBool(SubchunkHeightMap is not null);
        if (SubchunkHeightMap is { } optionalValue3) {
            foreach (var item3 in optionalValue3) {
                foreach (var item1003 in item3) {
                    writer.WriteInt8(item1003);
                }
            }
        }
        writer.WriteUInt8((byte)RenderHeightMapType);
        writer.WriteBool(SubchunkRenderHeightMap is not null);
        if (SubchunkRenderHeightMap is { } optionalValue7) {
            foreach (var item7 in optionalValue7) {
                foreach (var item1007 in item7) {
                    writer.WriteInt8(item1007);
                }
            }
        }
    }
}
