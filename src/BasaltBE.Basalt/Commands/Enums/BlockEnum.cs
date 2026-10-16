using System.Collections.Frozen;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class BlockEnum() : IdentifierEnum<BlockType>("Block", Values)
{
    private static readonly FrozenDictionary<string, BlockType> Values =
        Index(BlockPalette.Types, static type => type.Identifier);
}
