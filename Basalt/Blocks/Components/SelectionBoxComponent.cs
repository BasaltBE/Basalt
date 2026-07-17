using System.Text.Json;
using Basalt.Protocol.Nbt;

namespace Basalt.Core.Blocks.Components;

public sealed class SelectionBoxComponent : BlockComponent
{
    public static new string Identifier => "minecraft:selection_box";
    public override string ComponentIdentifier => "minecraft:selection_box";

    public bool Enabled { get; }
    public float OriginX { get; }
    public float OriginY { get; }
    public float OriginZ { get; }
    public float SizeX { get; }
    public float SizeY { get; }
    public float SizeZ { get; }

    public SelectionBoxComponent(
      bool enabled = true,
      float originX = -8f, float originY = 0f, float originZ = -8f,
      float sizeX = 16f, float sizeY = 16f, float sizeZ = 16f)
    {
        Enabled = enabled;
        OriginX = originX;
        OriginY = originY;
        OriginZ = originZ;
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
    }

    public override void OnWrite(CompoundTag tag)
    {
        tag.Set("enabled", new ByteTag { Value = (sbyte)(Enabled ? 1 : 0) });

        ListTag origin = new() { Name = "origin" };
        origin.Values.Add(new FloatTag { Value = OriginX });
        origin.Values.Add(new FloatTag { Value = OriginY });
        origin.Values.Add(new FloatTag { Value = OriginZ });
        tag.Set("origin", origin);

        ListTag size = new() { Name = "size" };
        size.Values.Add(new FloatTag { Value = SizeX });
        size.Values.Add(new FloatTag { Value = SizeY });
        size.Values.Add(new FloatTag { Value = SizeZ });
        tag.Set("size", size);
    }

    public override void OnRead(CompoundTag tag)
    {
    }

    public static SelectionBoxComponent FromJson(JsonElement element)
    {
        bool enabled = true;
        float originX = -8f, originY = 0f, originZ = -8f;
        float sizeX = 16f, sizeY = 16f, sizeZ = 16f;

        if (element.TryGetProperty("enabled", out JsonElement enabledEl))
        {
            enabled = enabledEl.ValueKind == JsonValueKind.True;
        }

        if (element.TryGetProperty("origin", out JsonElement originEl) && originEl.ValueKind == JsonValueKind.Array)
        {
            int i = 0;
            foreach (JsonElement val in originEl.EnumerateArray())
            {
                if (i == 0) { originX = val.GetSingle(); }
                else if (i == 1) { originY = val.GetSingle(); }
                else if (i == 2) { originZ = val.GetSingle(); }
                i++;
            }
        }

        if (element.TryGetProperty("size", out JsonElement sizeEl) && sizeEl.ValueKind == JsonValueKind.Array)
        {
            int i = 0;
            foreach (JsonElement val in sizeEl.EnumerateArray())
            {
                if (i == 0) { sizeX = val.GetSingle(); }
                else if (i == 1) { sizeY = val.GetSingle(); }
                else if (i == 2) { sizeZ = val.GetSingle(); }
                i++;
            }
        }

        return new SelectionBoxComponent(enabled, originX, originY, originZ, sizeX, sizeY, sizeZ);
    }
}
