using System.Text.Json;
using Basalt.Protocol.Nbt;

namespace Basalt.Core.Blocks.Components;

public sealed class MaterialInstancesComponent : BlockComponent
{
    public static new string Identifier => "minecraft:material_instances";
    public override string ComponentIdentifier => "minecraft:material_instances";

    public MaterialInstance[] Instances { get; }

    public MaterialInstancesComponent(MaterialInstance[]? instances = null)
    {
        Instances = instances ?? [];
    }

    public override void OnWrite(CompoundTag tag)
    {
        CompoundTag mappings = new() { Name = "mappings" };
        tag.Set("mappings", mappings);

        CompoundTag materials = new() { Name = "materials" };
        for (int i = 0; i < Instances.Length; i++)
        {
            MaterialInstance instance = Instances[i];
            CompoundTag instanceTag = new() { Name = instance.Key };
            instanceTag.Set("texture", new StringTag { Value = instance.Texture });
            instanceTag.Set("render_method", new StringTag { Value = instance.RenderMethod });
            instanceTag.Set("face_dimming", new StringTag { Value = instance.FaceDimming ? "true" : "false" });
            instanceTag.Set("ambient_occlusion", new StringTag { Value = instance.AmbientOcclusion ? "true" : "false" });
            materials.Set(instance.Key, instanceTag);
        }

        tag.Set("materials", materials);
    }

    public override void OnRead(CompoundTag tag)
    {
    }

    public static MaterialInstancesComponent FromJson(JsonElement element)
    {
        List<MaterialInstance> instances = [];

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in element.EnumerateObject())
            {
                string key = prop.Name;
                JsonElement val = prop.Value;

                string texture = "";
                string renderMethod = "alpha_test";
                bool faceDimming = true;
                bool ambientOcclusion = true;

                if (val.TryGetProperty("texture", out JsonElement texEl) && texEl.ValueKind == JsonValueKind.String)
                {
                    texture = texEl.GetString() ?? "";
                }

                if (val.TryGetProperty("render_method", out JsonElement rmEl) && rmEl.ValueKind == JsonValueKind.String)
                {
                    renderMethod = rmEl.GetString() ?? "alpha_test";
                }

                if (val.TryGetProperty("face_dimming", out JsonElement fdEl))
                {
                    faceDimming = fdEl.ValueKind == JsonValueKind.True;
                }

                if (val.TryGetProperty("ambient_occlusion", out JsonElement aoEl))
                {
                    ambientOcclusion = aoEl.ValueKind == JsonValueKind.True;
                }

                instances.Add(new MaterialInstance(key, texture, renderMethod, faceDimming, ambientOcclusion));
            }
        }

        return new MaterialInstancesComponent([.. instances]);
    }
}

public readonly struct MaterialInstance(
  string key, string texture, string renderMethod,
  bool faceDimming, bool ambientOcclusion)
{
    public string Key { get; } = key;
    public string Texture { get; } = texture;
    public string RenderMethod { get; } = renderMethod;
    public bool FaceDimming { get; } = faceDimming;
    public bool AmbientOcclusion { get; } = ambientOcclusion;
}
