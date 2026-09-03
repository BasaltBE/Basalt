using System.Text.Json;
using Basalt.BedrockProtocol.NBT;

namespace Basalt.Core.Blocks.Components;

public sealed class CollisionBoxComponent : BlockComponent {
    public static new string Identifier => "minecraft:collision_box";
    public override string ComponentIdentifier => "minecraft:collision_box";

    public bool Enabled { get; }
    public CollisionBox[] Boxes { get; }

    public CollisionBoxComponent(bool enabled = true, CollisionBox[]? boxes = null) {
        Enabled = enabled;
        Boxes = boxes ?? [new CollisionBox(-8f, 0f, -8f, 16f, 16f, 16f)];
    }

    public override void OnWrite(CompoundTag tag) {
        tag.Set("enabled", new ByteTag { Value = (sbyte)(Enabled ? 1 : 0) });

        ListTag boxesTag = new() { Name = "boxes" };
        for (int i = 0; i < Boxes.Length; i++) {
            CollisionBox box = Boxes[i];
            CompoundTag boxTag = new();
            boxTag.Set("minX", new FloatTag { Value = box.OriginX });
            boxTag.Set("minY", new FloatTag { Value = box.OriginY });
            boxTag.Set("minZ", new FloatTag { Value = box.OriginZ });
            boxTag.Set("maxX", new FloatTag { Value = box.SizeX });
            boxTag.Set("maxY", new FloatTag { Value = box.SizeY });
            boxTag.Set("maxZ", new FloatTag { Value = box.SizeZ });
            boxesTag.Values.Add(boxTag);
        }

        tag.Set("boxes", boxesTag);
    }

    public override void OnRead(CompoundTag tag) {
    }

    public static CollisionBoxComponent FromJson(JsonElement element) {
        bool enabled = true;
        List<CollisionBox> boxes = [];

        if (element.TryGetProperty("enabled", out JsonElement enabledEl)) {
            enabled = enabledEl.ValueKind == JsonValueKind.True;
        }

        if (element.TryGetProperty("boxes", out JsonElement boxesEl) && boxesEl.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement boxEl in boxesEl.EnumerateArray()) {
                float originX = -8f, originY = 0f, originZ = -8f;
                float sizeX = 16f, sizeY = 16f, sizeZ = 16f;

                if (boxEl.TryGetProperty("origin", out JsonElement originEl) && originEl.ValueKind == JsonValueKind.Array) {
                    int i = 0;
                    foreach (JsonElement val in originEl.EnumerateArray()) {
                        if (i == 0) { originX = val.GetSingle(); }
                        else if (i == 1) { originY = val.GetSingle(); }
                        else if (i == 2) { originZ = val.GetSingle(); }
                        i++;
                    }
                }

                if (boxEl.TryGetProperty("size", out JsonElement sizeEl) && sizeEl.ValueKind == JsonValueKind.Array) {
                    int i = 0;
                    foreach (JsonElement val in sizeEl.EnumerateArray()) {
                        if (i == 0) { sizeX = val.GetSingle(); }
                        else if (i == 1) { sizeY = val.GetSingle(); }
                        else if (i == 2) { sizeZ = val.GetSingle(); }
                        i++;
                    }
                }

                boxes.Add(new CollisionBox(originX, originY, originZ, sizeX, sizeY, sizeZ));
            }
        }

        if (boxes.Count == 0) {
            boxes.Add(new CollisionBox(-8f, 0f, -8f, 16f, 16f, 16f));
        }

        return new CollisionBoxComponent(enabled, [.. boxes]);
    }
}

public readonly struct CollisionBox(
  float originX, float originY, float originZ,
  float sizeX, float sizeY, float sizeZ) {
    public float OriginX { get; } = originX;
    public float OriginY { get; } = originY;
    public float OriginZ { get; } = originZ;
    public float SizeX { get; } = sizeX;
    public float SizeY { get; } = sizeY;
    public float SizeZ { get; } = sizeZ;
}
