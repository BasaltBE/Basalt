using BasaltBE.Basalt.Blocks;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Structures;

public readonly record struct StructureBlockActor(int Index, CompoundTag Data);
public readonly record struct StructurePaletteEntry(BlockPermutation? Permutation, CompoundTag? SourceData);

public sealed class Structure {
    public readonly string Identifier;
    public readonly int SizeX;
    public readonly int SizeY;
    public readonly int SizeZ;
    public readonly int OffsetX;
    public readonly int OffsetY;
    public readonly int OffsetZ;
    public readonly StructurePaletteEntry[] Palette;
    public readonly byte IndexWidth;
    public readonly uint VoidIndex;
    public readonly byte[] Indices;
    public readonly StructureBlockActor[] BlockActors;
    public readonly CompoundTag[] Entities;
    public readonly CompoundTag Extra;

    public Structure(
        string identifier,
        int sizeX,
        int sizeY,
        int sizeZ,
        int offsetX,
        int offsetY,
        int offsetZ,
        StructurePaletteEntry[] palette,
        byte indexWidth,
        byte[] indices,
        StructureBlockActor[] blockActors,
        CompoundTag[] entities,
        CompoundTag? extra = null
    ) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(indices);
        ArgumentNullException.ThrowIfNull(blockActors);
        ArgumentNullException.ThrowIfNull(entities);
        if (sizeX <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeX), "Structure dimensions must be positive.");
        if (sizeY <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeY), "Structure dimensions must be positive.");
        if (sizeZ <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeZ), "Structure dimensions must be positive.");
        if (indexWidth is not (1 or 2 or 4))
            throw new ArgumentOutOfRangeException(nameof(indexWidth), "Structure index width must be 1, 2, or 4 bytes.");

        int volume = checked(sizeX * sizeY * sizeZ);
        if (indices.Length != checked(volume * indexWidth))
            throw new ArgumentException("Structure index data does not match its dimensions.", nameof(indices));
        uint voidIndex = indexWidth switch {
            1 => byte.MaxValue,
            2 => ushort.MaxValue,
            _ => uint.MaxValue
        };
        if (palette.Length >= voidIndex)
            throw new ArgumentException("The structure palette does not fit its index width.", nameof(palette));

        for (int cell = 0; cell < volume; cell++) {
            int offset = cell * indexWidth;
            uint index = indexWidth switch {
                1 => indices[offset],
                2 => (uint)(indices[offset] | indices[offset + 1] << 8),
                _ => (uint)(indices[offset] | indices[offset + 1] << 8 | indices[offset + 2] << 16 | indices[offset + 3] << 24)
            };
            if (index != voidIndex && index >= palette.Length)
                throw new ArgumentException("Structure index data refers outside its palette.", nameof(indices));
        }

        Identifier = identifier;
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
        OffsetX = offsetX;
        OffsetY = offsetY;
        OffsetZ = offsetZ;
        Palette = palette;
        IndexWidth = indexWidth;
        VoidIndex = voidIndex;
        Indices = indices;
        BlockActors = blockActors;
        Entities = entities;
        Extra = extra ?? new CompoundTag();
    }

    public StructurePaletteEntry? GetBlock(int x, int y, int z) {
        if ((uint)x >= (uint)SizeX)
            throw new ArgumentOutOfRangeException(nameof(x), "Structure coordinates are outside its dimensions.");
        if ((uint)y >= (uint)SizeY)
            throw new ArgumentOutOfRangeException(nameof(y), "Structure coordinates are outside its dimensions.");
        if ((uint)z >= (uint)SizeZ)
            throw new ArgumentOutOfRangeException(nameof(z), "Structure coordinates are outside its dimensions.");

        int cell = x + SizeX * (y + SizeY * z);
        int offset = cell * IndexWidth;
        uint index = IndexWidth switch {
            1 => Indices[offset],
            2 => (uint)(Indices[offset] | Indices[offset + 1] << 8),
            _ => (uint)(Indices[offset] | Indices[offset + 1] << 8 | Indices[offset + 2] << 16 | Indices[offset + 3] << 24)
        };
        if (index == VoidIndex)
            return null;
        if (index >= Palette.Length)
            throw new InvalidDataException($"Structure '{Identifier}' has a block index outside its palette.");

        return Palette[(int)index];
    }
}
