namespace BasaltBE.Basalt.Worlds.Dimensions.Chunk;

static class PaletteEncoding {
    internal static int GetBitsPerEntry(int paletteCount) => paletteCount switch {
        <= 1 => 0,
        <= 2 => 1,
        <= 4 => 2,
        <= 8 => 3,
        <= 16 => 4,
        <= 32 => 5,
        <= 64 => 6,
        <= 256 => 8,
        _ => 16
    };
}
