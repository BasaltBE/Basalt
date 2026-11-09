using System.IO.Compression;
using System.Reflection;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Nbt;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Worlds.Structures;

public sealed class StructureRegistry {
    private readonly Dictionary<string, Structure> structures = new(StringComparer.Ordinal);
    private bool frozen;

    public int Count => structures.Count;
    public IEnumerable<Structure> Values => structures.Values;

    public StructureRegistry() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream source = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.structures.nbt")
            ?? throw new InvalidDataException("The built-in structure catalog is missing.");
        using GZipStream gzip = new(source, CompressionMode.Decompress);
        using MemoryStream data = new();
        gzip.CopyTo(data);

        int offset = 0;
        if (BasaltBE.Nbt.Nbt.Read(data.GetBuffer().AsSpan(0, (int)data.Length), ref offset, Options.LittleEndian) is not CompoundTag root ||
            root.Get<IntTag>("format_version") is not { Value: 1 } ||
            root.Get<CompoundTag>("structures") is not { } entries || offset != data.Length)
            throw new InvalidDataException("The built-in structure catalog has an unsupported format.");

        foreach ((string identifier, Tag tag) in entries.Tags) {
            if (tag is not CompoundTag entry ||
                entry.Get<IntArrayTag>("size") is not { Value.Length: 3 } size ||
                entry.Get<IntArrayTag>("offset") is not { Value.Length: 3 } structureOffset ||
                entry.Get<ListTag>("palette") is not { } paletteTag ||
                entry.Get<ByteTag>("index_width") is not { } widthTag ||
                entry.Get<ByteArrayTag>("indices") is not { } indicesTag)
                throw new InvalidDataException($"Structure '{identifier}' has invalid catalog data.");

            StructurePaletteEntry[] palette = new StructurePaletteEntry[paletteTag.Value.Count];
            for (int index = 0; index < palette.Length; index++) {
                if (paletteTag.Value[index] is not CompoundTag state || state.Get<StringTag>("name") is null)
                    throw new InvalidDataException($"Structure '{identifier}' has an invalid block palette entry at index {index}.");

                BlockPermutation? permutation = BlockStorage.ReadPermutation(state);
                palette[index] = new StructurePaletteEntry(permutation, permutation is null ? state : null);
            }

            ListTag blockActorTags = entry.Get<ListTag>("block_actors") ?? new ListTag();
            StructureBlockActor[] blockActors = new StructureBlockActor[blockActorTags.Value.Count];
            for (int index = 0; index < blockActors.Length; index++) {
                if (blockActorTags.Value[index] is not CompoundTag actor ||
                    actor.Get<IntTag>("index") is not { } cell ||
                    actor.Get<CompoundTag>("data") is not { } actorData)
                    throw new InvalidDataException($"Structure '{identifier}' has invalid block actor data.");
                if (cell.Value < 0 || cell.Value >= checked(size.Value[0] * size.Value[1] * size.Value[2]))
                    throw new InvalidDataException($"Structure '{identifier}' has a block actor outside its dimensions.");

                blockActors[index] = new StructureBlockActor(cell.Value, actorData);
            }

            ListTag entityTags = entry.Get<ListTag>("entities") ?? new ListTag();
            CompoundTag[] entities = new CompoundTag[entityTags.Value.Count];
            for (int index = 0; index < entities.Length; index++) {
                if (entityTags.Value[index] is not CompoundTag entity || entity.Get<CompoundTag>("data") is not { } entityData)
                    throw new InvalidDataException($"Structure '{identifier}' has invalid entity data.");

                entities[index] = entityData;
            }

            CompoundTag extra = entry.Get<CompoundTag>("extra") ?? new CompoundTag();
            Structure structure = new(
                identifier,
                size.Value[0], size.Value[1], size.Value[2],
                structureOffset.Value[0], structureOffset.Value[1], structureOffset.Value[2],
                palette,
                widthTag.Value,
                indicesTag.Value,
                blockActors,
                entities,
                extra
            );
            if (!structures.TryAdd(identifier, structure))
                throw new InvalidDataException($"The structure catalog contains duplicate identifier '{identifier}'.");
        }
    }

    public Structure? Find(string identifier) => structures.GetValueOrDefault(identifier);

    public void Register(Structure structure) {
        if (frozen) throw new InvalidOperationException("Structure registrations are closed after the server starts.");
        ArgumentNullException.ThrowIfNull(structure);

        if (!structures.TryAdd(structure.Identifier, structure))
            throw new InvalidOperationException($"Structure '{structure.Identifier}' is already registered.");
    }

    public bool Unregister(string identifier) {
        if (frozen) throw new InvalidOperationException("Structure registrations are closed after the server starts.");
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return structures.Remove(identifier);
    }

    internal void Freeze() => frozen = true;
}
