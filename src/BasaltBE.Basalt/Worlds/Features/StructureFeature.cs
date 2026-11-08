using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Worlds.Structures;
using BasaltBE.Nbt;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Worlds.Features;

public sealed class StructureFeature : Feature {
    readonly (int X, int Y, int Z, BlockPermutation Block)[] blocks;
    readonly (int X, int Y, int Z, byte[] Data)[] blockActors;
    readonly (string Identifier, float X, float Y, float Z, float Yaw, float Pitch, byte[] Data)[] entities;

    public static bool Supports(Structure structure) {
        ArgumentNullException.ThrowIfNull(structure);
        foreach (CompoundTag saved in structure.Entities) {
            string? identifier = saved.Get<CompoundTag>("nbt")?.Get<StringTag>("id")?.Value;
            if (identifier is null || EntityPalette.Find(identifier) is null || saved.Get<ListTag>("pos")?.Value.Count != 3)
                return false;
        }
        foreach (StructureBlockActor actor in structure.BlockActors) {
            if (actor.Data.Get<StringTag>("id") is null) return false;
        }

        for (int z = 0; z < structure.SizeZ; z++) {
            for (int y = 0; y < structure.SizeY; y++) {
                for (int x = 0; x < structure.SizeX; x++) {
                    if (structure.GetBlock(x, y, z) is not { } entry) continue;
                    if (entry.Permutation is null) return false;
                }
            }
        }

        return true;
    }

    public StructureFeature(Structure structure) : this(
        structure?.Identifier ?? throw new ArgumentNullException(nameof(structure)), structure
    ) { }

    public StructureFeature(string identifier, Structure structure) : base(identifier) {
        ArgumentNullException.ThrowIfNull(structure);
        if (!Supports(structure))
            throw new ArgumentException($"Structure '{structure.Identifier}' is not supported by feature placement.", nameof(structure));

        List<(int X, int Y, int Z, BlockPermutation Block)> entries = [];
        for (int z = 0; z < structure.SizeZ; z++) {
            for (int y = 0; y < structure.SizeY; y++) {
                for (int x = 0; x < structure.SizeX; x++) {
                    if (structure.GetBlock(x, y, z) is not { } entry) continue;
                    if (entry.Permutation is not { } block)
                        throw new ArgumentException($"Structure '{structure.Identifier}' contains an unsupported block.", nameof(structure));
                    if (block.Identifier == "minecraft:structure_void") continue;
                    entries.Add((checked(x + structure.OffsetX), checked(y + structure.OffsetY), checked(z + structure.OffsetZ), block));
                }
            }
        }
        blocks = [.. entries];
        blockActors = new (int, int, int, byte[])[structure.BlockActors.Length];
        for (int index = 0; index < blockActors.Length; index++) {
            StructureBlockActor actor = structure.BlockActors[index];
            if (actor.Data.Get<StringTag>("id") is null)
                throw new ArgumentException($"Structure '{identifier}' contains unsupported block actor data.", nameof(structure));
            int x = actor.Index % structure.SizeX;
            int y = actor.Index / structure.SizeX % structure.SizeY;
            int z = actor.Index / structure.SizeX / structure.SizeY;
            byte[] data = new byte[actor.Data.GetSize(Options.LittleEndian)];
            int offset = 0;
            BasaltBE.Nbt.Nbt.Write(actor.Data, data, ref offset, Options.LittleEndian);
            blockActors[index] = (checked(x + structure.OffsetX), checked(y + structure.OffsetY), checked(z + structure.OffsetZ), data);
        }
        entities = new (string, float, float, float, float, float, byte[])[structure.Entities.Length];
        for (int index = 0; index < entities.Length; index++) {
            CompoundTag saved = structure.Entities[index];
            CompoundTag data = saved.Get<CompoundTag>("nbt")!;
            ListTag pos = saved.Get<ListTag>("pos")!;
            ListTag? rotation = data.Get<ListTag>("Rotation");
            float[] coordinates = pos.Value.Select(value => value switch {
                DoubleTag number => (float)number.Value,
                FloatTag number => number.Value,
                _ => throw new InvalidDataException("A structure entity has an invalid position.")
            }).ToArray();
            byte[] payload = new byte[data.GetSize(Options.LittleEndian)];
            int offset = 0;
            BasaltBE.Nbt.Nbt.Write(data, payload, ref offset, Options.LittleEndian);
            entities[index] = (data.Get<StringTag>("id")!.Value,
                coordinates[0] + structure.OffsetX, coordinates[1] + structure.OffsetY, coordinates[2] + structure.OffsetZ,
                (rotation?.Value.ElementAtOrDefault(0) as FloatTag)?.Value ?? 0,
                (rotation?.Value.ElementAtOrDefault(1) as FloatTag)?.Value ?? 0, payload);
        }
    }

    public override bool Generate(FeatureContext context, int x, int y, int z) {
        foreach ((int offsetX, int offsetY, int offsetZ, BlockPermutation block) in blocks) {
            long targetX = (long)x + offsetX;
            long targetY = (long)y + offsetY;
            long targetZ = (long)z + offsetZ;
            if (targetX < int.MinValue || targetX > int.MaxValue || targetY < int.MinValue || targetY > int.MaxValue ||
                targetZ < int.MinValue || targetZ > int.MaxValue || !context.SetBlock((int)targetX, (int)targetY, (int)targetZ, block))
                return false;
        }
        foreach ((int offsetX, int offsetY, int offsetZ, byte[] data) in blockActors) {
            int offset = 0;
            CompoundTag actor = (CompoundTag)BasaltBE.Nbt.Nbt.Read(data, ref offset, Options.LittleEndian);
            context.SetBlockNbt(checked(x + offsetX), checked(y + offsetY), checked(z + offsetZ), actor);
        }
        foreach ((string identifier, float offsetX, float offsetY, float offsetZ, float yaw, float pitch, byte[] data) in entities) {
            Entity entity = new(identifier);
            int offset = 0;
            entity.Read((CompoundTag)BasaltBE.Nbt.Nbt.Read(data, ref offset, Options.LittleEndian));
            entity.Position.X = x + offsetX;
            entity.Position.Y = y + offsetY;
            entity.Position.Z = z + offsetZ;
            entity.Rotation.Y = yaw;
            entity.Rotation.X = pitch;
            context.AddEntity(entity);
        }

        return true;
    }
}
